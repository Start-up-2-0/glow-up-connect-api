using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class ConviteNegocioIndisponivelException : DomainException
{
    public const string ErrorCode = "CONVITE_NEGOCIO_INDISPONIVEL";
    public const string MensagemPadrao =
        "Este convite expirou ou não está mais disponível. Solicite um novo convite ao administrador da loja.";

    public ConviteNegocioIndisponivelException()
        : base(MensagemPadrao, ErrorCode)
    {
    }
}
