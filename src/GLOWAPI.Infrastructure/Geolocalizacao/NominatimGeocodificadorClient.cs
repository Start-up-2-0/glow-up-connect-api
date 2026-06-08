using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Geolocalizacao;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Infrastructure.Geolocalizacao;

public class NominatimGeocodificadorClient : IGeocodificadorService
{
    private readonly HttpClient _httpClient;
    private readonly GeocodificacaoOptions _options;
    private readonly ILogger<NominatimGeocodificadorClient> _logger;

    public NominatimGeocodificadorClient(
        HttpClient httpClient,
        IOptions<GeocodificacaoOptions> options,
        ILogger<NominatimGeocodificadorClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CoordenadaGeografica?> GeocodificarEnderecoAsync(
        string enderecoFormatado,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(enderecoFormatado))
        {
            return null;
        }

        try
        {
            var url = CriarRequestUri(
                $"/search?q={Uri.EscapeDataString(enderecoFormatado)}&format=json&limit=1&countrycodes={_options.PaisPadrao}");
            var resultados = await _httpClient.GetFromJsonAsync<List<NominatimSearchResult>>(url, cancellationToken);

            var primeiro = resultados?.FirstOrDefault();
            if (primeiro is null
                || !decimal.TryParse(primeiro.Lat, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var latitude)
                || !decimal.TryParse(primeiro.Lon, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var longitude))
            {
                return null;
            }

            return new CoordenadaGeografica(latitude, longitude);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Falha ao geocodificar endereco via Nominatim.");
            return null;
        }
    }

    public async Task<LocalizacaoReversa?> ReverseGeocodificarAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = CriarRequestUri(
                $"/reverse?lat={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&format=json");
            var resultado = await _httpClient.GetFromJsonAsync<NominatimReverseResult>(url, cancellationToken);
            if (resultado?.Address is null)
            {
                return null;
            }

            var cidade = ObterCidade(resultado.Address);
            var estado = ObterEstado(resultado.Address);

            if (string.IsNullOrWhiteSpace(cidade) || string.IsNullOrWhiteSpace(estado))
            {
                return null;
            }

            return new LocalizacaoReversa(
                cidade.Trim(),
                estado.Trim().ToUpperInvariant());
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Falha no reverse geocode via Nominatim.");
            return null;
        }
    }

    private Uri CriarRequestUri(string path)
    {
        var relativePath = path.TrimStart('/');
        var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://nominatim.openstreetmap.org"
            : _options.BaseUrl.Trim().TrimEnd('/');
        return new Uri($"{baseUrl}/{relativePath}");
    }

    private static string? ObterCidade(NominatimAddress address) =>
        address.City
        ?? address.Town
        ?? address.Village
        ?? address.Municipality
        ?? address.County;

    private static string? ObterEstado(NominatimAddress address)
    {
        if (!string.IsNullOrWhiteSpace(address.StateCode))
        {
            var codigo = address.StateCode.Trim();
            if (codigo.Contains('-', StringComparison.Ordinal))
            {
                var uf = codigo.Split('-').Last();
                if (uf.Length == 2)
                {
                    return uf.ToUpperInvariant();
                }
            }

            if (codigo.Length == 2)
            {
                return codigo.ToUpperInvariant();
            }
        }

        if (string.IsNullOrWhiteSpace(address.State))
        {
            return null;
        }

        var estado = address.State.Trim();
        return estado.Length >= 2 ? estado[..2].ToUpperInvariant() : null;
    }

    private sealed class NominatimSearchResult
    {
        [JsonPropertyName("lat")]
        public string Lat { get; set; } = string.Empty;

        [JsonPropertyName("lon")]
        public string Lon { get; set; } = string.Empty;
    }

    private sealed class NominatimReverseResult
    {
        [JsonPropertyName("address")]
        public NominatimAddress? Address { get; set; }
    }

    private sealed class NominatimAddress
    {
        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("town")]
        public string? Town { get; set; }

        [JsonPropertyName("village")]
        public string? Village { get; set; }

        [JsonPropertyName("municipality")]
        public string? Municipality { get; set; }

        [JsonPropertyName("county")]
        public string? County { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("ISO3166-2-lvl4")]
        public string? StateCode { get; set; }
    }
}
