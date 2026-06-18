using System.Text.Json.Serialization;

namespace GLOWAPI.Infrastructure.Mensageria;

internal sealed class EvolutionSendTextV1Request
{
    [JsonPropertyName("number")]
    public string Number { get; init; } = string.Empty;

    [JsonPropertyName("textMessage")]
    public EvolutionSendTextV1TextMessage TextMessage { get; init; } = new();
}

internal sealed class EvolutionSendTextV1TextMessage
{
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;
}
