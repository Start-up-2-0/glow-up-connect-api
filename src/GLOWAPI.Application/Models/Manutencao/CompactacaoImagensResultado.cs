namespace GLOWAPI.Application.Models.Manutencao;

public sealed class CompactacaoImagensResultado
{
    public int RegistrosProcessados { get; set; }
    public int RegistrosAtualizados { get; set; }
    public int RegistrosIgnorados { get; set; }
    public long BytesEconomizados { get; set; }

    public static CompactacaoImagensResultado Vazio => new();

    public void Somar(CompactacaoImagensResultado outro)
    {
        RegistrosProcessados += outro.RegistrosProcessados;
        RegistrosAtualizados += outro.RegistrosAtualizados;
        RegistrosIgnorados += outro.RegistrosIgnorados;
        BytesEconomizados += outro.BytesEconomizados;
    }
}
