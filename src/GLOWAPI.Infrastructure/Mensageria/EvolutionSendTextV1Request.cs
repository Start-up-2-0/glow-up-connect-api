using System.Text.Json.Serialization;

namespace GLOWAPI.Infrastructure.Mensageria;

internal sealed class EvolutionSendTextV1Request
{
    [JsonPropertyName("number")]
    public string Number { get; init; } = string.Empty;

    [JsonPropertyName("textMessage")]
    public EvolutionSendTextV1TextMessage TextMessage { get; init; } = new();

    [JsonPropertyName("options")]
    public EvolutionSendTextV1Options Options { get; init; } = new();
}

internal sealed class EvolutionSendTextV1TextMessage
{
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;
}

internal sealed class EvolutionSendTextV1Options
{
    [JsonPropertyName("delay")]
    public int Delay { get; init; }

    [JsonPropertyName("presence")]
    public string Presence { get; init; } = "composing";

    [JsonPropertyName("linkPreview")]
    public bool LinkPreview { get; init; }
}
