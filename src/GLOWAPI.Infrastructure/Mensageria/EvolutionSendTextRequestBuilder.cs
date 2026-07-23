using System.Text.Encodings.Web;
using System.Text.Json;

namespace GLOWAPI.Infrastructure.Mensageria;

internal static class EvolutionSendTextRequestBuilder
{
    private static readonly JsonSerializerOptions V1Options = new()
    {
        PropertyNamingPolicy = null,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions V2Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string CriarBodyV1(string number, string text) =>
        JsonSerializer.Serialize(
            new EvolutionSendTextV1Request
            {
                Number = number,
                TextMessage = new EvolutionSendTextV1TextMessage { Text = text },
                Options = new EvolutionSendTextV1Options { LinkPreview = false }
            },
            V1Options);

    public static string CriarBodyV2(string number, string text) =>
        JsonSerializer.Serialize(new { number, text }, V2Options);
}
