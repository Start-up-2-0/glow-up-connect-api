namespace GLOWAPI.Application.Models.Mensageria;

public enum WhatsAppConfirmacaoInboundTipo
{
    Usuario = 1,
    Estabelecimento = 2
}

public enum WhatsAppConfirmacaoInboundMotivoIgnorado
{
    Nenhum = 0,
    TelefoneInvalido,
    EntidadeNaoEncontrada,
    JaConfirmado,
    SemPendencia,
    CodigoInvalido
}

public class WhatsAppConfirmacaoInboundResultado
{
    public bool Confirmado { get; init; }
    public WhatsAppConfirmacaoInboundTipo? Tipo { get; init; }
    public WhatsAppConfirmacaoInboundMotivoIgnorado MotivoIgnorado { get; init; }
    public string NomeDestinatario { get; init; } = string.Empty;
    public string TelefoneResposta { get; init; } = string.Empty;
    public string EmailDestinatario { get; init; } = string.Empty;
    public int? UsuarioId { get; init; }
    public int? EstabelecimentoId { get; init; }

    public bool PossuiContatoIdentificado =>
        !string.IsNullOrWhiteSpace(NomeDestinatario);

    public static WhatsAppConfirmacaoInboundResultado Ignorado(
        WhatsAppConfirmacaoInboundMotivoIgnorado motivo = WhatsAppConfirmacaoInboundMotivoIgnorado.Nenhum,
        string? nomeDestinatario = null,
        string? telefoneResposta = null,
        string? emailDestinatario = null,
        int? usuarioId = null,
        int? estabelecimentoId = null) =>
        new()
        {
            Confirmado = false,
            MotivoIgnorado = motivo,
            NomeDestinatario = nomeDestinatario ?? string.Empty,
            TelefoneResposta = telefoneResposta ?? string.Empty,
            EmailDestinatario = emailDestinatario ?? string.Empty,
            UsuarioId = usuarioId,
            EstabelecimentoId = estabelecimentoId
        };

    public static WhatsAppConfirmacaoInboundResultado SucessoUsuario(string nome, string telefone, int usuarioId) =>
        new()
        {
            Confirmado = true,
            Tipo = WhatsAppConfirmacaoInboundTipo.Usuario,
            NomeDestinatario = nome,
            TelefoneResposta = telefone,
            UsuarioId = usuarioId
        };

    public static WhatsAppConfirmacaoInboundResultado SucessoEstabelecimento(
        string nome,
        string telefone,
        int estabelecimentoId) =>
        new()
        {
            Confirmado = true,
            Tipo = WhatsAppConfirmacaoInboundTipo.Estabelecimento,
            NomeDestinatario = nome,
            TelefoneResposta = telefone,
            EstabelecimentoId = estabelecimentoId
        };
}
