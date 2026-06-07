using System.Text.Json;
using System.Text.RegularExpressions;
using GLOWAPI.Application.Services;

namespace GLOWAPI.Application.Models.Pagamentos;

public record GatewayPagamentoErrorDetails(
    string Operacao,
    int? HttpStatusCode,
    string? GatewayMessage,
    object? GatewayResponse,
    string? GatewayResponseRaw,
    bool RespostaVazia,
    string? RequestUri,
    IReadOnlyDictionary<string, string>? ResponseHeaders,
    object? RequestPayload)
{
    private static readonly Regex HttpStatusPattern = new(
        @"retornou\s+(?<status>\d{3})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static GatewayPagamentoErrorDetails FromAssinaturaRecorrente(
        CriarAssinaturaRecorrenteGatewayResponse response) =>
        Criar(
            "criar_assinatura_recorrente",
            response.MensagemErro,
            response.ResponsePayload,
            response.RequestPayload,
            response.FailureInfo);

    public static GatewayPagamentoErrorDetails FromCobranca(CriarCobrancaGatewayResponse response) =>
        Criar(
            "criar_cobranca",
            response.MensagemErro,
            response.ResponsePayload,
            response.RequestPayload,
            response.FailureInfo);

    private static GatewayPagamentoErrorDetails Criar(
        string operacao,
        string? mensagemErro,
        string responsePayload,
        string requestPayload,
        GatewayHttpFailureInfo? failureInfo)
    {
        var gatewayResponseRaw = string.IsNullOrWhiteSpace(responsePayload) ? string.Empty : responsePayload;
        var respostaVazia = string.IsNullOrWhiteSpace(responsePayload);
        var gatewayResponse = ParseGatewayResponse(responsePayload);
        var gatewayMessage = ExtrairMensagemGateway(responsePayload);

        return new GatewayPagamentoErrorDetails(
            Operacao: operacao,
            HttpStatusCode: failureInfo?.HttpStatusCode ?? ExtrairHttpStatus(mensagemErro),
            GatewayMessage: gatewayMessage,
            GatewayResponse: gatewayResponse,
            GatewayResponseRaw: gatewayResponseRaw,
            RespostaVazia: respostaVazia,
            RequestUri: failureInfo?.RequestUri,
            ResponseHeaders: failureInfo?.ResponseHeaders,
            RequestPayload: GatewayPagamentoRequestSanitizer.Sanitizar(requestPayload));
    }

    private static int? ExtrairHttpStatus(string? mensagemErro)
    {
        if (string.IsNullOrWhiteSpace(mensagemErro))
        {
            return null;
        }

        var match = HttpStatusPattern.Match(mensagemErro);
        return match.Success && int.TryParse(match.Groups["status"].Value, out var status)
            ? status
            : null;
    }

    private static object? ParseGatewayResponse(string responsePayload)
    {
        if (string.IsNullOrWhiteSpace(responsePayload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responsePayload);
            return JsonSerializer.Deserialize<object>(document.RootElement.GetRawText());
        }
        catch (JsonException)
        {
            return responsePayload;
        }
    }

    private static string? ExtrairMensagemGateway(string responsePayload)
    {
        if (string.IsNullOrWhiteSpace(responsePayload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responsePayload);
            var root = document.RootElement;
            if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
            {
                return message.GetString();
            }

            if (root.TryGetProperty("cause", out var cause)
                && cause.ValueKind == JsonValueKind.Array
                && cause.GetArrayLength() > 0
                && cause[0].TryGetProperty("description", out var description)
                && description.ValueKind == JsonValueKind.String)
            {
                return description.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
