using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class UsuarioEquipeNegocioNaoEncontradoException : DomainException
{
    public const string ErrorCode = "USUARIO_EQUIPE_NEGOCIO_NAO_ENCONTRADO";

    public UsuarioEquipeNegocioNaoEncontradoException()
        : base("Usuario da equipe nao encontrado ou inativo.", ErrorCode)
    {
    }
}
