using System.Diagnostics;
using System.Text.Json;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure.Mensageria;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Infrastructure.Mensageria.Provedores;

public class ProvedorMensagemWhatsApp : IProvedorMensagem
{
    private readonly HttpClient _httpClient;
    private readonly MensageriaWhatsAppOptions _options;
    private readonly ILogger<ProvedorMensagemWhatsApp> _logger;

    public ProvedorMensagemWhatsApp(
        HttpClient httpClient,
        IOptions<MensageriaWhatsAppOptions> options,
        ILogger<ProvedorMensagemWhatsApp> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public CanalMensagemNotificacao CanalSuportado => CanalMensagemNotificacao.WhatsApp;

    public async Task<ResultadoEnvioMensagem> EnviarAsync(
        MensagemNotificacao mensagem,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sw = Stopwatch.StartNew();

        var requestPayload = JsonSerializer.Serialize(new
        {
            mensagem.Destinatario,
            mensagem.Assunto,
            TextoLength = mensagem.Conteudo.Length,
            ApiVersion = _options.UsarApiV2 ? "v2" : "v1"
        });

        if (!_options.Habilitado
            || string.IsNullOrWhiteSpace(_options.ApiUrl)
            || string.IsNullOrWhiteSpace(_options.InstanceName))
        {
            _logger.LogInformation(
                "Stub WhatsApp enviado. MensagemGuid={MensagemGuid}, Destinatario={Destinatario}",
                mensagem.Guid,
                mensagem.Destinatario);

            sw.Stop();
            return new ResultadoEnvioMensagem(
                Sucesso: true,
                RequestPayload: requestPayload,
                ResponsePayload: """{"status":"stub_ok"}""",
                RespostaProvedor: "stub-whatsapp",
                MensagemErro: null,
                TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            sw.Stop();
            return new ResultadoEnvioMensagem(
                Sucesso: false,
                RequestPayload: requestPayload,
                ResponsePayload: null,
                RespostaProvedor: null,
                MensagemErro: "Mensageria:WhatsApp:ApiKey nao configurado.",
                TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
        }

        try
        {
            var candidatos = EvolutionDestinoHelper.CriarCandidatosDestinoOutboundDeMensagem(
                mensagem.Destinatario,
                mensagem.PayloadJson);

            if (candidatos.Count == 0)
            {
                sw.Stop();
                _logger.LogWarning(
                    "WhatsApp outbound ignorado: destinatario invalido para Evolution sendText. MensagemGuid={MensagemGuid}, Destinatario={Destinatario}",
                    mensagem.Guid,
                    mensagem.Destinatario);

                return new ResultadoEnvioMensagem(
                    Sucesso: false,
                    RequestPayload: requestPayload,
                    ResponsePayload: null,
                    RespostaProvedor: null,
                    MensagemErro: "Destinatario WhatsApp invalido para Evolution sendText.",
                    TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
            }

            var envio = new EvolutionWhatsAppTextoEnvio(
                _httpClient,
                Options.Create(_options),
                _logger);

            var (sucesso, responseBody, destinatarioUsado, formatoUsado) = await envio.EnviarAsync(
                candidatos,
                mensagem.Conteudo,
                cancellationToken);

            sw.Stop();

            if (!sucesso)
            {
                _logger.LogWarning(
                    "Evolution sendText falhou. MensagemGuid={MensagemGuid}, Destinatario={Destinatario}, Response={Response}",
                    mensagem.Guid,
                    destinatarioUsado,
                    responseBody);

                return new ResultadoEnvioMensagem(
                    Sucesso: false,
                    RequestPayload: requestPayload,
                    ResponsePayload: responseBody,
                    RespostaProvedor: null,
                    MensagemErro: "Evolution API nao confirmou entrega do texto.",
                    TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
            }

            _logger.LogInformation(
                "WhatsApp enviado via Evolution API. MensagemGuid={MensagemGuid}, Formato={Formato}",
                mensagem.Guid,
                formatoUsado);

            return new ResultadoEnvioMensagem(
                Sucesso: true,
                RequestPayload: requestPayload,
                ResponsePayload: responseBody,
                RespostaProvedor: $"evolution-whatsapp-{formatoUsado}",
                MensagemErro: null,
                TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Falha ao enviar WhatsApp via Evolution API. MensagemGuid={MensagemGuid}", mensagem.Guid);

            return new ResultadoEnvioMensagem(
                Sucesso: false,
                RequestPayload: requestPayload,
                ResponsePayload: ex.Message,
                RespostaProvedor: null,
                MensagemErro: ex.Message,
                TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
        }
    }
}
