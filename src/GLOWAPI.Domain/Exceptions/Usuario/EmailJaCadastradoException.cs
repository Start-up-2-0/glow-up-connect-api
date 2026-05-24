namespace GLOWAPI.Domain.Exceptions.Usuario;

public class EmailJaCadastradoException : DomainException
{
    public const string ErrorCode = "EMAIL_JA_CADASTRADO";

    public EmailJaCadastradoException()
        : base("Usuário com este email já existe.", ErrorCode)
    {
    }
}
