using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

/// <summary>
/// Telefone do onboarding diverge do telefone da conta.
/// O usuario deve atualizar e confirmar o numero na conta antes de assinar.
/// </summary>
public class TelefoneAssinaturaDivergenteException : DomainException
{
    public const string ErrorCode = "TELEFONE_DIVERGENTE_NAO_CONFIRMADO";

    public TelefoneAssinaturaDivergenteException()
        : base(
            "O telefone informado difere do cadastrado na conta. Atualize e confirme o numero via WhatsApp antes de concluir a assinatura.",
            ErrorCode)
    {
    }
}
