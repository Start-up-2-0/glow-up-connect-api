using System.Text.Json;
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

        if (candidatosDestino.Count == 0)
        {
            return (false, null, string.Empty, "destino-invalido");
        }

        var formatos = CriarFormatosPayload(conteudo).ToList();

        var ultimoBody = string.Empty;
        HttpResponseMessage? ultimaResponse = null;
        string ultimoDestino = candidatosDestino[0];
        string ultimoFormato = "nenhum";

        foreach (var candidato in candidatosDestino)
        {
            foreach (var (nomeFormato, payload) in formatos)
            {
                var url = $"{_options.ApiUrl.TrimEnd('/')}/message/sendText/{Uri.EscapeDataString(_options.InstanceName)}";
                var requestBody = JsonSerializer.Serialize(payload(candidato));

                _logger.LogInformation(
                    "Evolution sendText request. Url={Url}, Destinatario={Destinatario}, Formato={Formato}, Body={Body}",
                    url,
                    candidato,
                    nomeFormato,
                    requestBody);

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("apikey", _options.ApiKey);
                request.Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                ultimoBody = responseBody;
                ultimaResponse = response;
                ultimoDestino = candidato;
                ultimoFormato = nomeFormato;

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Evolution sendText tentativa falhou. StatusCode={StatusCode}, Destinatario={Destinatario}, Formato={Formato}, Response={Response}",
                        (int)response.StatusCode,
                        candidato,
                        nomeFormato,
                        responseBody);
                    continue;
                }

                if (ConsiderarSucessoAposHttpOk(nomeFormato, responseBody, conteudo))
                {
                    _logger.LogInformation(
                        "Evolution sendText ok. Destinatario={Destinatario}, Formato={Formato}, Response={Response}",
                        candidato,
                        nomeFormato,
                        responseBody);

                    return (true, responseBody, candidato, nomeFormato);
                }

                _logger.LogWarning(
                    "Evolution sendText retornou sucesso sem texto na resposta. Destinatario={Destinatario}, Formato={Formato}, Response={Response}",
                    candidato,
                    nomeFormato,
                    responseBody);
            }
        }

        var status = ultimaResponse is null ? 0 : (int)ultimaResponse.StatusCode;
        _logger.LogWarning(
            "Evolution sendText esgotou tentativas. UltimoStatus={StatusCode}, Destinatario={Destinatario}, Formato={Formato}, Response={Response}",
            status,
            ultimoDestino,
            ultimoFormato,
            ultimoBody);

        return (false, ultimoBody, ultimoDestino, ultimoFormato);
    }

    public Task<(bool Sucesso, string? ResponseBody, string DestinatarioUsado, string FormatoUsado)> EnviarAsync(
        string destinatarioBruto,
        string conteudo,
        CancellationToken cancellationToken) =>
        EnviarAsync(
            EvolutionDestinoHelper.CriarCandidatosDestinoOutbound(destinatarioBruto),
            conteudo,
            cancellationToken);

    private IEnumerable<(string Nome, Func<string, object> Payload)> CriarFormatosPayload(string conteudo)
    {
        if (_options.UsarApiV2)
        {
            yield return ("v2-text", destino => new { number = destino, text = conteudo });
            yield break;
        }

        yield return ("v1-textMessage", destino => new
        {
            number = destino,
            textMessage = new { text = conteudo }
        });
    }

    private static bool ConsiderarSucessoAposHttpOk(string nomeFormato, string responseBody, string conteudo)
    {
        if (nomeFormato.StartsWith("v1-textMessage", StringComparison.Ordinal))
        {
            return true;
        }

        return RespostaIndicaTextoEntregue(responseBody, conteudo);
    }

    private static bool RespostaIndicaTextoEntregue(string responseBody, string conteudo)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return false;
        }

        if (responseBody.Contains(conteudo, StringComparison.Ordinal))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            return ContemTextoNoJson(document.RootElement, conteudo);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool ContemTextoNoJson(JsonElement element, string conteudo)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if ((property.Name.Equals("text", StringComparison.OrdinalIgnoreCase)
                            || property.Name.Equals("conversation", StringComparison.OrdinalIgnoreCase))
                        && property.Value.ValueKind == JsonValueKind.String
                        && string.Equals(property.Value.GetString(), conteudo, StringComparison.Ordinal))
                    {
                        return true;
                    }

                    if (ContemTextoNoJson(property.Value, conteudo))
                    {
                        return true;
                    }
                }

                return false;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (ContemTextoNoJson(item, conteudo))
                    {
                        return true;
                    }
                }

                return false;
            default:
                return false;
        }
    }
}
