using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Geolocalizacao;
using Microsoft.Extensions.Logging;

namespace GLOWAPI.Infrastructure.Geolocalizacao;

public class GeocodificadorCompostoService : IGeocodificadorService
{
    private readonly NominatimGeocodificadorClient _nominatim;
    private readonly PhotonGeocodificadorClient _photon;
    private readonly ILogger<GeocodificadorCompostoService> _logger;

    public GeocodificadorCompostoService(
        NominatimGeocodificadorClient nominatim,
        PhotonGeocodificadorClient photon,
        ILogger<GeocodificadorCompostoService> logger)
    {
        _nominatim = nominatim;
        _photon = photon;
        _logger = logger;
    }

    public async Task<CoordenadaGeografica?> GeocodificarEnderecoAsync(
        EnderecoGeocodificacaoInput endereco,
        CancellationToken cancellationToken = default)
    {
        var coordenada = await _nominatim.GeocodificarEnderecoAsync(endereco, cancellationToken);
        if (coordenada is not null)
        {
            return coordenada;
        }

        _logger.LogInformation(
            "Nominatim nao retornou coordenadas para {Cidade}/{Estado}. Tentando fallback Photon.",
            endereco.Cidade,
            endereco.Estado);

        return await _photon.GeocodificarEnderecoAsync(endereco, cancellationToken);
    }

    public Task<LocalizacaoReversa?> ReverseGeocodificarAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken = default) =>
        _nominatim.ReverseGeocodificarAsync(latitude, longitude, cancellationToken);
}
