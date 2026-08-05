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
    bool WhatsAppConfirmado,
    bool WhatsAppOptIn,
    bool WhatsAppPendenteConfirmacao,
    EnderecoOperacaoResponseDto Endereco,
    string? Descricao = null,
    int? CategoriaEstabelecimentoId = null,
    string? CategoriaEstabelecimento = null)
{
    public static EstabelecimentoPerfilResponseDto From(Estabelecimento estabelecimento) =>
        new(
            estabelecimento.Id,
            estabelecimento.PublicGuid,
            estabelecimento.Nome,
            estabelecimento.Logo,
            estabelecimento.Telefone,
            estabelecimento.Email,
            estabelecimento.WhatsAppConfirmadoEm.HasValue,
            estabelecimento.WhatsAppOptIn,
            estabelecimento.PendenteConfirmacaoWhatsApp(),
            EnderecoOperacaoResponseDto.From(estabelecimento.Endereco),
            estabelecimento.Descricao,
            estabelecimento.CategoriaEstabelecimentoId,
            estabelecimento.CategoriaEstabelecimento?.Nome);
}
