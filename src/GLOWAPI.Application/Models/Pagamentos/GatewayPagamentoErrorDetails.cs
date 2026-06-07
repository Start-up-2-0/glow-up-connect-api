using System.Text.Json;
using System.Text.RegularExpressions;

namespace GLOWAPI.Application.Models.Pagamentos;

public record GatewayPagamentoErrorDetails(
    string Operacao,
    int? HttpStatusCode,
    string? GatewayMessage,
    object? GatewayResponse)
{
    private static readonly Regex HttpStatusPattern = new(
        @"retornou\s+(?<status>\d{3})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static GatewayPagamentoErrorDetails FromAssinaturaRecorrente(
        CriarAssinaturaRecorrenteGatewayResponse response) =>
        Criar("criar_assinatura_recorrente", response.MensagemErro, response.ResponsePayload);

    public static GatewayPagamentoErrorDetails FromCobranca(CriarCobrancaGatewayResponse response) =>
        Criar("criar_cobranca", response.MensagemErro, response.ResponsePayload);

    private static GatewayPagamentoErrorDetails Criar(
        string operacao,
        string? mensagemErro,
        string responsePayload)
    {
        var gatewayResponse = ParseGatewayResponse(responsePayload);
        var gatewayMessage = ExtrairMensagemGateway(gatewayResponse, responsePayload);

        return new GatewayPagamentoErrorDetails(
            Operacao: operacao,
            HttpStatusCode: ExtrairHttpStatus(mensagemErro),
            GatewayMessage: gatewayMessage,
            GatewayResponse: gatewayResponse);
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
        if (string.IsNullOrWhiteSpace(responsePayload) || responsePayload == "{}")
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

    private static string? ExtrairMensagemGateway(object? gatewayResponse, string responsePayload)
    {
        if (gatewayResponse is null)
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
