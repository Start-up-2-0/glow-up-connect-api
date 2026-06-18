using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.DTOs.Estabelecimentos;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class EstabelecimentoDescobertaService : IEstabelecimentoDescobertaService
{
    private const double RaioKmPadrao = 10d;
    private const double RaioKmMaximo = 50d;
    private const int TamanhoPaginaPadrao = 20;
    private const int TamanhoPaginaMaximo = 50;

    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IHorarioFuncionamentoEstabelecimentoRepository _horarioFuncionamentoRepository;
    private readonly IGeocodificadorService _geocodificadorService;

    public EstabelecimentoDescobertaService(
        IEstabelecimentoRepository estabelecimentoRepository,
        IHorarioFuncionamentoEstabelecimentoRepository horarioFuncionamentoRepository,
        IGeocodificadorService geocodificadorService)
    {
        _estabelecimentoRepository = estabelecimentoRepository;
        _horarioFuncionamentoRepository = horarioFuncionamentoRepository;
        _geocodificadorService = geocodificadorService;
    }

    public async Task<EstabelecimentosProximosPaginadoResponseDto> ListarProximosAsync(
        decimal latitude,
        decimal longitude,
        double? raioKm,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default)
    {
        ValidarCoordenadas(latitude, longitude);

        var raio = raioKm ?? RaioKmPadrao;
        if (raio <= 0 || raio > RaioKmMaximo)
        {
            throw new LocalizacaoClienteInvalidaException($"Raio deve estar entre 0 e {RaioKmMaximo} km.");
        }

        if (pagina < 1)
        {
            throw new LocalizacaoClienteInvalidaException("Pagina deve ser maior ou igual a 1.");
        }

        if (tamanhoPagina < 1)
        {
            tamanhoPagina = TamanhoPaginaPadrao;
        }

        if (tamanhoPagina > TamanhoPaginaMaximo)
        {
            tamanhoPagina = TamanhoPaginaMaximo;
        }

        var localizacao = await _geocodificadorService.ReverseGeocodificarAsync(latitude, longitude, cancellationToken);
        if (localizacao is null)
        {
            throw new LocalizacaoClienteInvalidaException();
        }

        var cidadeNormalizada = GeolocalizacaoHelper.NormalizarTextoLocalizacao(localizacao.Cidade);
        var (itens, total) = await _estabelecimentoRepository.ListarProximosAsync(
            cidadeNormalizada,
            localizacao.Estado,
            latitude,
            longitude,
            raio,
            pagina,
            tamanhoPagina,
            cancellationToken);

        return new EstabelecimentosProximosPaginadoResponseDto(
            localizacao.Cidade,
            localizacao.Estado,
            raio,
            total,
            itens.Select(Mapear).ToList());
    }

    public async Task<EstabelecimentoPublicoResponseDto> ObterPorPublicGuidAsync(
        Guid publicGuid,
        decimal? latitude,
        decimal? longitude,
        CancellationToken cancellationToken = default)
    {
        var estabelecimento = await _estabelecimentoRepository.ObterPorPublicGuidAsync(publicGuid, cancellationToken);
        if (estabelecimento is null || !estabelecimento.Ativo)
        {
            throw new NegocioNaoEncontradoException();
        }

        double? distanciaKm = null;
        if (latitude.HasValue && longitude.HasValue)
        {
            ValidarCoordenadas(latitude.Value, longitude.Value);
            if (estabelecimento.Endereco?.Latitude is not null && estabelecimento.Endereco.Longitude is not null)
            {
                distanciaKm = Math.Round(
                    GeolocalizacaoHelper.CalcularDistanciaKm(
                        latitude.Value,
                        longitude.Value,
                        estabelecimento.Endereco.Latitude.Value,
                        estabelecimento.Endereco.Longitude.Value),
                    2);
            }
        }

        EnderecoResumoDto? endereco = null;
        if (estabelecimento.Endereco is not null)
        {
            var end = estabelecimento.Endereco;
            endereco = new EnderecoResumoDto(end.Logradouro, end.Bairro, end.Cidade, end.Estado);
        }

        var horarios = await _horarioFuncionamentoRepository.ListarPorEstabelecimentoAsync(
            estabelecimento.Id,
            ativo: true,
            cancellationToken: cancellationToken);
        var (abertoAgora, horarioAbertura, horarioFechamento) =
            HorarioFuncionamentoPublicoHelper.ResolverParaHoje(horarios);

        return new EstabelecimentoPublicoResponseDto(
            estabelecimento.PublicGuid,
            estabelecimento.Nome,
            estabelecimento.Logo,
            TruncarDescricao(estabelecimento.Descricao),
            endereco,
            distanciaKm,
            estabelecimento.NotaMedia,
            estabelecimento.TotalAvaliacoes,
            abertoAgora,
            horarioAbertura,
            horarioFechamento);
    }

    private static void ValidarCoordenadas(decimal latitude, decimal longitude)
    {
        if (latitude is < -90m or > 90m)
        {
            throw new LocalizacaoClienteInvalidaException("Latitude deve estar entre -90 e 90.");
        }

        if (longitude is < -180m or > 180m)
        {
            throw new LocalizacaoClienteInvalidaException("Longitude deve estar entre -180 e 180.");
        }
    }

    private static EstabelecimentoProximoResponseDto Mapear(Models.Geolocalizacao.EstabelecimentoProximoConsulta consulta)
    {
        var estabelecimento = consulta.Estabelecimento;
        var endereco = estabelecimento.Endereco!;

        return new EstabelecimentoProximoResponseDto(
            estabelecimento.PublicGuid,
            estabelecimento.Nome,
            estabelecimento.Logo,
            TruncarDescricao(estabelecimento.Descricao),
            Math.Round(consulta.DistanciaKm, 2),
            consulta.DestaqueMarketplace,
            new EnderecoResumoDto(
                endereco.Logradouro,
                endereco.Bairro,
                endereco.Cidade,
                endereco.Estado),
            estabelecimento.NotaMedia,
            estabelecimento.TotalAvaliacoes);
    }

    private static string TruncarDescricao(string descricao) =>
        descricao.Length <= 160 ? descricao : descricao[..157] + "...";
}
