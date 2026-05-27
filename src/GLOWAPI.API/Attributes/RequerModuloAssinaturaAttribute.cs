using GLOWAPI.Domain.Enums;

namespace GLOWAPI.API.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequerModuloAssinaturaAttribute : Attribute
{
    public RequerModuloAssinaturaAttribute(
        TipoAssinatura tipoAssinatura,
        ModuloAssinatura modulo,
        string parametroId)
    {
        TipoAssinatura = tipoAssinatura;
        Modulo = modulo;
        ParametroId = parametroId;
    }

    public TipoAssinatura TipoAssinatura { get; }
    public ModuloAssinatura Modulo { get; }
    public string ParametroId { get; }
}
