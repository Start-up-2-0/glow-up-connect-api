namespace GLOWAPI.Application.Models.Mensageria;

public record ResultadoEnvioMensagem(
    bool Sucesso,
    string? RequestPayload,
    string? ResponsePayload,
    string? RespostaProvedor,
    string? MensagemErro,
    int TempoExecucaoMs);
