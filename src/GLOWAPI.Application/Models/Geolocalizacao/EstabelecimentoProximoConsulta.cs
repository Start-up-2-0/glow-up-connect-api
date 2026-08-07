namespace GLOWAPI.Application.Models.Geolocalizacao;

/// <summary>
/// Projeção leve do marketplace — sem Logo/longtext.
/// </summary>
public record EstabelecimentoProximoConsulta(
    int Id,
    Guid PublicGuid,
    string Nome,
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
    bool DestaqueMarketplace);
