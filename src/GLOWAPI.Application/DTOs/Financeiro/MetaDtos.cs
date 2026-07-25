using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Financeiro;

// Response
public record MetaResponseDto(
    int Id,
    string Nome,
    string TipoMeta,
    decimal ValorMeta,
    decimal PercentualComissao,
    bool Ativa,
    DateTime CreateAd,
    DateTime? UpdatedAt)
{
    public static MetaResponseDto From(Domain.Entities.Meta meta) => new(
        meta.Id,
        meta.Nome,
        meta.TipoMeta.ToString(),
        meta.ValorMeta,
        meta.PercentualComissao,
        meta.Ativa,
        meta.CreateAd,
        meta.UpdatedAt);
}

// Create request
public record CriarMetaRequestDto(
    string Nome,
    TipoMeta TipoMeta,
    decimal ValorMeta,
    decimal PercentualComissao);

// Update request
public record AtualizarMetaRequestDto(
    string Nome,
    TipoMeta TipoMeta,
    decimal ValorMeta,
    decimal PercentualComissao,
    bool Ativa);

// Per-professional progress against a Meta
public record MetaProgressoProfissionalDto(
    int ProfissionalEstabelecimentoId,
    int ProfissionalId,
    string NomePublico,
    string MetaNome,
    string TipoMeta,
    decimal ValorMeta,
    decimal PercentualComissao,
    int? QuantidadeRealizada,
    decimal? ValorRealizado,
    decimal PercentualProgresso,
    bool Atingida);
