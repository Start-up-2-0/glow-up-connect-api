using System.Diagnostics;
using System.Text.Json;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Infrastructure.Mensageria;

public class WhatsAppEnvioImediatoService : IWhatsAppEnvioImediatoService
{
    private readonly HttpClient _httpClient;
    private readonly MensageriaWhatsAppOptions _options;
    private readonly ILogger<WhatsAppEnvioImediatoService> _logger;

    public WhatsAppEnvioImediatoService(
        HttpClient httpClient,
        IOptions<MensageriaWhatsAppOptions> options,
        ILogger<WhatsAppEnvioImediatoService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ResultadoEnvioMensagem> EnviarTextoAsync(
        string destinatario,
        string conteudo,
        CancellationToken cancellationToken = default,
        string? remoteJidConversa = null,
        string? remoteJidAlt = null,
        EvolutionWhatsAppContextoResposta? contextoResposta = null)
    {
        var sw = Stopwatch.StartNew();
        var candidatos = EvolutionDestinoHelper.CriarCandidatosDestinoOutbound(
            destinatario,
            remoteJidConversa,
            remoteJidAlt);

        var requestPayload = JsonSerializer.Serialize(new
        {
            Destinatario = destinatario,
            Candidatos = candidatos,
            TextoLength = conteudo.Length,
            ApiVersion = _options.UsarApiV2 ? "v2" : "v1",
            TemQuoted = contextoResposta?.TemQuoted == true,
            RemoteJidConversa = remoteJidConversa
        });

        if (!_options.Habilitado)
        {
            sw.Stop();
            return new ResultadoEnvioMensagem(
                Sucesso: false,
                RequestPayload: requestPayload,
                ResponsePayload: """{"status":"whatsapp_desabilitado"}""",
                RespostaProvedor: null,
                MensagemErro: "Mensageria:WhatsApp nao habilitado.",
                TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
        }

        var envio = new EvolutionWhatsAppTextoEnvio(
            _httpClient,
            Options.Create(_options),
            _logger);

        var (sucesso, responseBody, _, formatoUsado) = await envio.EnviarAsync(
            candidatos,
            conteudo,
            contextoResposta,
            cancellationToken);

        sw.Stop();

        if (!sucesso)
        {
            return new ResultadoEnvioMensagem(
                Sucesso: false,
                RequestPayload: requestPayload,
                ResponsePayload: responseBody,
                RespostaProvedor: null,
                MensagemErro: "Evolution API nao confirmou entrega do texto.",
                TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
        }

        return new ResultadoEnvioMensagem(
            Sucesso: true,
            RequestPayload: requestPayload,
            ResponsePayload: responseBody,
            RespostaProvedor: $"evolution-whatsapp-{formatoUsado}",
            MensagemErro: null,
            TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
    }
}
