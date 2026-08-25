using GLOWAPI.Application.DTOs.Estabelecimentos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class ComodidadePerfilService : IComodidadePerfilService
{
    private readonly IComodidadeRepository _comodidadeRepository;
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly ICurrentUserContext _currentUser;

    public ComodidadePerfilService(
        IComodidadeRepository comodidadeRepository,
        IEstabelecimentoRepository estabelecimentoRepository,
        IProfissionalRepository profissionalRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        ICurrentUserContext currentUser)
    {
        _comodidadeRepository = comodidadeRepository;
        _estabelecimentoRepository = estabelecimentoRepository;
        _profissionalRepository = profissionalRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ComodidadeDto>> ListarCatalogoAsync(CancellationToken cancellationToken = default) =>
        Mapear(await _comodidadeRepository.ListarAtivasAsync(cancellationToken));

    public async Task<IReadOnlyList<ComodidadeDto>> ListarPorEstabelecimentoAsync(int estabelecimentoId, CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(estabelecimentoId, PermissaoNegocio.NegocioVisualizar, cancellationToken);
        return Mapear(await _comodidadeRepository.ListarPorEstabelecimentoAsync(estabelecimentoId, cancellationToken));
    }

    public async Task<IReadOnlyList<ComodidadeDto>> AtualizarEstabelecimentoAsync(
        int estabelecimentoId,
        IReadOnlyCollection<int> comodidadeIds,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(estabelecimentoId, PermissaoNegocio.NegocioEditar, cancellationToken);
        await ValidarIdsAsync(comodidadeIds, cancellationToken);
        await _comodidadeRepository.SubstituirDoEstabelecimentoAsync(estabelecimentoId, comodidadeIds, cancellationToken);
        return Mapear(await _comodidadeRepository.ListarPorEstabelecimentoAsync(estabelecimentoId, cancellationToken));
    }

    public async Task<IReadOnlyList<ComodidadeDto>> ListarPorProfissionalAutonomoAsync(int profissionalId, CancellationToken cancellationToken = default)
    {
        var estabelecimentoId = await ObterEstabelecimentoAutonomoAsync(profissionalId, cancellationToken);
        return Mapear(await _comodidadeRepository.ListarPorEstabelecimentoAsync(estabelecimentoId, cancellationToken));
    }

    public async Task<IReadOnlyList<ComodidadeDto>> AtualizarProfissionalAutonomoAsync(
        int profissionalId,
        IReadOnlyCollection<int> comodidadeIds,
        CancellationToken cancellationToken = default)
    {
        var estabelecimentoId = await ObterEstabelecimentoAutonomoAsync(profissionalId, cancellationToken);
        await ValidarIdsAsync(comodidadeIds, cancellationToken);
        await _comodidadeRepository.SubstituirDoEstabelecimentoAsync(estabelecimentoId, comodidadeIds, cancellationToken);
        return Mapear(await _comodidadeRepository.ListarPorEstabelecimentoAsync(estabelecimentoId, cancellationToken));
    }

    public async Task<IReadOnlyList<ComodidadeDto>> ListarPublicasAsync(Guid estabelecimentoPublicGuid, CancellationToken cancellationToken = default)
    {
        var estabelecimento = await _estabelecimentoRepository.ObterPorPublicGuidAsync(estabelecimentoPublicGuid, cancellationToken);
        if (estabelecimento is null || !estabelecimento.Ativo || !estabelecimento.VisivelPublicamente)
        {
            throw new NegocioNaoEncontradoException();
        }

        return Mapear(await _comodidadeRepository.ListarPorEstabelecimentoAsync(estabelecimento.Id, cancellationToken));
    }

    private async Task<int> ObterEstabelecimentoAutonomoAsync(int profissionalId, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedException();
        }

        var profissional = await _profissionalRepository.ObterPorIdAsync(profissionalId, cancellationToken);
        if (profissional is null || !profissional.Ativo || profissional.TipoProfissional != ProfessionalType.Autonomo)
        {
            throw new TitularAssinaturaNaoEncontradoException();
        }

        if (profissional.UsuarioId != _currentUser.UserId.Value)
        {
            throw new UsuarioSemPermissaoAssinaturaException();
        }

        var vinculo = await _profissionalEstabelecimentoRepository.ObterAtivoPorProfissionalAsync(profissionalId, cancellationToken);
        if (vinculo is null)
        {
            throw new ProfissionalAutonomoAssinaturaInvalidoException("Profissional autonomo nao possui negocio vinculado.");
        }

        return vinculo.EstabelecimentoId;
    }

    private async Task ValidarIdsAsync(IReadOnlyCollection<int> comodidadeIds, CancellationToken cancellationToken)
    {
        if (comodidadeIds.Count != comodidadeIds.Distinct().Count())
        {
            throw new EstabelecimentoAssinaturaInvalidoException("A lista de comodidades possui itens duplicados.");
        }

        var idsAtivos = (await _comodidadeRepository.ListarAtivasAsync(cancellationToken)).Select(item => item.Id).ToHashSet();
        if (comodidadeIds.Any(id => !idsAtivos.Contains(id)))
        {
            throw new EstabelecimentoAssinaturaInvalidoException("Uma ou mais comodidades sao invalidas ou estao inativas.");
        }
    }

    private static IReadOnlyList<ComodidadeDto> Mapear(IEnumerable<Comodidade> comodidades) =>
        comodidades.Select(item => new ComodidadeDto(item.Id, item.Nome, item.Slug, item.Icone, item.Ordem)).ToList();
}
