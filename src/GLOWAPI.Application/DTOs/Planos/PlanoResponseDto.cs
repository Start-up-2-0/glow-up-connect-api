using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Planos;

public record PlanoResponseDto(
    int Id,
    string Nome,
    string Descricao,
    decimal Preco,
    string Periodo,
    int? LimiteProfissionais,
    int? LimiteServicos,
    int? LimiteAgendamentos,
    IReadOnlyList<string> Modulos)
{
    public static PlanoResponseDto From(Plano plano) =>
        new(
            plano.Id,
            plano.Nome,
            plano.Descricao,
            plano.Preco,
            plano.Periodo.ToString(),
            plano.LimiteProfissionais,
            plano.LimiteServicos,
            plano.LimiteAgendamentos,
            Array.Empty<string>());
}
