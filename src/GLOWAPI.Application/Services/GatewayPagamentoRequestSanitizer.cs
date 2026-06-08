using System.Text.Json;
using System.Text.Json.Nodes;

namespace GLOWAPI.Application.Services;

public static class GatewayPagamentoRequestSanitizer
{
    private static readonly string[] CamposSensiveis = ["card_token_id", "token"];

    public static object? Sanitizar(string? requestPayload)
    {
        if (string.IsNullOrWhiteSpace(requestPayload))
        {
            return null;
        }

        try
        {
            var node = JsonNode.Parse(requestPayload);
            if (node is null)
            {
                return null;
            }

            MascararCamposSensiveis(node);
            return JsonSerializer.Deserialize<object>(node.ToJsonString());
        }
        catch (JsonException)
        {
            return new { payloadInvalido = true };
        }
    }

    private static void MascararCamposSensiveis(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            foreach (var campo in CamposSensiveis)
            {
                if (obj.ContainsKey(campo))
                {
                    obj[campo] = "***";
                }
            }

            foreach (var property in obj.ToList())
            {
                if (property.Value is not null)
                {
                    MascararCamposSensiveis(property.Value);
                }
            }

            return;
        }

        if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                if (item is not null)
                {
                    MascararCamposSensiveis(item);
                }
            }
        }
    }
}
