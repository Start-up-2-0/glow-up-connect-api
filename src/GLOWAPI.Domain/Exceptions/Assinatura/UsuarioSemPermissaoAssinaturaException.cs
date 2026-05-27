using GLOWAPI.Domain.Exceptions;

namespace GLOWAPI.Domain.Exceptions.Assinatura;

public class UsuarioSemPermissaoAssinaturaException : DomainException
{
    public const string ErrorCode = "USUARIO_SEM_PERMISSAO_ASSINATURA";

    public UsuarioSemPermissaoAssinaturaException()
        : base("Usuario nao possui permissao para iniciar assinatura desta operacao.", ErrorCode)
    {
    }
}
