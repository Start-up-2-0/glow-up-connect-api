using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GLOWAPI.Application.Models.Geolocalizacao;
using Microsoft.Extensions.Logging;

namespace GLOWAPI.Infrastructure.Geolocalizacao;

public class PhotonGeocodificadorClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PhotonGeocodificadorClient> _logger;

    public PhotonGeocodificadorClient(
        HttpClient httpClient,
        ILogger<PhotonGeocodificadorClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CoordenadaGeografica?> GeocodificarEnderecoAsync(
        EnderecoGeocodificacaoInput endereco,
        CancellationToken cancellationToken = default)
    {
        var coordenada = await BuscarAsync(endereco.MontarConsultaLivre(), cancellationToken);
        if (coordenada is not null)
        {
            return coordenada;
        }

        var consultaSimplificada = $"{endereco.Logradouro}, {endereco.Numero}, {endereco.Cidade}, {endereco.Estado}, Brasil";
        return await BuscarAsync(consultaSimplificada, cancellationToken);
    }

    private async Task<CoordenadaGeografica?> BuscarAsync(
        string consulta,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(consulta))
        {
            return null;
        }

        try
        {
            var url = $"api/?q={Uri.EscapeDataString(consulta)}&limit=1&lang=default";
            var resultado = await _httpClient.GetFromJsonAsync<PhotonResponse>(url, cancellationToken);
            var feature = resultado?.Features?.FirstOrDefault();
            var coordinates = feature?.Geometry?.Coordinates;
            if (coordinates is null || coordinates.Count < 2)
            {
                return null;
            }

            var longitude = coordinates[0];
            var latitude = coordinates[1];
            return new CoordenadaGeografica(latitude, longitude);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException or InvalidOperationException)
        {
            _logger.LogDebug(ex, "Tentativa de geocodificacao Photon falhou.");
            return null;
        }
    }

    private sealed class PhotonResponse
    {
        [JsonPropertyName("features")]
        public List<PhotonFeature>? Features { get; set; }
    }

    private sealed class PhotonFeature
    {
        [JsonPropertyName("geometry")]
        public PhotonGeometry? Geometry { get; set; }
    }

    private sealed class PhotonGeometry
    {
        [JsonPropertyName("coordinates")]
        public List<decimal>? Coordinates { get; set; }
    }
}
