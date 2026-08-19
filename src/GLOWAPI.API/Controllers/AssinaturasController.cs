using GLOWAPI.API.Attributes;
using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/assinaturas")]
public class AssinaturasController : ControllerBase
{
    private readonly IAssinaturaService _assinaturaService;
    private readonly ICobrancaAssinaturaService _cobrancaAssinaturaService;
    private readonly IAssinaturaOnboardingContextoService _assinaturaOnboardingContextoService;
    private readonly IOnboardingPublicacaoService _onboardingPublicacaoService;
    private readonly ICurrentUserContext _currentUser;

    public AssinaturasController(
        IAssinaturaService assinaturaService,
        ICobrancaAssinaturaService cobrancaAssinaturaService,
        IAssinaturaOnboardingContextoService assinaturaOnboardingContextoService,
        IOnboardingPublicacaoService onboardingPublicacaoService,
        ICurrentUserContext currentUser)
    {
        _assinaturaService = assinaturaService;
        _cobrancaAssinaturaService = cobrancaAssinaturaService;
        _assinaturaOnboardingContextoService = assinaturaOnboardingContextoService;
        _onboardingPublicacaoService = onboardingPublicacaoService;
        _currentUser = currentUser;
    }

    [HttpGet("onboarding/contexto")]
    public async Task<IActionResult> ObterContextoOnboarding(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return Unauthorized();
        }

        var contexto = await _assinaturaOnboardingContextoService.ObterContextoAsync(cancellationToken);
        return Ok(ApiSuccessResponse<AssinaturaOnboardingContextoResponseDto>.From(
            "Contexto de onboarding obtido com sucesso.",
            contexto));
    }

    [HttpGet("onboarding/publicacao")]
    [RequerPermissaoNegocio(PermissaoNegocio.NegocioVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ObterStatusPublicacao(
        [FromQuery] int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return Unauthorized();
        }

        var status = await _onboardingPublicacaoService.ObterStatusAsync(estabelecimentoId, cancellationToken);
        return Ok(ApiSuccessResponse<OnboardingPublicacaoStatusDto>.From(
            "Status de publicacao obtido com sucesso.",
            status));
    }

    [HttpPost("onboarding/publicacao/recalcular")]
    [RequerPermissaoNegocio(PermissaoNegocio.NegocioEditar, "estabelecimentoId")]
    public async Task<IActionResult> RecalcularPublicacao(
        [FromQuery] int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return Unauthorized();
        }

        var visivel = await _onboardingPublicacaoService.RecalcularVisibilidadeAsync(
            estabelecimentoId,
            cancellationToken);
        var status = await _onboardingPublicacaoService.ObterStatusAsync(estabelecimentoId, cancellationToken);

        return Ok(ApiSuccessResponse<OnboardingPublicacaoStatusDto>.From(
            visivel
                ? "Perfil publicado com sucesso."
                : "Perfil atualizado. Conclua o onboarding para publicar.",
            status));
    }

    [HttpPost]
    public async Task<IActionResult> Iniciar(
        [FromBody] IniciarAssinaturaRequestDto request,
        CancellationToken cancellationToken)
    {
        var assinatura = await _assinaturaService.IniciarAsync(request, cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<AssinaturaResponseDto>.From("Assinatura iniciada com sucesso.", assinatura));
    }

    [HttpPost("{assinaturaId:int}/trocar-plano")]
    public async Task<IActionResult> TrocarPlano(
        int assinaturaId,
        [FromBody] TrocarPlanoAssinaturaRequestDto request,
        CancellationToken cancellationToken)
    {
        var assinatura = await _assinaturaService.TrocarPlanoAsync(assinaturaId, request, cancellationToken);
        return Ok(ApiSuccessResponse<AssinaturaResponseDto>.From(
            "Solicitacao de troca de plano registrada com sucesso.",
            assinatura));
    }

    [HttpPost("{assinaturaId:int}/cancelar")]
    public async Task<IActionResult> Cancelar(
        int assinaturaId,
        CancellationToken cancellationToken)
    {
        var assinatura = await _assinaturaService.CancelarAsync(assinaturaId, cancellationToken);
        return Ok(ApiSuccessResponse<AssinaturaResponseDto>.From(
            "Assinatura cancelada com sucesso.",
            assinatura));
    }

    [HttpPost("{assinaturaId:int}/estabelecimentos")]
    public async Task<IActionResult> AdicionarEstabelecimento(
        int assinaturaId,
        [FromBody] AdicionarEstabelecimentoAssinaturaRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return Unauthorized();
        }

        var resultado = await _assinaturaService.AdicionarEstabelecimentoAsync(
            assinaturaId,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<AdicionarEstabelecimentoAssinaturaResponseDto>.From(
                "Unidade vinculada com sucesso.",
                resultado));
    }

    [HttpGet("atual")]
    [RequerPermissaoNegocio(PermissaoNegocio.NegocioVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ObterAtual(
        [FromQuery] int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return Unauthorized();
        }

        var assinatura = await _assinaturaService.ObterAtualPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        return Ok(ApiSuccessResponse<AssinaturaResponseDto>.From(
            "Assinatura atual obtida com sucesso.",
            assinatura));
    }

    [HttpGet("{assinaturaId:int}/cobrancas")]
    public async Task<IActionResult> ListarCobrancas(
        int assinaturaId,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        var cobrancas = await _cobrancaAssinaturaService.ListarPorAssinaturaAsync(
            assinaturaId,
            _currentUser.UserId.Value,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<CobrancaAssinaturaResponseDto>>.From(
            "Cobrancas da assinatura listadas com sucesso.",
            cobrancas));
    }
}
