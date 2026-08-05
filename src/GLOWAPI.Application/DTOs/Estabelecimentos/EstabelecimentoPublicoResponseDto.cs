using System.Text.Json.Serialization;

namespace GLOWAPI.Application.DTOs.Estabelecimentos;

public record EstabelecimentoPublicoResponseDto(
    Guid PublicGuid,
    string Nome,
    string Logo,
    string Descricao,
    EnderecoResumoDto? Endereco,
    double? DistanciaKm,
    decimal? NotaMedia = null,
    int TotalAvaliacoes = 0,
    bool AbertoAgora = false,
    string? HorarioAbertura = null,
    string? HorarioFechamento = null,
    [property: JsonPropertyName("categoriaId")] int? CategoriaEstabelecimentoId = null,
    [property: JsonPropertyName("categoria")] string? CategoriaEstabelecimento = null);
