namespace GLOWAPI.Application.Models.Mensageria;

public record WhatsAppConfirmacaoInboundRespostaDestino(
    string Telefone,
    string? Nome,
    int? UsuarioId = null,
    int? EstabelecimentoId = null);
