namespace GLOWAPI.Application.Helpers;

/// <summary>
/// Convenção Glow: horários de agenda são relógio de parede do estabelecimento com <see cref="DateTimeKind.Utc"/>.
/// </summary>
public static class AgendaDateTimeHelper
{
    public static DateTime ComporInicioWallClockUtc(DateOnly data, TimeOnly horario) =>
        data.ToDateTime(horario, DateTimeKind.Utc);

    /// <summary>
    /// Extrai data/hora do timestamp sem conversão de fuso (componentes do valor recebido).
    /// </summary>
    public static DateTime ExtrairWallClockUtc(DateTime value) =>
        new(
            value.Year,
            value.Month,
            value.Day,
            value.Hour,
            value.Minute,
            value.Second,
            DateTimeKind.Utc);

    /// <summary>
    /// Prioriza o timestamp do slot selecionado quando enviado pelo cliente.
    /// </summary>
    public static DateTime ResolverInicio(DateOnly data, TimeOnly horarioInicio, DateTime? inicioSelecionado)
    {
        if (inicioSelecionado.HasValue)
        {
            return ExtrairWallClockUtc(inicioSelecionado.Value);
        }

        return ComporInicioWallClockUtc(data, horarioInicio);
    }
}
