namespace GLOWAPI.Application.DTOs.Estabelecimentos;

public record EstabelecimentoPublicoResponseDto(
    Guid PublicGuid,
    string Nome,
    string Logo,
    string Descricao,
    EnderecoResumoDto? Endereco,
    double? DistanciaKm,
    decimal? NotaMedia = null,
    int TotalAvaliacoes = 0);
