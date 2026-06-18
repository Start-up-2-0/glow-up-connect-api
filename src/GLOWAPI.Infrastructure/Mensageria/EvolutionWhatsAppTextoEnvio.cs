using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Infrastructure.Mensageria;

internal sealed class EvolutionWhatsAppTextoEnvio
{
    private readonly HttpClient _httpClient;
    private readonly MensageriaWhatsAppOptions _options;
    private readonly ILogger _logger;

    public EvolutionWhatsAppTextoEnvio(
        HttpClient httpClient,
        IOptions<MensageriaWhatsAppOptions> options,
        ILogger logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(bool Sucesso, string? ResponseBody, string DestinatarioUsado, string FormatoUsado)> EnviarAsync(
        IReadOnlyList<string> candidatosDestino,
        string conteudo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiUrl)
            || string.IsNullOrWhiteSpace(_options.InstanceName)
            || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return (false, null, string.Empty, "config-ausente");
        }

        if (candidatosDestino.Count == 0 || string.IsNullOrWhiteSpace(conteudo))
        {
            return (false, null, string.Empty, "destino-invalido");
        }

        var ultimoBody = string.Empty;
        HttpResponseMessage? ultimaResponse = null;
        string ultimoDestino = candidatosDestino[0];
        const string formato = "v1-textMessage";

        foreach (var candidato in candidatosDestino)
        {
            var url = $"{_options.ApiUrl.TrimEnd('/')}/message/sendText/{Uri.EscapeDataString(_options.InstanceName)}";
            var requestBody = _options.UsarApiV2
                ? EvolutionSendTextRequestBuilder.CriarBodyV2(candidato, conteudo)
                : EvolutionSendTextRequestBuilder.CriarBodyV1(candidato, conteudo);

            _logger.LogInformation(
                "Evolution sendText request. Url={Url}, Destinatario={Destinatario}, Formato={Formato}, Body={Body}",
                url,
                candidato,
                _options.UsarApiV2 ? "v2-text" : formato,
                requestBody);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.TryAddWithoutValidation("apiKey", _options.ApiKey);
            request.Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            ultimoBody = responseBody;
            ultimaResponse = response;
            ultimoDestino = candidato;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Evolution sendText tentativa falhou. StatusCode={StatusCode}, Destinatario={Destinatario}, Formato={Formato}, Response={Response}",
                    (int)response.StatusCode,
                    candidato,
                    formato,
                    responseBody);
                continue;
            }

            _logger.LogInformation(
                "Evolution sendText ok. Destinatario={Destinatario}, Formato={Formato}, Response={Response}",
                candidato,
                formato,
                responseBody);

            return (true, responseBody, candidato, formato);
        }

        var status = ultimaResponse is null ? 0 : (int)ultimaResponse.StatusCode;
        _logger.LogWarning(
            "Evolution sendText esgotou tentativas. UltimoStatus={StatusCode}, Destinatario={Destinatario}, Formato={Formato}, Response={Response}",
            status,
            ultimoDestino,
            formato,
            ultimoBody);

        return (false, ultimoBody, ultimoDestino, formato);
    }

    public Task<(bool Sucesso, string? ResponseBody, string DestinatarioUsado, string FormatoUsado)> EnviarAsync(
        string destinatarioBruto,
        string conteudo,
        CancellationToken cancellationToken) =>
        EnviarAsync(
            EvolutionDestinoHelper.CriarCandidatosDestinoOutbound(destinatarioBruto),
            conteudo,
            cancellationToken);
}
