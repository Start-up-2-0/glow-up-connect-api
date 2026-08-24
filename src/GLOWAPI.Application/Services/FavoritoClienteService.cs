using GLOWAPI.Application.DTOs.Favoritos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class FavoritoClienteService : IFavoritoClienteService
{
    private readonly IFavoritoClienteRepository _favoritoRepository;
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IProfissionalEstabelecimentoRepository _vinculoRepository;
    private readonly ICurrentUserContext _currentUser;

    public FavoritoClienteService(
        IFavoritoClienteRepository favoritoRepository,
        IEstabelecimentoRepository estabelecimentoRepository,
        IProfissionalRepository profissionalRepository,
        IProfissionalEstabelecimentoRepository vinculoRepository,
        ICurrentUserContext currentUser)
    {
        _favoritoRepository = favoritoRepository;
        _estabelecimentoRepository = estabelecimentoRepository;
        _profissionalRepository = profissionalRepository;
        _vinculoRepository = vinculoRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<FavoritoClienteResponseDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var favoritos = await _favoritoRepository.ListarPorUsuarioAsync(ObterUsuarioId(), cancellationToken);
        return favoritos
            .Where(favorito => favorito.Estabelecimento is { Ativo: true, VisivelPublicamente: true }
                && (favorito.Profissional is null || favorito.Profissional.Ativo))
            .Select(Mapear)
            .ToList();
    }

    public async Task<FavoritoClienteResponseDto> AdicionarAsync(
        CriarFavoritoClienteRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = ObterUsuarioId();
        var estabelecimento = await _estabelecimentoRepository.ObterPorPublicGuidAsync(
            request.EstabelecimentoPublicGuid,
            cancellationToken);
        if (estabelecimento is null || !estabelecimento.Ativo || !estabelecimento.VisivelPublicamente)
        {
            throw new NegocioNaoEncontradoException();
        }

        Profissional? profissional = null;
        if (request.ProfissionalPublicGuid.HasValue)
        {
            profissional = await _profissionalRepository.ObterPorPublicGuidAsync(
                request.ProfissionalPublicGuid.Value,
                cancellationToken);
            if (profissional is null || !profissional.Ativo
                || !await _vinculoRepository.ExisteAtivoAsync(profissional.Id, estabelecimento.Id, cancellationToken))
            {
                throw new ProfissionalNegocioNaoEncontradoException();
            }
        }

        var existente = await _favoritoRepository.ObterAsync(
            usuarioId,
            estabelecimento.Id,
            profissional?.Id,
            cancellationToken);
        if (existente is not null)
        {
            return Mapear(existente);
        }

        var favorito = new FavoritoCliente
        {
            UsuarioClienteId = usuarioId,
            EstabelecimentoId = estabelecimento.Id,
            ProfissionalId = profissional?.Id,
            ProfissionalChave = profissional?.Id ?? 0,
            Estabelecimento = estabelecimento,
            Profissional = profissional,
            CriadoEm = DateTime.UtcNow
        };

        await _favoritoRepository.AdicionarAsync(favorito, cancellationToken);
        await _favoritoRepository.SalvarAlteracoesAsync(cancellationToken);
        return Mapear(favorito);
    }

    public async Task RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        var favorito = await _favoritoRepository.ObterPorIdAsync(id, ObterUsuarioId(), cancellationToken)
            ?? throw new NegocioNaoEncontradoException();
        _favoritoRepository.Remover(favorito);
        await _favoritoRepository.SalvarAlteracoesAsync(cancellationToken);
    }

    private int ObterUsuarioId() =>
        _currentUser.UserId ?? throw new UnauthorizedException();

    private static FavoritoClienteResponseDto Mapear(FavoritoCliente favorito)
    {
        var estabelecimento = favorito.Estabelecimento ?? throw new NegocioNaoEncontradoException();
        var profissional = favorito.Profissional;
        var tipo = profissional is null
            ? "Loja"
            : profissional.TipoProfissional == ProfessionalType.Autonomo
                ? "ProfissionalAutonomo"
                : "ProfissionalLoja";

        return new FavoritoClienteResponseDto(
            favorito.Id,
            estabelecimento.PublicGuid,
            estabelecimento.Nome,
            estabelecimento.Logo,
            profissional?.PublicGuid,
            profissional?.NomePublico,
            profissional?.Logo,
            tipo,
            favorito.CriadoEm);
    }
}
