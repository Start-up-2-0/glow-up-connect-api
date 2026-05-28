using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Operacoes;

public record EnderecoOperacaoResponseDto(
    string Cidade,
    string Estado,
    string Local)
{
    public static EnderecoOperacaoResponseDto From(Endereco? endereco) =>
        new(
            endereco?.Cidade ?? string.Empty,
            endereco?.Estado ?? string.Empty,
            endereco?.Logradouro ?? string.Empty);
}
