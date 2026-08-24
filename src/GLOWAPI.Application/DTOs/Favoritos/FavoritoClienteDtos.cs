namespace GLOWAPI.Application.DTOs.Favoritos;

public record CriarFavoritoClienteRequestDto(
    Guid EstabelecimentoPublicGuid,
    Guid? ProfissionalPublicGuid);

public record FavoritoClienteResponseDto(
    int Id,
    Guid EstabelecimentoPublicGuid,
    string EstabelecimentoNome,
    string EstabelecimentoLogo,
    Guid? ProfissionalPublicGuid,
    string? ProfissionalNome,
    string? ProfissionalLogo,
    string Tipo,
    DateTime CriadoEm);
