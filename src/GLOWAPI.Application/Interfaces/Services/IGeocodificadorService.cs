using GLOWAPI.Application.Models.Geolocalizacao;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IGeocodificadorService
{
    Task<CoordenadaGeografica?> GeocodificarEnderecoAsync(
        EnderecoGeocodificacaoInput endereco,
        CancellationToken cancellationToken = default);

    Task<LocalizacaoReversa?> ReverseGeocodificarAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken = default);
}
