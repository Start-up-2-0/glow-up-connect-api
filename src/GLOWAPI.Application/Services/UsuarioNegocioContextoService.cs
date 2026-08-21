using GLOWAPI.Application.DTOs.Usuario;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Auth;

namespace GLOWAPI.Application.Services;

public class UsuarioNegocioContextoService : IUsuarioNegocioContextoService
{
    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IMatrizPermissaoNegocioService _matrizPermissaoNegocioService;
    private readonly IModulosAssinaturaService _modulosAssinaturaService;
    private readonly IAssinaturaRepository _assinaturaRepository;
    private readonly ICampanhaPromocionalRepository _campanhaPromocionalRepository;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IOnboardingPublicacaoService _onboardingPublicacaoService;

    public UsuarioNegocioContextoService(
        IEstabelecimentoUsuarioRepository estabelecimentoUsuarioRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IProfissionalRepository profissionalRepository,
        IMatrizPermissaoNegocioService matrizPermissaoNegocioService,
        IModulosAssinaturaService modulosAssinaturaService,
        IAssinaturaRepository assinaturaRepository,
        ICampanhaPromocionalRepository campanhaPromocionalRepository,
        ICurrentUserContext currentUserContext,
        IOnboardingPublicacaoService onboardingPublicacaoService)
    {
        _estabelecimentoUsuarioRepository = estabelecimentoUsuarioRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _profissionalRepository = profissionalRepository;
        _matrizPermissaoNegocioService = matrizPermissaoNegocioService;
        _modulosAssinaturaService = modulosAssinaturaService;
        _assinaturaRepository = assinaturaRepository;
        _campanhaPromocionalRepository = campanhaPromocionalRepository;
        _currentUserContext = currentUserContext;
        _onboardingPublicacaoService = onboardingPublicacaoService;
    }

    public async Task<IReadOnlyList<EstabelecimentoAcessoResponseDto>> ListarEstabelecimentosAsync(
        CancellationToken cancellationToken = default)
    {
        GarantirAcessoNegocio();

        var usuarioId = ObterUsuarioAutenticado();
        var vinculos = await _estabelecimentoUsuarioRepository.ListarAtivosPorUsuarioAsync(
            usuarioId,
            cancellationToken);

        var response = new List<EstabelecimentoAcessoResponseDto>(vinculos.Count);
        foreach (var vinculo in vinculos)
        {
            if (vinculo.Estabelecimento is null)
            {
                continue;
            }

            var possuiVinculoProfissional = await _profissionalEstabelecimentoRepository.ExisteAtivoPorUsuarioAsync(
                usuarioId,
                vinculo.EstabelecimentoId,
                cancellationToken);
            var permissoes = _matrizPermissaoNegocioService
                .ObterPermissoes(vinculo.RoleNoEstabelecimento, possuiVinculoProfissional)
                .Select(permissao => permissao.ToString())
                .OrderBy(permissao => permissao)
                .ToList();
            var modulos = await _modulosAssinaturaService.ObterPorEstabelecimentoAsync(
                vinculo.EstabelecimentoId,
                cancellationToken);

            var (diasTrial, _) = await ObterDiasTrialAsync(modulos.AssinaturaId, cancellationToken);

            var onboardingPendente = false;
            string? proximaEtapaOnboarding = null;
            if (modulos.AssinaturaAtiva && vinculo.RoleNoEstabelecimento == EstablishmentUserRole.Owner)
            {
                await _onboardingPublicacaoService.RecalcularVisibilidadeAsync(
                    vinculo.EstabelecimentoId,
                    cancellationToken);
                var statusPublicacao = await _onboardingPublicacaoService.ObterStatusAsync(
                    vinculo.EstabelecimentoId,
                    cancellationToken);
                onboardingPendente = statusPublicacao.OnboardingObrigatorioPendente;
                proximaEtapaOnboarding = statusPublicacao.ProximaEtapa;
            }

            // Regra de acesso: em plano de loja única (Básico/Plus), o Dono só enxerga a loja principal.
            var limiteEstabelecimentos = modulos.Limites.Estabelecimentos;
            if (vinculo.RoleNoEstabelecimento == EstablishmentUserRole.Owner
                && limiteEstabelecimentos is > 0 and <= 1
                && modulos.AssinaturaId.HasValue)
            {
                var assinatura = await _assinaturaRepository.ObterPorIdAsync(
                    modulos.AssinaturaId.Value,
                    cancellationToken);
                // Filtra filiais fora do escopo do plano: mantém apenas a matriz da assinatura.
                if (assinatura?.EstabelecimentoId != vinculo.EstabelecimentoId)
                {
                    continue;
                }
            }

            int? profissionalId = null;
            Guid? profissionalPublicGuid = null;
            if (possuiVinculoProfissional)
            {
                var profissional = await _profissionalRepository.ObterPorUsuarioIdAsync(
                    usuarioId,
                    cancellationToken);
                if (profissional is not null)
                {
                    profissionalId = profissional.Id;
                    profissionalPublicGuid = profissional.PublicGuid;
                }
            }

            response.Add(new EstabelecimentoAcessoResponseDto(
                vinculo.EstabelecimentoId,
                vinculo.Estabelecimento.PublicGuid,
                vinculo.Estabelecimento.Nome,
                vinculo.Estabelecimento.Logo,
                vinculo.RoleNoEstabelecimento.ToString(),
                possuiVinculoProfissional,
                profissionalId,
                profissionalPublicGuid,
                permissoes,
                modulos.AssinaturaAtiva,
                modulos.AssinaturaId,
                modulos.PlanoId,
                modulos.PlanoNome,
                modulos.Status,
                modulos.Status == AssinaturaStatus.Trial.ToString(),
                diasTrial,
                await ObterProximaDataVencimentoAsync(modulos.AssinaturaId, cancellationToken),
                modulos.Modulos,
                modulos.Limites,
                modulos.TipoAssinatura,
                vinculo.Estabelecimento.CategoriaEstabelecimentoId,
                vinculo.Estabelecimento.CategoriaEstabelecimento?.Nome,
                onboardingPendente,
                proximaEtapaOnboarding));
        }

        return response;
    }

    private void GarantirAcessoNegocio()
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            throw new UnauthorizedException();
        }
    }

    private async Task<DateTime?> ObterProximaDataVencimentoAsync(
        int? assinaturaId,
        CancellationToken cancellationToken)
    {
        if (!assinaturaId.HasValue)
        {
            return null;
        }

        var assinatura = await _assinaturaRepository.ObterPorIdAsync(assinaturaId.Value, cancellationToken);
        return assinatura?.ProximaDataVencimento;
    }

    private async Task<(int? DiasTrial, Assinatura? Assinatura)> ObterDiasTrialAsync(
        int? assinaturaId,
        CancellationToken cancellationToken)
    {
        if (!assinaturaId.HasValue)
        {
            return (null, null);
        }

        var assinatura = await _assinaturaRepository.ObterPorIdAsync(assinaturaId.Value, cancellationToken);
        if (assinatura?.CampanhaPromocionalId is null)
        {
            return (null, assinatura);
        }

        var campanha = await _campanhaPromocionalRepository.ObterPorIdAsync(
            assinatura.CampanhaPromocionalId.Value,
            cancellationToken);

        return (campanha?.DiasTrial, assinatura);
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
