namespace GLOWAPI.Application.Helpers;

public static class AgendaSemPreferenciaHelper
{
    public const int ProfissionalIdEstabelecimento = 0;

    public static bool SlotEstabelecimento(int profissionalId) =>
        profissionalId == ProfissionalIdEstabelecimento;
}
