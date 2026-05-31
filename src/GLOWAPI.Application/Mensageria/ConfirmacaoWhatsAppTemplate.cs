namespace GLOWAPI.Application.Mensageria;

public static class ConfirmacaoWhatsAppTemplate
{
    public static string MensagemSugeridaInbound(string codigo) => $"GLOW {codigo}";

    public static string InstrucaoApp(string numeroPlataforma, string codigo, int validadeHoras)
    {
        var validadeTexto = validadeHoras == 1 ? "1 hora" : $"{validadeHoras} horas";
        var mensagem = MensagemSugeridaInbound(codigo);

        return $"""
            Envie a mensagem "{mensagem}" para o WhatsApp {numeroPlataforma} usando o numero cadastrado no perfil.
            Valido por {validadeTexto}.
            """;
    }

    public static string RespostaConfirmacaoSucesso(string nome) =>
        $"Ola {nome}! Seu WhatsApp foi confirmado no Glow Up Connect. Voce passara a receber alertas de agendamento.";
}
