using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

/// <summary>
/// Telefone do payload bate com o da conta, mas o WhatsApp ainda nao foi confirmado.
/// </summary>
public class TelefoneAssinaturaNaoConfirmadoException : DomainException
{
    public const string ErrorCode = "TELEFONE_NAO_CONFIRMADO";

    public TelefoneAssinaturaNaoConfirmadoException()
        : base(
            "Confirme o telefone da sua conta via WhatsApp antes de concluir a assinatura.",
            ErrorCode)
    {
    }
}
