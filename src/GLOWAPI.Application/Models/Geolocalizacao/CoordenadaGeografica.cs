namespace GLOWAPI.Application.Models.Geolocalizacao;

public record CoordenadaGeografica(decimal Latitude, decimal Longitude);

public record LocalizacaoReversa(string Cidade, string Estado);
