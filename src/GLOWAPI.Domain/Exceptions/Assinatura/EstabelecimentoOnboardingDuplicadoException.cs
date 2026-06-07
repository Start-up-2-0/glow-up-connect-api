using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

public class EstabelecimentoOnboardingDuplicadoException : DomainException
{
    public const string ErrorCode = "ESTABELECIMENTO_ONBOARDING_DUPLICADO";

    public EstabelecimentoOnboardingDuplicadoException()
        : base(
            "Voce ja possui um estabelecimento cadastrado. Use a assinatura vinculada a ele.",
            ErrorCode)
    {
    }
}
