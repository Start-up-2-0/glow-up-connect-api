namespace GLOWAPI.Domain.Exceptions.Usuario;

public class AvatarInvalidoException : DomainException
{
    public const string ErrorCode = "AVATAR_INVALIDO";

    public AvatarInvalidoException(string? mensagem = null)
        : base(mensagem ?? "Avatar invalido.", ErrorCode)
    {
    }
}
