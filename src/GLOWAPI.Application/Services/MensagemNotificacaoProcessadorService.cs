using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Mensageria;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class MensagemNotificacaoProcessadorService : IMensagemNotificacaoProcessadorService
{
    private readonly IMensagemNotificacaoRepository _repository;
    private readonly IProvedorMensagemResolver _provedorResolver;
    private readonly MensageriaOptions _options;
    private readonly ILogger<MensagemNotificacaoProcessadorService> _logger;

    public MensagemNotificacaoProcessadorService(
        IMensagemNotificacaoRepository repository,
        IProvedorMensagemResolver provedorResolver,
        IOptions<MensageriaOptions> options,
        ILogger<MensagemNotificacaoProcessadorService> logger)
    {
        _repository = repository;
        _provedorResolver = provedorResolver;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> ProcessarLoteAsync(
        string instanciaWorker,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var lote = await _repository.ReservarLoteAsync(
            _options.TamanhoLote,
            instanciaWorker,
            utcNow,
            cancellationToken);

        if (lote.Count == 0)
        {
            return 0;
        }

        var processadas = 0;

        foreach (var mensagem in lote)
        {
            try
            {
                await ProcessarMensagemAsync(mensagem, utcNow, cancellationToken);
                processadas++;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha inesperada ao processar mensagem. MensagemGuid={MensagemGuid}, Canal={Canal}, InstanciaWorker={InstanciaWorker}",
                    mensagem.Guid,
                    mensagem.Canal,
                    instanciaWorker);

                mensagem.MarcarFalhaParaRetry(
                    DateTime.UtcNow,
                    ex.Message,
                    _options.BackoffBaseSegundos,
                    _options.BackoffMaximoSegundos);
                _repository.Atualizar(mensagem);
            }
        }

        await _repository.SalvarAlteracoesAsync(cancellationToken);
        return processadas;
    }

    private async Task ProcessarMensagemAsync(
        MensagemNotificacao mensagem,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var provedor = _provedorResolver.Resolver(mensagem);
        var tentativa = mensagem.Tentativas + 1;

        ResultadoEnvioMensagem resultado;

        try
        {
            resultado = await provedor.EnviarAsync(mensagem, cancellationToken);
        }
        catch (Exception ex)
        {
            resultado = new ResultadoEnvioMensagem(
                Sucesso: false,
                RequestPayload: null,
                ResponsePayload: null,
                RespostaProvedor: null,
                MensagemErro: ex.Message,
                TempoExecucaoMs: 0);
        }

        var log = new MensagemNotificacaoLog
        {
            MensagemNotificacaoId = mensagem.Id,
            Tentativa = tentativa,
            RequestPayload = MensageriaLogSanitizer.MascararPayload(resultado.RequestPayload, _options.MascararDadosSensiveisEmLogs),
            ResponsePayload = MensageriaLogSanitizer.MascararPayload(resultado.ResponsePayload, _options.MascararDadosSensiveisEmLogs),
            RespostaProvedor = resultado.RespostaProvedor,
            TempoExecucaoMs = resultado.TempoExecucaoMs,
            Status = resultado.Sucesso ? StatusMensagemNotificacao.Enviado : StatusMensagemNotificacao.Falhou,
            MensagemErro = resultado.MensagemErro,
            CriadoEm = DateTime.UtcNow
        };

        await _repository.AdicionarLogAsync(log, cancellationToken);

        if (resultado.Sucesso)
        {
            mensagem.MarcarEnviada(DateTime.UtcNow);

            _logger.LogInformation(
                "Mensagem enviada. MensagemGuid={MensagemGuid}, Canal={Canal}, Destinatario={Destinatario}, Tentativa={Tentativa}, TempoExecucaoMs={TempoExecucaoMs}",
                mensagem.Guid,
                mensagem.Canal,
                MensageriaLogSanitizer.MascararDestinatario(mensagem.Destinatario, _options.MascararDadosSensiveisEmLogs),
                tentativa,
                resultado.TempoExecucaoMs);
        }
        else
        {
            mensagem.MarcarFalhaParaRetry(
                DateTime.UtcNow,
                resultado.MensagemErro ?? "Falha no envio.",
                _options.BackoffBaseSegundos,
                _options.BackoffMaximoSegundos);

            _logger.LogWarning(
                "Falha no envio da mensagem. MensagemGuid={MensagemGuid}, Canal={Canal}, Status={Status}, Tentativa={Tentativa}, Erro={Erro}",
                mensagem.Guid,
                mensagem.Canal,
                mensagem.Status,
                tentativa,
                resultado.MensagemErro);
        }

        _repository.Atualizar(mensagem);
    }
}
