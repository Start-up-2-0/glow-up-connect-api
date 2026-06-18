using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Mensageria;

public class WebhookWhatsAppNaoAutorizadoException : DomainException
{
    public const string ErrorCode = "WEBHOOK_WHATSAPP_NAO_AUTORIZADO";

    public WebhookWhatsAppNaoAutorizadoException(string message)
        : base(message, ErrorCode)
    {
    }
}
