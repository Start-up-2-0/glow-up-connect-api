using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Financeiro;

public record ComissaoProfissionalResponseDto(
    int Id,
    int ProfissionalEstabelecimentoId,
    int ProfissionalId,
    string NomePublico,
    string TipoComissao,
    decimal? Percentual,
    decimal? ValorFixo,
    bool Ativo,
    DateTime InicioVigencia,
    DateTime? FimVigencia)
{
    public static ComissaoProfissionalResponseDto From(
        ComissaoProfissional comissao,
        ProfissionalEstabelecimento vinculo) =>
        new(
            comissao.Id,
            comissao.ProfissionalEstabelecimentoId,
            vinculo.ProfissionalId,
            vinculo.Profissional?.NomePublico ?? "Profissional",
            comissao.TipoComissao.ToString(),
            comissao.Percentual,
            comissao.ValorFixo,
            comissao.Ativo,
            comissao.InicioVigencia,
            comissao.FimVigencia);
}
