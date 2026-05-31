namespace GLOWAPI.Domain.Exceptions.Usuario;

public class ConfirmacaoWhatsAppInvalidaException : DomainException
{
    public const string ErrorCode = "CONFIRMACAO_WHATSAPP_INVALIDA";

    public ConfirmacaoWhatsAppInvalidaException()
        : base("Confirmacao de WhatsApp invalida ou expirada.", ErrorCode)
    {
    }
}
