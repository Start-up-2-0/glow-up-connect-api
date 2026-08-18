using GLOWAPI.API.Helpers;
using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Usuario;
using GLOWAPI.Application.DTOs.Privacidade;
using GLOWAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/usuario")]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly IUsuarioNegocioContextoService _usuarioNegocioContextoService;
    private readonly ICaptchaValidator _captchaValidator;
    private readonly IExclusaoContaService _exclusaoContaService;

    public UsuarioController(
        IUsuarioService usuarioService,
        IUsuarioNegocioContextoService usuarioNegocioContextoService,
        ICaptchaValidator captchaValidator,
        IExclusaoContaService exclusaoContaService)
    {
        _usuarioService = usuarioService;
        _usuarioNegocioContextoService = usuarioNegocioContextoService;
        _captchaValidator = captchaValidator;
        _exclusaoContaService = exclusaoContaService;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> CadastrarCliente([FromBody] CadastrarClienteDto request, CancellationToken cancellationToken)
    {
        await CaptchaGuard.GarantirValidoAsync(_captchaValidator, request.CaptchaToken, HttpContext, cancellationToken);

        var usuario = await _usuarioService.CadastrarClienteAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, CadastroClienteResponseDto.From(usuario));
    }

    [HttpGet("me")]
    public async Task<IActionResult> ObterUsuario(CancellationToken cancellationToken)
    {
        var usuario = await _usuarioService.ObterPerfilAtualAsync(cancellationToken);
        return Ok(UsuarioResponseDto.From(usuario));
    }

    [HttpPost("me/codigo-agendamento/regenerar")]
    public async Task<IActionResult> RegenerarCodigoAgendamento(CancellationToken cancellationToken)
    {
        var codigo = await _usuarioService.RegenerarCodigoAgendamentoAtualAsync(cancellationToken);
        return Ok(ApiSuccessResponse<object>.From(
            "Código de agendamento regenerado. O código anterior deixa de funcionar.",
            new { codigoAgendamento = codigo }));
    }

    [HttpGet("me/estabelecimentos")]
    public async Task<IActionResult> ListarEstabelecimentos(CancellationToken cancellationToken)
    {
        var estabelecimentos = await _usuarioNegocioContextoService.ListarEstabelecimentosAsync(cancellationToken);
        return Ok(estabelecimentos);
    }

    [HttpPut("me")]
    public async Task<IActionResult> AtualizarUsuario([FromBody] AtualizarUsuarioDto request, CancellationToken cancellationToken)
    {
        await _usuarioService.AtualizarPerfilAtualAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("me/whatsapp/solicitar-confirmacao")]
    public async Task<IActionResult> SolicitarConfirmacaoWhatsApp(CancellationToken cancellationToken)
    {
        var instrucoes = await _usuarioService.SolicitarConfirmacaoWhatsAppAtualAsync(cancellationToken);
        return Ok(ApiSuccessResponse<object>.From(
            "Verifique o WhatsApp e seu e-mail para confirmar.",
            instrucoes));
    }

    [HttpPost("me/whatsapp/opt-in")]
    public async Task<IActionResult> AtualizarWhatsAppOptIn(
        [FromBody] WhatsAppOptInRequestDto request,
        CancellationToken cancellationToken)
    {
        await _usuarioService.AtualizarWhatsAppOptInAtualAsync(request.OptIn, cancellationToken);
        return NoContent();
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DesativarUsuario(
        [FromBody] SolicitarExclusaoContaRequestDto request,
        CancellationToken cancellationToken)
    {
        await _exclusaoContaService.SolicitarAsync(request.Senha, cancellationToken);
        return Ok(ApiSuccessResponse.From(
            "Exclusao solicitada. Voce tem 30 dias para reativar a conta."));
    }
}
