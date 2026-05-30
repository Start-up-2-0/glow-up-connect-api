using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Equipe;

public record ProfissionalEquipeResponseDto(
    int Id,
    int EstabelecimentoId,
    int ProfissionalId,
    int UsuarioId,
    string NomePublico,
    string Email,
    string Telefone,
    bool PodeReceberAgendamento,
    bool Ativo)
{
    public static ProfissionalEquipeResponseDto From(
        ProfissionalEstabelecimento vinculo,
        Profissional profissional) =>
        new(
            vinculo.Id,
            vinculo.EstabelecimentoId,
            profissional.Id,
            profissional.UsuarioId,
            profissional.NomePublico,
            profissional.Email,
            profissional.Telefone,
            vinculo.PodeReceberAgendamento,
            vinculo.Ativo);
}
