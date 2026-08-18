using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Models.Geolocalizacao;

/// <summary>
/// Projeção do marketplace. Logo vem do banco e é miniaturizada na camada de serviço.
/// </summary>
public record EstabelecimentoProximoConsulta(
    int Id,
    Guid PublicGuid,
    string Nome,
    string Logo,
    string Descricao,
    decimal? NotaMedia,
    int TotalAvaliacoes,
    int? CategoriaEstabelecimentoId,
    string? CategoriaNome,
    string Logradouro,
    string Bairro,
    string Cidade,
    string Estado,
    decimal Latitude,
    decimal Longitude,
    double DistanciaKm,
    bool DestaqueMarketplace,
    TipoAssinatura TipoAssinatura);
