using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Horarios;

public class HorarioProfissionalResponseDto
{
    public int Id { get; set; }
    public int EstabelecimentoId { get; set; }
    public int ProfissionalId { get; set; }
    public string DiaSemana { get; set; } = string.Empty;
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFim { get; set; }
    public bool Ativo { get; set; }

    public static HorarioProfissionalResponseDto From(HorarioAtendimentoProfissional horario)
    {
        return new HorarioProfissionalResponseDto
        {
            Id = horario.Id,
            EstabelecimentoId = horario.EstabelecimentoId!.Value,
            ProfissionalId = horario.ProfissionalId,
            DiaSemana = horario.DiaSemana.ToString(),
            HoraInicio = horario.HoraInicio,
            HoraFim = horario.HoraFim,
            Ativo = horario.Ativo
        };
    }
}
