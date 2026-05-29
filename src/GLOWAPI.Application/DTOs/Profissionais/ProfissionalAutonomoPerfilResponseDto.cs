using GLOWAPI.Application.DTOs.Operacoes;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Profissionais;

public record ProfissionalAutonomoPerfilResponseDto(
    int Id,
    Guid PublicGuid,
    string NomePublico,
    string Logo,
    string Telefone,
    string Email,
    EnderecoOperacaoResponseDto Endereco)
{
    public static ProfissionalAutonomoPerfilResponseDto From(
        Profissional profissional,
        Estabelecimento? estabelecimento = null) =>
        new(
            profissional.Id,
            profissional.PublicGuid,
            profissional.NomePublico,
            profissional.Logo,
            profissional.Telefone,
            profissional.Email,
            EnderecoOperacaoResponseDto.From(estabelecimento?.Endereco));
}
