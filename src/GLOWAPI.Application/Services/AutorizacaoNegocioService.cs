using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Autorizacao;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class AutorizacaoNegocioService : IAutorizacaoNegocioService
{
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IMatrizPermissaoNegocioService _matrizPermissaoNegocioService;
    private readonly ICurrentUserContext _currentUserContext;

    public AutorizacaoNegocioService(
        IEstabelecimentoRepository estabelecimentoRepository,
        IEstabelecimentoUsuarioRepository estabelecimentoUsuarioRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IMatrizPermissaoNegocioService matrizPermissaoNegocioService,
        ICurrentUserContext currentUserContext)
    {
        _estabelecimentoRepository = estabelecimentoRepository;
        _estabelecimentoUsuarioRepository = estabelecimentoUsuarioRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _matrizPermissaoNegocioService = matrizPermissaoNegocioService;
        _currentUserContext = currentUserContext;
    }

    public async Task<AutorizacaoNegocioResultado> ObterContextoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        var userId = ObterUsuarioAutenticado();

        var vinculoUsuario = await _estabelecimentoUsuarioRepository.ObterAtivoAsync(
            estabelecimentoId,
            userId,
            cancellationToken);

        if (vinculoUsuario is null)
        {
            throw new UsuarioSemVinculoNegocioException();
        }

        var possuiVinculoProfissional = await _profissionalEstabelecimentoRepository.ExisteAtivoPorUsuarioAsync(
            userId,
            estabelecimentoId,
            cancellationToken);

        var permissoes = _matrizPermissaoNegocioService.ObterPermissoes(
            vinculoUsuario.RoleNoEstabelecimento,
            possuiVinculoProfissional);

        return new AutorizacaoNegocioResultado(
            estabelecimentoId,
            userId,
            vinculoUsuario.RoleNoEstabelecimento,
            possuiVinculoProfissional,
            permissoes);
    }

    public async Task<AutorizacaoNegocioResultado> ObterContextoPorPublicGuidAsync(
        Guid publicGuid,
        CancellationToken cancellationToken = default)
    {
        var estabelecimento = await _estabelecimentoRepository.ObterPorPublicGuidAsync(publicGuid, cancellationToken);
        if (estabelecimento is null)
        {
            throw new NegocioNaoEncontradoException();
        }

        return await ObterContextoAsync(estabelecimento.Id, cancellationToken);
    }

    public async Task<AutorizacaoNegocioResultado> AutorizarAsync(
        int estabelecimentoId,
        PermissaoNegocio permissao,
        CancellationToken cancellationToken = default)
    {
        var contexto = await ObterContextoAsync(estabelecimentoId, cancellationToken);

        if (!contexto.PossuiPermissao(permissao))
        {
            throw new UsuarioSemPermissaoNegocioException();
        }

        return contexto;
    }

    public async Task<AutorizacaoNegocioResultado> AutorizarPorPublicGuidAsync(
        Guid publicGuid,
        PermissaoNegocio permissao,
        CancellationToken cancellationToken = default)
    {
        var contexto = await ObterContextoPorPublicGuidAsync(publicGuid, cancellationToken);

        if (!contexto.PossuiPermissao(permissao))
        {
            throw new UsuarioSemPermissaoNegocioException();
        }

        return contexto;
    }

    public async Task<bool> PossuiPermissaoAsync(
        int estabelecimentoId,
        PermissaoNegocio permissao,
        CancellationToken cancellationToken = default)
    {
        var contexto = await ObterContextoAsync(estabelecimentoId, cancellationToken);
        return contexto.PossuiPermissao(permissao);
    }

    private int ObterUsuarioAutenticado()
    {
        if (!_currentUserContext.IsAuthenticated || !_currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedException();
        }

        return _currentUserContext.UserId.Value;
    }
}
