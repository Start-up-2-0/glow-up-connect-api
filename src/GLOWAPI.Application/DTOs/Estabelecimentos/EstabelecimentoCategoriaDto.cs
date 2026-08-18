using System.Text.Json.Serialization;

namespace GLOWAPI.Application.DTOs.Estabelecimentos;

/// <summary>Catálogo de categorias do marketplace (exibição + filtros).</summary>
public record EstabelecimentoCategoriaDto(
    int Id,
    string Nome,
    [property: JsonPropertyName("tipoAssinatura")] string TipoAssinatura);
