using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Equipe;

public record ProfissionalVitrineResponseDto(
    int Id,
    int EstabelecimentoId,
    int ProfissionalId,
    Guid PublicGuid,
    string NomePublico,
    string Biografia,
    string Logo,
    bool SomenteExibicao,
    bool Ativo)
{
    public static ProfissionalVitrineResponseDto From(
        ProfissionalEstabelecimento vinculo,
        Profissional profissional) =>
        new(
            vinculo.Id,
            vinculo.EstabelecimentoId,
            profissional.Id,
            profissional.PublicGuid,
            profissional.NomePublico,
            profissional.Biografia,
            profissional.Logo,
            vinculo.SomenteExibicao,
            vinculo.Ativo && profissional.Ativo);
}
