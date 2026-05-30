using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Negocios;

public class UsuarioEquipeNegocioDuplicadoException : DomainException
{
    public const string ErrorCode = "USUARIO_EQUIPE_NEGOCIO_DUPLICADO";

    public UsuarioEquipeNegocioDuplicadoException()
        : base("Usuario ja possui vinculo ativo com este negocio.", ErrorCode)
    {
    }
}
