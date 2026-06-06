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

    public static string RespostaProcessandoConfirmacao(string nome) =>
        $"Ola {nome}! Estamos processando sua confirmacao de WhatsApp no Glow Up Connect. Aguarde um instante.";

    public static string RespostaProcessandoConfirmacaoGenerica() =>
        "Recebemos sua mensagem. Estamos processando sua confirmacao de WhatsApp no Glow Up Connect. Aguarde um instante.";

    public static string RespostaConfirmacaoSucesso(string nome) =>
        $"Ola {nome}! Confirmacao aprovada: seu WhatsApp foi confirmado no Glow Up Connect. Voce passara a receber alertas de agendamento.";

    public static string RespostaConfirmacaoJaRealizada(string nome) =>
        $"Ola {nome}! Seu WhatsApp ja esta confirmado no Glow Up Connect.";

    public static string RespostaConfirmacaoFalha(string nome) =>
        $"Ola {nome}! Nao conseguimos confirmar seu numero no Glow Up Connect. Verifique o codigo mais recente no e-mail ou solicite uma nova confirmacao pelo app.";

    public static string RespostaConfirmacaoFalhaGenerica() =>
        "Nao conseguimos confirmar seu numero no Glow Up Connect. Verifique se o telefone esta cadastrado no perfil e use o codigo mais recente enviado por e-mail.";
}
