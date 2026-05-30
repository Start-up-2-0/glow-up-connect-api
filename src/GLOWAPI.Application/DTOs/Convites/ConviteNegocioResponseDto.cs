using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Convites;

public record ConviteNegocioResponseDto(
    int Id,
    int EstabelecimentoId,
    string Email,
    string TipoConvite,
    string RoleSugerida,
    string Status,
    DateTime ExpiraEm,
    DateTime CriadoEm)
{
    public static ConviteNegocioResponseDto From(ConviteNegocio convite) =>
        new(
            convite.Id,
            convite.EstabelecimentoId,
            convite.Email,
            convite.TipoConvite.ToString(),
            convite.RoleSugerida.ToString(),
            convite.Status.ToString(),
            convite.ExpiraEm,
            convite.CriadoEm);
}
