using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Convites;

public record ConviteNegocioPreviewResponseDto(
    int EstabelecimentoId,
    string NomeEstabelecimento,
    string Email,
    string TipoConvite,
    string RoleSugerida,
    string Status,
    DateTime ExpiraEm)
{
    public static ConviteNegocioPreviewResponseDto From(ConviteNegocio convite) =>
        new(
            convite.EstabelecimentoId,
            convite.Estabelecimento?.Nome ?? string.Empty,
            convite.Email,
            convite.TipoConvite.ToString(),
            convite.RoleSugerida.ToString(),
            convite.Status.ToString(),
            convite.ExpiraEm);
}
