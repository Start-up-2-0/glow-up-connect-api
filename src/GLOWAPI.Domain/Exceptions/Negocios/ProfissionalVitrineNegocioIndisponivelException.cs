using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class ProfissionalVitrineNegocioIndisponivelException : DomainException
{
    public const string ErrorCode = "PROFISSIONAL_VITRINE_INDISPONIVEL";

    public ProfissionalVitrineNegocioIndisponivelException()
        : base("Profissionais de vitrine estao disponiveis apenas no plano Basic.", ErrorCode)
    {
    }
}
