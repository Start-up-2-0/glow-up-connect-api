using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Agendamento;
using GLOWAPI.Application.DTOs.Auth;
using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.DTOs.Servicos;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/publico/agendar")]
[AllowAnonymous]
public class AgendamentoPublicoController : ControllerBase
{
    private readonly IDisponibilidadeAgendaService _disponibilidadeAgendaService;
    private readonly IServicoNegocioService _servicoNegocioService;
    private readonly IAgendamentoNegocioService _agendamentoNegocioService;
    private readonly IAuthService _authService;

    public AgendamentoPublicoController(
        IDisponibilidadeAgendaService disponibilidadeAgendaService,
        IServicoNegocioService servicoNegocioService,
        IAgendamentoNegocioService agendamentoNegocioService,
        IAuthService authService)
    {
        _disponibilidadeAgendaService = disponibilidadeAgendaService;
        _servicoNegocioService = servicoNegocioService;
        _agendamentoNegocioService = agendamentoNegocioService;
        _authService = authService;
    }

    [HttpGet("loja/{publicGuid:guid}/profissional/{profissionalPublicGuid:guid}")]
    public async Task<IActionResult> ObterContextoLojaProfissional(
        Guid publicGuid,
        Guid profissionalPublicGuid,
        CancellationToken cancellationToken)
    {
        var contexto = await _agendamentoNegocioService.ObterContextoPublicoAsync(
            publicGuid,
            profissionalPublicGuid,
            cancellationToken);

        return Ok(ApiSuccessResponse<AgendamentoContextoPublicoResponseDto>.From(
            "Contexto publico de agendamento obtido com sucesso.",
            contexto));
    }

    [HttpGet("loja/{publicGuid:guid}/profissionais")]
    public async Task<IActionResult> ListarProfissionaisLoja(
        Guid publicGuid,
        CancellationToken cancellationToken)
    {
        var profissionais = await _agendamentoNegocioService.ListarProfissionaisPublicosPorLojaAsync(
            publicGuid,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ProfissionalPublicoResponseDto>>.From(
            "Profissionais publicos da loja listados com sucesso.",
            profissionais));
    }

    [HttpGet("loja/{publicGuid:guid}/disponibilidade")]
    public async Task<IActionResult> ConsultarDisponibilidadeLoja(
        Guid publicGuid,
        [FromQuery] ConsultarDisponibilidadeAgendaDto request,
        CancellationToken cancellationToken)
    {
        var disponibilidade = await _disponibilidadeAgendaService.ConsultarPublicoPorEstabelecimentoAsync(
            publicGuid,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<DisponibilidadeAgendaResponseDto>.From(
            "Disponibilidade publica consultada com sucesso.",
            disponibilidade));
    }

    [HttpGet("profissional/{publicGuid:guid}/disponibilidade")]
    public async Task<IActionResult> ConsultarDisponibilidadeProfissional(
        Guid publicGuid,
        [FromQuery] ConsultarDisponibilidadeAgendaDto request,
        CancellationToken cancellationToken)
    {
        var disponibilidade = await _disponibilidadeAgendaService.ConsultarPublicoPorProfissionalAsync(
            publicGuid,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<DisponibilidadeAgendaResponseDto>.From(
            "Disponibilidade publica do profissional consultada com sucesso.",
            disponibilidade));
    }

    [HttpGet("loja/{publicGuid:guid}/servicos")]
    public async Task<IActionResult> ListarServicosLoja(
        Guid publicGuid,
        [FromQuery] Guid? profissionalPublicGuid,
        CancellationToken cancellationToken)
    {
        var servicos = await _servicoNegocioService.ListarPublicosPorEstabelecimentoAsync(
            publicGuid,
            profissionalPublicGuid,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ServicoPublicoResponseDto>>.From(
            "Servicos publicos da loja listados com sucesso.",
            servicos));
    }

