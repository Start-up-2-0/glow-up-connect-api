using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class ConviteNegocioDuplicadoException : DomainException
{
    public const string ErrorCode = "CONVITE_NEGOCIO_DUPLICADO";

    public ConviteNegocioDuplicadoException()
        : base("Ja existe um convite pendente para este destinatario no negocio.", ErrorCode)
    {
    }
}
