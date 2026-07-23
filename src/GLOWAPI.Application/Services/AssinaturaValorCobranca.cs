namespace GLOWAPI.Application.Services;

public static class AssinaturaValorCobranca
{
    public static decimal CalcularMensalidade(decimal precoPlano, decimal? percentualDescontoPermanente)
    {
        if (percentualDescontoPermanente is null or <= 0)
        {
            return precoPlano;
        }

        if (percentualDescontoPermanente >= 100)
        {
            return 0m;
        }

        return Math.Round(precoPlano * (1 - percentualDescontoPermanente.Value / 100m), 2, MidpointRounding.AwayFromZero);
    }
}
