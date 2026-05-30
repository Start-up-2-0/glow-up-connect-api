using GLOWAPI.Domain.Enums;

namespace GLOWAPI.API.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequerPermissaoNegocioAttribute : Attribute
{
    public RequerPermissaoNegocioAttribute(
        PermissaoNegocio permissao,
        string parametroId,
        bool parametroEhPublicGuid = false)
    {
        Permissao = permissao;
        ParametroId = parametroId;
        ParametroEhPublicGuid = parametroEhPublicGuid;
    }

    public PermissaoNegocio Permissao { get; }
    public string ParametroId { get; }
    public bool ParametroEhPublicGuid { get; }
}
