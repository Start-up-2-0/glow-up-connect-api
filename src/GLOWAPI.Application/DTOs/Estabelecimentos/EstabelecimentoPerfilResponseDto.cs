using GLOWAPI.Application.DTOs.Operacoes;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Estabelecimentos;

public record EstabelecimentoPerfilResponseDto(
    int Id,
    Guid PublicGuid,
    string Nome,
    string Logo,
    string Telefone,
    string Email,
    EnderecoOperacaoResponseDto Endereco)
{
    public static EstabelecimentoPerfilResponseDto From(Estabelecimento estabelecimento) =>
        new(
            estabelecimento.Id,
            estabelecimento.PublicGuid,
            estabelecimento.Nome,
            estabelecimento.Logo,
            estabelecimento.Telefone,
            estabelecimento.Email,
            EnderecoOperacaoResponseDto.From(estabelecimento.Endereco));
}
