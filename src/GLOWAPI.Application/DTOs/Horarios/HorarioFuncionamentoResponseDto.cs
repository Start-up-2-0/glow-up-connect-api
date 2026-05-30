using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Horarios;

public class HorarioFuncionamentoResponseDto
{
    public int Id { get; set; }
    public int EstabelecimentoId { get; set; }
    public string DiaSemana { get; set; } = string.Empty;
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFim { get; set; }
    public bool Ativo { get; set; }

    public static HorarioFuncionamentoResponseDto From(HorarioFuncionamentoEstabelecimento horario)
    {
        return new HorarioFuncionamentoResponseDto
        {
            Id = horario.Id,
            EstabelecimentoId = horario.EstabelecimentoId,
            DiaSemana = horario.DiaSemana.ToString(),
            HoraInicio = horario.HoraInicio,
            HoraFim = horario.HoraFim,
            Ativo = horario.Ativo
        };
    }
}
