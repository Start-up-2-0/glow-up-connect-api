using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
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
            var destinatario = NormalizarDestinatarioEvolution(mensagem.Destinatario);
            if (string.IsNullOrWhiteSpace(destinatario))
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

            var url = $"{_options.ApiUrl.TrimEnd('/')}/message/sendText/{Uri.EscapeDataString(_options.InstanceName)}";
            var (response, responseBody) = await EnviarTextoEvolutionAsync(
                url,
                destinatario,
                mensagem.Conteudo,
                cancellationToken);

            if (!response.IsSuccessStatusCode
                && !_options.UsarApiV2
                && DeveTentarPayloadV2(responseBody))
            {
                _logger.LogInformation(
                    "Evolution sendText v1 falhou; tentando payload v2. MensagemGuid={MensagemGuid}",
                    mensagem.Guid);

                (response, responseBody) = await EnviarTextoEvolutionAsync(
                    url,
                    destinatario,
                    mensagem.Conteudo,
                    cancellationToken,
                    usarApiV2: true);
            }

            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Evolution sendText falhou. MensagemGuid={MensagemGuid}, StatusCode={StatusCode}, Destinatario={Destinatario}, Response={Response}",
                    mensagem.Guid,
                    (int)response.StatusCode,
                    destinatario,
                    responseBody);

                return new ResultadoEnvioMensagem(
                    Sucesso: false,
                    RequestPayload: requestPayload,
                    ResponsePayload: responseBody,
                    RespostaProvedor: null,
                    MensagemErro: $"Evolution API retornou {(int)response.StatusCode}.",
                    TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
            }

            _logger.LogInformation(
                "WhatsApp enviado via Evolution API. MensagemGuid={MensagemGuid}",
                mensagem.Guid);

            return new ResultadoEnvioMensagem(
                Sucesso: true,
                RequestPayload: requestPayload,
                ResponsePayload: responseBody,
                RespostaProvedor: "evolution-whatsapp",
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

    private async Task<(HttpResponseMessage Response, string Body)> EnviarTextoEvolutionAsync(
        string url,
        string destinatario,
        string conteudo,
        CancellationToken cancellationToken,
        bool? usarApiV2 = null)
    {
        var apiV2 = usarApiV2 ?? _options.UsarApiV2;

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("apikey", _options.ApiKey);
        request.Content = JsonContent.Create(
            apiV2
                ? (object)new { number = destinatario, text = conteudo }
                : new { number = destinatario, textMessage = new { text = conteudo } });

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return (response, responseBody);
    }

    private static bool DeveTentarPayloadV2(string responseBody) =>
        responseBody.Contains("requires property \"text\"", StringComparison.OrdinalIgnoreCase)
        || responseBody.Contains("textMessage", StringComparison.OrdinalIgnoreCase);

    private static string NormalizarDestinatarioEvolution(string destinatario)
    {
        if (string.IsNullOrWhiteSpace(destinatario))
        {
            return string.Empty;
        }

        if (EvolutionWebhookParser.EhRemoteJidLid(destinatario))
        {
            return destinatario.Trim();
        }

        if (destinatario.Contains('@', StringComparison.Ordinal))
        {
            var prefixo = destinatario.Split('@')[0];
            return TelefoneHelper.NormalizarParaWhatsApp(prefixo);
        }

        return TelefoneHelper.NormalizarParaWhatsApp(destinatario);
    }
}
