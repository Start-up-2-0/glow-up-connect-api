using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Caixa;

public record LancamentoCaixaResponseDto(
    int Id,
    int CaixaId,
    int? AgendamentoId,
    int? PagamentoId,
    int? ProfissionalId,
    string Tipo,
    decimal Valor,
    string Descricao,
    DateTime CriadoEm)
{
    public static LancamentoCaixaResponseDto From(LancamentoCaixa lancamento) =>
        new(
            lancamento.Id,
            lancamento.CaixaId,
            lancamento.AgendamentoId,
            lancamento.PagamentoId,
            lancamento.ProfissionalId,
            lancamento.Tipo.ToString(),
            lancamento.Valor,
            lancamento.Descricao,
            lancamento.CreateAd);
}
