namespace GLOWAPI.Domain.Exceptions.Usuario;

public class UsuarioNaoEncontradoException : DomainException
{
    public const string ErrorCode = "USUARIO_NAO_ENCONTRADO";

    public UsuarioNaoEncontradoException()
        : base("Usuário não encontrado ou inativo.", ErrorCode)
    {
    }
}
