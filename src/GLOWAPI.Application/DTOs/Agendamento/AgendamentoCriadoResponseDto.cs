using GLOWAPI.Application.Helpers;
using GLOWAPI.Domain.Entities;
using AgendamentoEntity = GLOWAPI.Domain.Entities.Agendamento;

namespace GLOWAPI.Application.DTOs.Agendamento;

public class AgendamentoCriadoResponseDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
    public int DuracaoTotalMinutos { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public IReadOnlyList<AgendamentoItemCriadoResponseDto> Itens { get; set; } = [];

    public static AgendamentoCriadoResponseDto From(AgendamentoEntity agendamento)
    {
        var itens = agendamento.Itens.OrderBy(item => item.Inicio).ToList();

        return new AgendamentoCriadoResponseDto
        {
            Id = agendamento.Id,
            Status = agendamento.Status.ToString(),
            ValorTotal = agendamento.ValorTotal,
            DuracaoTotalMinutos = AgendamentoHorarioHelper.ObterDuracaoTotalMinutos(agendamento),
            Inicio = AgendamentoHorarioHelper.ObterInicio(agendamento),
            Fim = AgendamentoHorarioHelper.ObterFim(agendamento),
            Itens = itens.Select(AgendamentoItemCriadoResponseDto.From).ToList()
        };
    }
}

public class AgendamentoItemCriadoResponseDto
{
    public int Id { get; set; }
    public int ServicoId { get; set; }
    public string ServicoNome { get; set; } = string.Empty;
    public int ProfissionalId { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public decimal Valor { get; set; }
    public string Status { get; set; } = string.Empty;

    public static AgendamentoItemCriadoResponseDto From(AgendamentoItem item) =>
        new()
        {
            Id = item.Id,
            ServicoId = item.ServicoId,
            ServicoNome = item.Servico?.Nome ?? string.Empty,
            ProfissionalId = item.ProfissionalId,
            Inicio = item.Inicio,
            Fim = item.Fim,
            Valor = item.Valor,
            Status = item.Status.ToString()
        };
}
