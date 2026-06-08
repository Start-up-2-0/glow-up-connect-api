using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GLOWAPI.Application.Services;

public class EnderecoGeocodificacaoService : IEnderecoGeocodificacaoService
{
    private readonly IGeocodificadorService _geocodificadorService;
    private readonly ILogger<EnderecoGeocodificacaoService> _logger;

    public EnderecoGeocodificacaoService(
        IGeocodificadorService geocodificadorService,
        ILogger<EnderecoGeocodificacaoService> logger)
    {
        _geocodificadorService = geocodificadorService;
        _logger = logger;
    }

    public async Task TentarGeocodificarAsync(Endereco endereco, CancellationToken cancellationToken = default)
    {
        if (!OperacaoPerfilValidation.EnderecoEstaCompletoParaGeocodificacao(endereco))
        {
            endereco.Latitude = null;
            endereco.Longitude = null;
            endereco.GeocodificadoEm = null;
            return;
        }

        try
        {
            var enderecoFormatado = OperacaoPerfilValidation.MontarEnderecoGeocodificacao(endereco);
            var coordenada = await _geocodificadorService.GeocodificarEnderecoAsync(enderecoFormatado, cancellationToken);

            if (coordenada is null)
            {
                endereco.Latitude = null;
                endereco.Longitude = null;
                endereco.GeocodificadoEm = null;
                _logger.LogWarning(
                    "Geocodificacao nao retornou coordenadas para endereco do estabelecimento {EstabelecimentoId}.",
                    endereco.EstabelecimentoId);
                return;
            }

            endereco.Latitude = coordenada.Latitude;
            endereco.Longitude = coordenada.Longitude;
            endereco.GeocodificadoEm = DateTime.UtcNow;
            return;
        }
        catch (Exception ex)
        {
            endereco.Latitude = null;
            endereco.Longitude = null;
            endereco.GeocodificadoEm = null;
            _logger.LogWarning(
                ex,
                "Falha ao geocodificar endereco do estabelecimento {EstabelecimentoId}.",
                endereco.EstabelecimentoId);
            return;
        }
    }
}
