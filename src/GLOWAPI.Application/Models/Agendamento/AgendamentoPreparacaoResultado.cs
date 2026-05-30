using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Models.Agendamento;

public class AgendamentoPreparacaoResultado
{
    public required Estabelecimento Estabelecimento { get; init; }
    public required Profissional Profissional { get; init; }
    public required IReadOnlyList<Servico> Servicos { get; init; }
    public required IReadOnlyList<ProfissionalServico> VinculosProfissionalServico { get; init; }
    public required DateTime Inicio { get; init; }
    public required DateTime Fim { get; init; }
    public required decimal ValorTotal { get; init; }
    public required int DuracaoTotalMinutos { get; init; }
    public required IReadOnlyList<AgendamentoItemPreparacao> Itens { get; init; }
    public string? ClienteNome { get; init; }
    public string? ClienteEmail { get; init; }
    public string? ClienteTelefone { get; init; }
    public int? UsuarioClienteId { get; init; }
}

public class AgendamentoItemPreparacao
{
    public required Servico Servico { get; init; }
    public required ProfissionalServico? Vinculo { get; init; }
    public required DateTime Inicio { get; init; }
    public required DateTime Fim { get; init; }
    public required decimal Valor { get; init; }
}