    [HttpGet("profissional/{publicGuid:guid}/servicos")]
    public async Task<IActionResult> ListarServicosProfissional(
        Guid publicGuid,
        CancellationToken cancellationToken)
    {
        var servicos = await _servicoNegocioService.ListarPublicosPorProfissionalAsync(
            publicGuid,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ServicoPublicoResponseDto>>.From(
            "Servicos publicos do profissional listados com sucesso.",
            servicos));
    }

    [HttpPost("loja/{publicGuid:guid}/com-cadastro")]
    public async Task<IActionResult> CriarAgendamentoComCadastro(
        Guid publicGuid,
        [FromBody] CriarAgendamentoComCadastroRequestDto request,
        CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoNegocioService.CriarPublicoComCadastroAsync(
            publicGuid,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<AgendamentoCriadoResponseDto>.From(
                "Agendamento com cadastro criado com sucesso. Confirme seu e-mail para ativar a conta.",
                agendamento));
    }

    [HttpPost("loja/{publicGuid:guid}")]
    public async Task<IActionResult> CriarAgendamentoLoja(
        Guid publicGuid,
        [FromBody] CriarAgendamentoRequestDto request,
        CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoNegocioService.CriarPublicoPorLojaAsync(
            publicGuid,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<AgendamentoCriadoResponseDto>.From(
                "Agendamento publico criado com sucesso.",
                agendamento));
    }

    [HttpGet("remarcacao/{token:guid}")]
    public async Task<IActionResult> ObterPropostaRemarcacao(
        Guid token,
        CancellationToken cancellationToken)
    {
        var proposta = await _agendamentoNegocioService.ObterPropostaRemarcacaoPorTokenAsync(token, cancellationToken);

        return Ok(ApiSuccessResponse<PropostaRemarcacaoResponseDto>.From(
            "Proposta de remarcacao obtida com sucesso.",
            proposta));
    }

    [HttpPost("remarcacao/{token:guid}/aceitar")]
    public async Task<IActionResult> AceitarPropostaRemarcacao(
        Guid token,
        CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoNegocioService.AceitarPropostaRemarcacaoPorTokenAsync(token, cancellationToken);

        return Ok(ApiSuccessResponse<AgendamentoCriadoResponseDto>.From(
            "Proposta de remarcacao aceita com sucesso.",
            agendamento));
    }

    [HttpPost("remarcacao/{token:guid}/recusar")]
    public async Task<IActionResult> RecusarPropostaRemarcacao(
        Guid token,
        CancellationToken cancellationToken)
    {
        await _agendamentoNegocioService.RecusarPropostaRemarcacaoPorTokenAsync(token, cancellationToken);

        return Ok(ApiSuccessResponse<object>.From(
            "Proposta de remarcacao recusada com sucesso.",
            null!));
    }

    [HttpPost("profissional/{publicGuid:guid}")]
    public async Task<IActionResult> CriarAgendamentoProfissional(
        Guid publicGuid,
        [FromBody] CriarAgendamentoRequestDto request,
        CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoNegocioService.CriarPublicoPorProfissionalAsync(
            publicGuid,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<AgendamentoCriadoResponseDto>.From(
                "Agendamento publico do profissional criado com sucesso.",
                agendamento));
    }

    /// <summary>
    /// Autentica o cliente pelo código pessoal de agendamento (sessão curta, ~15 min).
    /// </summary>
    [HttpPost("auth/codigo")]
    public async Task<IActionResult> AutenticarPorCodigo(
        [FromBody] AutenticarCodigoAgendamentoRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.AutenticarPorCodigoAgendamentoAsync(
            request,
            new AuthSessionContext(
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.Request.Headers.UserAgent.ToString()),
            cancellationToken);

        var dto = LoginResponseDto.From(result);
        dto.RefreshToken = string.Empty;

        return Ok(ApiSuccessResponse<LoginResponseDto>.From(
            "Autenticado para agendamento público. A sessão expira em poucos minutos ou ao confirmar o agendamento.",
            dto));
    }
}
