using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Operacoes;

public record EnderecoOperacaoResponseDto(
    string Cep,
    string Logradouro,
    string Numero,
    string Bairro,
    string Cidade,
    string Estado,
    string Complemento,
    bool EnderecoCompleto)
{
    public static EnderecoOperacaoResponseDto From(Endereco? endereco) =>
        new(
            endereco?.Cep ?? string.Empty,
            endereco?.Logradouro ?? string.Empty,
            endereco?.Numero ?? string.Empty,
            endereco?.Bairro ?? string.Empty,
            endereco?.Cidade ?? string.Empty,
            endereco?.Estado ?? string.Empty,
            endereco?.Complemento ?? string.Empty,
            endereco is not null && OperacaoPerfilValidation.EnderecoPossuiCoordenadas(endereco));
}
