using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Auth;

namespace GLOWAPI.Application.Services;

public class AssinaturaOnboardingContextoService : IAssinaturaOnboardingContextoService
{
    public const string EtapaEscolherPlano = "EscolherPlano";
    public const string EtapaCadastrarEstabelecimento = "CadastrarEstabelecimento";
    public const string EtapaAssinarPlano = "AssinarPlano";
    public const string EtapaGerenciarAssinatura = "GerenciarAssinatura";

    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly IAssinaturaRepository _assinaturaRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public AssinaturaOnboardingContextoService(
        IEstabelecimentoUsuarioRepository estabelecimentoUsuarioRepository,
        IAssinaturaRepository assinaturaRepository,
        ICurrentUserContext currentUserContext)
    {
        _estabelecimentoUsuarioRepository = estabelecimentoUsuarioRepository;
        _assinaturaRepository = assinaturaRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<AssinaturaOnboardingContextoResponseDto> ObterContextoAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = ObterUsuarioAutenticado();
        var vinculos = await _estabelecimentoUsuarioRepository.ListarAtivosPorUsuarioAsync(
            userId,
            cancellationToken);

        var owners = vinculos
            .Where(v =>
                v.RoleNoEstabelecimento == EstablishmentUserRole.Owner
                && v.Estabelecimento is not null
                && v.Estabelecimento.Ativo)
            .ToList();

        var estabelecimentos = new List<EstabelecimentoOnboardingContextoDto>(owners.Count);
        foreach (var vinculo in owners)
        {
            var estabelecimento = vinculo.Estabelecimento!;
            var assinatura = await _assinaturaRepository.ObterAtualPorEstabelecimentoAsync(
                estabelecimento.Id,
                cancellationToken);

            var assinaturaAtiva = assinatura?.Status is AssinaturaStatus.Ativa or AssinaturaStatus.Trial;
            var assinaturaPendente = assinatura?.Status == AssinaturaStatus.PendentePagamento;
            var podeContratar = assinatura is null
                || (!assinaturaAtiva && !assinaturaPendente);

            estabelecimentos.Add(new EstabelecimentoOnboardingContextoDto(
                estabelecimento.Id,
                estabelecimento.Nome,
                estabelecimento.Logo,
                assinaturaAtiva,
                assinaturaPendente,
                podeContratar));
        }

        if (estabelecimentos.Count == 0)
        {
            return new AssinaturaOnboardingContextoResponseDto(
                false,
                estabelecimentos,
                EtapaCadastrarEstabelecimento,
                null);
        }

        var comAssinaturaAtiva = estabelecimentos.Where(e => e.AssinaturaAtiva).ToList();
        if (comAssinaturaAtiva.Count == estabelecimentos.Count)
        {
            return new AssinaturaOnboardingContextoResponseDto(
                true,
                estabelecimentos,
                EtapaGerenciarAssinatura,
                comAssinaturaAtiva[0].EstabelecimentoId);
        }

        var comPendente = estabelecimentos.Where(e => e.AssinaturaPendente).ToList();
        if (comPendente.Count > 0)
        {
            return new AssinaturaOnboardingContextoResponseDto(
                true,
                estabelecimentos,
                EtapaGerenciarAssinatura,
                comPendente[0].EstabelecimentoId);
        }

        var podeAssinar = estabelecimentos.FirstOrDefault(e => e.PodeContratar);
        if (podeAssinar is not null)
        {
            return new AssinaturaOnboardingContextoResponseDto(
                true,
                estabelecimentos,
                EtapaAssinarPlano,
                podeAssinar.EstabelecimentoId);
        }

        return new AssinaturaOnboardingContextoResponseDto(
            true,
            estabelecimentos,
            EtapaGerenciarAssinatura,
            estabelecimentos[0].EstabelecimentoId);
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
