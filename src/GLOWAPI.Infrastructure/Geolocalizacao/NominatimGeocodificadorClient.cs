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
        EnderecoGeocodificacaoInput endereco,
        CancellationToken cancellationToken = default)
    {
        var consultaLivre = endereco.MontarConsultaLivre();

        var estruturada = await BuscarCoordenadasAsync(
            MontarUriEstruturada(endereco),
            cancellationToken);
        if (estruturada is not null)
        {
            return estruturada;
        }

        await RespeitarIntervaloNominatimAsync(cancellationToken);

        var livre = await BuscarCoordenadasAsync(
            $"search?q={Uri.EscapeDataString(consultaLivre)}&format=json&limit=1&countrycodes={_options.PaisPadrao}",
            cancellationToken);
        if (livre is not null)
        {
            return livre;
        }

        var consultaSimplificada = $"{endereco.Logradouro}, {endereco.Cidade}, {endereco.Estado}, Brasil";
        await RespeitarIntervaloNominatimAsync(cancellationToken);

        return await BuscarCoordenadasAsync(
            $"search?q={Uri.EscapeDataString(consultaSimplificada)}&format=json&limit=1&countrycodes={_options.PaisPadrao}",
            cancellationToken);
    }

    public async Task<LocalizacaoReversa?> ReverseGeocodificarAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url =
                $"reverse?lat={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&lon={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&format=json";
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

    private async Task<CoordenadaGeografica?> BuscarCoordenadasAsync(
        string relativePath,
        CancellationToken cancellationToken)
    {
        try
        {
            var resultados = await _httpClient.GetFromJsonAsync<List<NominatimSearchResult>>(relativePath, cancellationToken);
            return ParseCoordenada(resultados?.FirstOrDefault());
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException or InvalidOperationException)
        {
            _logger.LogDebug(ex, "Tentativa de geocodificacao Nominatim falhou para {Path}.", relativePath);
            return null;
        }
    }

    private static string MontarUriEstruturada(EnderecoGeocodificacaoInput endereco)
    {
        var street = string.Join(
            ' ',
            new[] { endereco.Numero.Trim(), endereco.Logradouro.Trim() }.Where(parte => !string.IsNullOrWhiteSpace(parte)));

        var query = new List<string>
        {
            $"street={Uri.EscapeDataString(street)}",
            $"city={Uri.EscapeDataString(endereco.Cidade.Trim())}",
            $"state={Uri.EscapeDataString(endereco.Estado.Trim())}",
            $"postalcode={Uri.EscapeDataString(FormatarCep(endereco.Cep))}",
            "country=Brazil",
            "format=json",
            "limit=1"
        };

        return $"search?{string.Join('&', query)}";
    }

    private static CoordenadaGeografica? ParseCoordenada(NominatimSearchResult? resultado)
    {
        if (resultado is null
            || !decimal.TryParse(
                resultado.Lat,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var latitude)
            || !decimal.TryParse(
                resultado.Lon,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var longitude))
        {
            return null;
        }

        return new CoordenadaGeografica(latitude, longitude);
    }

    private static string FormatarCep(string cep) =>
        cep.Length == 8 ? $"{cep[..5]}-{cep[5..]}" : cep;

    private static async Task RespeitarIntervaloNominatimAsync(CancellationToken cancellationToken) =>
        await Task.Delay(TimeSpan.FromMilliseconds(1100), cancellationToken);

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
