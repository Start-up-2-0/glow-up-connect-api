namespace GLOWAPI.Application.Models.Mensageria;

public enum WhatsAppConfirmacaoInboundTipo
{
    Usuario = 1,
    Estabelecimento = 2
}

public class WhatsAppConfirmacaoInboundResultado
{
    public bool Confirmado { get; init; }
    public WhatsAppConfirmacaoInboundTipo? Tipo { get; init; }
    public string NomeDestinatario { get; init; } = string.Empty;
    public string TelefoneResposta { get; init; } = string.Empty;
    public int? EstabelecimentoId { get; init; }

    public static WhatsAppConfirmacaoInboundResultado Ignorado() =>
        new() { Confirmado = false };

    public static WhatsAppConfirmacaoInboundResultado SucessoUsuario(string nome, string telefone) =>
        new()
        {
            Confirmado = true,
            Tipo = WhatsAppConfirmacaoInboundTipo.Usuario,
            NomeDestinatario = nome,
            TelefoneResposta = telefone
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
