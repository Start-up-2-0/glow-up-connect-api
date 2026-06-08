using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Convites;

public record ConviteNegocioCriadoResponseDto(
    int Id,
    int EstabelecimentoId,
    string Email,
    string TipoConvite,
    string RoleSugerida,
    string Status,
    DateTime ExpiraEm,
    DateTime CriadoEm,
    string LinkConvite)
{
    public static ConviteNegocioCriadoResponseDto From(ConviteNegocio convite, string linkConvite) =>
        new(
            convite.Id,
            convite.EstabelecimentoId,
            convite.Email,
            convite.TipoConvite.ToString(),
            convite.RoleSugerida.ToString(),
            convite.Status.ToString(),
            convite.ExpiraEm,
            convite.CriadoEm,
            linkConvite);
}
