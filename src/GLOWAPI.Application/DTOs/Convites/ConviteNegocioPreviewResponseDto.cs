using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Convites;

public record ConviteNegocioPreviewResponseDto(
    int EstabelecimentoId,
    string NomeEstabelecimento,
    string RoleSugerida,
    string Status,
    int LimiteUsuarios,
    int QuantidadeUtilizacoes,
    int VagasRestantes,
    DateTime ExpiraEm)
{
    public static ConviteNegocioPreviewResponseDto From(ConviteNegocio convite)
    {
        var vagas = Math.Max(0, convite.LimiteUsuarios - convite.QuantidadeUtilizacoes);
        return new(
            convite.EstabelecimentoId,
            convite.Estabelecimento?.Nome ?? string.Empty,
            convite.RoleSugerida.ToString(),
            convite.Status.ToString(),
            convite.LimiteUsuarios,
            convite.QuantidadeUtilizacoes,
            vagas,
            convite.ExpiraEm);
    }
}
