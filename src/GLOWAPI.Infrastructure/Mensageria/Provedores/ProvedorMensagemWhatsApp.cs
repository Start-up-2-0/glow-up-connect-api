using System.Diagnostics;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GLOWAPI.Infrastructure.Mensageria.Provedores;

public class ProvedorMensagemWhatsApp : IProvedorMensagem
{
    private readonly ILogger<ProvedorMensagemWhatsApp> _logger;

    public ProvedorMensagemWhatsApp(ILogger<ProvedorMensagemWhatsApp> logger)
    {
        _logger = logger;
    }

    public CanalMensagemNotificacao CanalSuportado => CanalMensagemNotificacao.WhatsApp;

    public Task<ResultadoEnvioMensagem> EnviarAsync(
        MensagemNotificacao mensagem,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sw = Stopwatch.StartNew();

        var request = JsonSerializer.Serialize(new
        {
            mensagem.Destinatario,
            mensagem.Conteudo
        });

        _logger.LogInformation(
            "Stub WhatsApp enviado. MensagemGuid={MensagemGuid}, Destinatario={Destinatario}",
            mensagem.Guid,
            mensagem.Destinatario);

        sw.Stop();

        return Task.FromResult(new ResultadoEnvioMensagem(
            Sucesso: true,
            RequestPayload: request,
            ResponsePayload: """{"status":"stub_ok"}""",
            RespostaProvedor: "stub-whatsapp",
            MensagemErro: null,
            TempoExecucaoMs: (int)sw.ElapsedMilliseconds));
    }
}
