using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Validators;

public static class HorarioIntervaloValidador
{
    public static void ValidarIntervalo(TimeOnly horaInicio, TimeOnly horaFim)
    {
        if (horaInicio >= horaFim)
        {
            throw new HorarioAtendimentoInvalidoException("A hora de inicio deve ser menor que a hora de fim.");
        }
    }

    public static bool IntervalosConflitam(
        TimeOnly horaInicio,
        TimeOnly horaFim,
        TimeOnly outroInicio,
        TimeOnly outroFim)
    {
        return horaInicio < outroFim && outroInicio < horaFim;
    }
}
