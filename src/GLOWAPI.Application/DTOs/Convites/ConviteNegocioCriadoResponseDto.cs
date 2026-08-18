using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Convites;

public record ConviteNegocioCriadoResponseDto(
    int Id,
    int EstabelecimentoId,
    string RoleSugerida,
    string TipoConvite,
    string Status,
    int LimiteUsuarios,
    int QuantidadeUtilizacoes,
    DateTime ExpiraEm,
    DateTime CriadoEm,
    string LinkConvite)
{
    public static ConviteNegocioCriadoResponseDto From(ConviteNegocio convite, string linkConvite) =>
        new(
            convite.Id,
            convite.EstabelecimentoId,
            convite.RoleSugerida.ToString(),
            convite.TipoConvite.ToString(),
            convite.Status.ToString(),
            convite.LimiteUsuarios,
            convite.QuantidadeUtilizacoes,
            convite.ExpiraEm,
            convite.CriadoEm,
            linkConvite);
}
