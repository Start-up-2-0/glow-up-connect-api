using GLOWAPI.Application.DTOs.Equipe;

namespace GLOWAPI.Application.DTOs.Convites;

public record ConviteOuVinculoResponseDto(
    string TipoResultado,
    string? LinkConvite,
    ConviteNegocioCriadoResponseDto? Convite,
    UsuarioEquipeResponseDto? VinculoUsuario,
    ProfissionalEquipeResponseDto? VinculoProfissional)
{
    public static ConviteOuVinculoResponseDto FromConvite(
        ConviteNegocioCriadoResponseDto convite) =>
        new("Convite", convite.LinkConvite, convite, null, null);

    public static ConviteOuVinculoResponseDto FromVinculoUsuario(
        UsuarioEquipeResponseDto vinculo) =>
        new("Vinculado", null, null, vinculo, null);

    public static ConviteOuVinculoResponseDto FromVinculoProfissional(
        ProfissionalEquipeResponseDto vinculo) =>
        new("Vinculado", null, null, null, vinculo);
}
