namespace GLOWAPI.Application.Mensageria;

public static class ConfirmacaoWhatsAppTemplate
{
    public static string MensagemOutboundConfirmacao(string linkConfirmacao) =>
        $"Glow: confirme seu numero acessando {linkConfirmacao}";

    public static string InstrucaoApp(string linkConfirmacao) =>
        $"""
            Abra o link e toque em "Confirmar no WhatsApp". Envie a mensagem usando o numero cadastrado no perfil.
            {linkConfirmacao}
            """;

    public static string RespostaProcessandoConfirmacao(string nome) =>
        $"Ola {nome}! Estamos processando sua confirmacao de WhatsApp no Glow Up Connect. Aguarde um instante.";

    public static string RespostaProcessandoConfirmacaoGenerica() =>
        "Recebemos sua mensagem. Estamos processando sua confirmacao de WhatsApp no Glow Up Connect. Aguarde um instante.";

    public static string RespostaConfirmacaoSucesso(string nome) =>
        $"Ola {nome}! Confirmacao aprovada: seu WhatsApp foi confirmado no Glow Up Connect. Voce passara a receber alertas de agendamento.";

    public static string RespostaConfirmacaoJaRealizada(string nome) =>
        $"Ola {nome}! Seu WhatsApp ja esta confirmado no Glow Up Connect.";

    public static string RespostaConfirmacaoFalha(string nome) =>
        $"Ola {nome}! Nao conseguimos confirmar seu numero no Glow Up Connect. Verifique o link mais recente no WhatsApp ou e-mail, ou solicite uma nova confirmacao pelo app.";

    public static string RespostaConfirmacaoFalhaGenerica() =>
        "Nao conseguimos confirmar seu numero no Glow Up Connect. Verifique se o telefone esta cadastrado no perfil e use o link mais recente enviado por WhatsApp ou e-mail.";
}
