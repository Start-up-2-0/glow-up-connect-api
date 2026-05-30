using GLOWAPI.API.Attributes;
using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Agenda;
using GLOWAPI.Application.DTOs.Caixa;
using GLOWAPI.Application.DTOs.Equipe;
using GLOWAPI.Application.DTOs.Estabelecimentos;
using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GLOWAPI.API.Controllers;

[ApiController]
[Route("api/estabelecimentos")]
public class EstabelecimentosController : ControllerBase
{
    private readonly IEstabelecimentoPerfilService _estabelecimentoPerfilService;
    private readonly IEquipeNegocioService _equipeNegocioService;
    private readonly IAgendaNegocioService _agendaNegocioService;
    private readonly IAtendimentoProfissionalService _atendimentoProfissionalService;
    private readonly ICaixaNegocioService _caixaNegocioService;
    private readonly IProfissionalServicoNegocioService _profissionalServicoNegocioService;
    private readonly IHorarioFuncionamentoNegocioService _horarioFuncionamentoNegocioService;
    private readonly IHorarioProfissionalNegocioService _horarioProfissionalNegocioService;
    private readonly IDisponibilidadeAgendaService _disponibilidadeAgendaService;

    public EstabelecimentosController(
        IEstabelecimentoPerfilService estabelecimentoPerfilService,
        IEquipeNegocioService equipeNegocioService,
        IAgendaNegocioService agendaNegocioService,
        IAtendimentoProfissionalService atendimentoProfissionalService,
        ICaixaNegocioService caixaNegocioService,
        IProfissionalServicoNegocioService profissionalServicoNegocioService,
        IHorarioFuncionamentoNegocioService horarioFuncionamentoNegocioService,
        IHorarioProfissionalNegocioService horarioProfissionalNegocioService,
        IDisponibilidadeAgendaService disponibilidadeAgendaService)
    {
        _estabelecimentoPerfilService = estabelecimentoPerfilService;
        _equipeNegocioService = equipeNegocioService;
        _agendaNegocioService = agendaNegocioService;
        _atendimentoProfissionalService = atendimentoProfissionalService;
        _caixaNegocioService = caixaNegocioService;
        _profissionalServicoNegocioService = profissionalServicoNegocioService;
        _horarioFuncionamentoNegocioService = horarioFuncionamentoNegocioService;
        _horarioProfissionalNegocioService = horarioProfissionalNegocioService;
        _disponibilidadeAgendaService = disponibilidadeAgendaService;
    }

    [HttpPut("{estabelecimentoId:int}/perfil")]
    [RequerPermissaoNegocio(PermissaoNegocio.NegocioEditar, "estabelecimentoId")]
    public async Task<IActionResult> AtualizarPerfil(
        int estabelecimentoId,
        [FromBody] AtualizarEstabelecimentoPerfilDto request,
        CancellationToken cancellationToken)
    {
        var perfil = await _estabelecimentoPerfilService.AtualizarAsync(
            estabelecimentoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<EstabelecimentoPerfilResponseDto>.From(
            "Perfil do estabelecimento atualizado com sucesso.",
            perfil));
    }

    [HttpPost("{estabelecimentoId:int}/equipe/usuarios")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Profissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.EquipeGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> CadastrarUsuarioEquipe(
        int estabelecimentoId,
        [FromBody] CadastrarUsuarioEquipeRequestDto request,
        CancellationToken cancellationToken)
    {
        var usuarioEquipe = await _equipeNegocioService.CadastrarUsuarioAsync(
            estabelecimentoId,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<UsuarioEquipeResponseDto>.From(
                "Usuario da equipe cadastrado com sucesso.",
                usuarioEquipe));
    }

    [HttpPatch("{estabelecimentoId:int}/equipe/usuarios/{usuarioId:int}/role")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Profissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.EquipeGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> AtualizarRoleUsuarioEquipe(
        int estabelecimentoId,
        int usuarioId,
        [FromBody] AtualizarRoleUsuarioEquipeRequestDto request,
        CancellationToken cancellationToken)
    {
        var usuarioEquipe = await _equipeNegocioService.AtualizarRoleUsuarioAsync(
            estabelecimentoId,
            usuarioId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<UsuarioEquipeResponseDto>.From(
            "Role do usuario da equipe atualizada com sucesso.",
            usuarioEquipe));
    }

    [HttpPatch("{estabelecimentoId:int}/equipe/usuarios/{usuarioId:int}/status")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Profissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.EquipeGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> AtualizarStatusUsuarioEquipe(
        int estabelecimentoId,
        int usuarioId,
        [FromBody] AtualizarStatusUsuarioEquipeRequestDto request,
        CancellationToken cancellationToken)
    {
        var usuarioEquipe = await _equipeNegocioService.AtualizarStatusUsuarioAsync(
            estabelecimentoId,
            usuarioId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<UsuarioEquipeResponseDto>.From(
            "Status do usuario da equipe atualizado com sucesso.",
            usuarioEquipe));
    }

    [HttpPost("{estabelecimentoId:int}/equipe/profissionais")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Profissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ProfissionalConvidar, "estabelecimentoId")]
    public async Task<IActionResult> ConvidarProfissionalEquipe(
        int estabelecimentoId,
        [FromBody] ConvidarProfissionalEquipeRequestDto request,
        CancellationToken cancellationToken)
    {
        var profissionalEquipe = await _equipeNegocioService.ConvidarProfissionalAsync(
            estabelecimentoId,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<ProfissionalEquipeResponseDto>.From(
                "Profissional vinculado ao negocio com sucesso.",
                profissionalEquipe));
    }

    [HttpPatch("{estabelecimentoId:int}/equipe/profissionais/{profissionalId:int}/status")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Profissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ProfissionalGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> AtualizarStatusProfissionalEquipe(
        int estabelecimentoId,
        int profissionalId,
        [FromBody] AtualizarStatusProfissionalEquipeRequestDto request,
        CancellationToken cancellationToken)
    {
        var profissionalEquipe = await _equipeNegocioService.AtualizarStatusProfissionalAsync(
            estabelecimentoId,
            profissionalId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<ProfissionalEquipeResponseDto>.From(
            "Status do profissional atualizado com sucesso.",
            profissionalEquipe));
    }

    [HttpGet("{estabelecimentoId:int}/agenda")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Agenda, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.AgendaVisualizarGeral, "estabelecimentoId")]
    public async Task<IActionResult> ListarAgendaGeral(
        int estabelecimentoId,
        [FromQuery] AgendaGeralFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var agenda = await _agendaNegocioService.ListarAgendaGeralAsync(
            estabelecimentoId,
            filtro,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<AgendaGeralResponseDto>>.From(
            "Agenda geral listada com sucesso.",
            agenda));
    }

    [HttpGet("{estabelecimentoId:int}/agenda/propria")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Agenda, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.AgendaVisualizarPropria, "estabelecimentoId")]
    public async Task<IActionResult> ListarAgendaProfissional(
        int estabelecimentoId,
        [FromQuery] AgendaProfissionalFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var agenda = await _agendaNegocioService.ListarAgendaProfissionalAsync(
            estabelecimentoId,
            filtro,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<AgendaProfissionalResponseDto>>.From(
            "Agenda do profissional listada com sucesso.",
            agenda));
    }

    [HttpPost("{estabelecimentoId:int}/atendimentos/{agendamentoItemId:int}/iniciar")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Agenda, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.AtendimentoIniciar, "estabelecimentoId")]
    public async Task<IActionResult> IniciarAtendimento(
        int estabelecimentoId,
        int agendamentoItemId,
        CancellationToken cancellationToken)
    {
        var atendimento = await _atendimentoProfissionalService.IniciarAsync(
            estabelecimentoId,
            agendamentoItemId,
            cancellationToken);

        return Ok(ApiSuccessResponse<AtendimentoProfissionalResponseDto>.From(
            "Atendimento iniciado com sucesso.",
            atendimento));
    }

    [HttpPost("{estabelecimentoId:int}/atendimentos/{agendamentoItemId:int}/finalizar")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Agenda, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.AtendimentoFinalizar, "estabelecimentoId")]
    public async Task<IActionResult> FinalizarAtendimento(
        int estabelecimentoId,
        int agendamentoItemId,
        CancellationToken cancellationToken)
    {
        var atendimento = await _atendimentoProfissionalService.FinalizarAsync(
            estabelecimentoId,
            agendamentoItemId,
            cancellationToken);

        return Ok(ApiSuccessResponse<AtendimentoProfissionalResponseDto>.From(
            "Atendimento finalizado com sucesso.",
            atendimento));
    }

    [HttpGet("{estabelecimentoId:int}/caixa")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Caixa, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ObterCaixa(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var caixa = await _caixaNegocioService.ObterResumoAsync(
            estabelecimentoId,
            cancellationToken);

        return Ok(ApiSuccessResponse<CaixaResumoResponseDto>.From(
            "Caixa consultado com sucesso.",
            caixa));
    }

    [HttpGet("{estabelecimentoId:int}/caixa/lancamentos")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Caixa, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ListarLancamentosCaixa(
        int estabelecimentoId,
        [FromQuery] LancamentoCaixaFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var lancamentos = await _caixaNegocioService.ListarLancamentosAsync(
            estabelecimentoId,
            filtro,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<LancamentoCaixaResponseDto>>.From(
            "Lancamentos do caixa listados com sucesso.",
            lancamentos));
    }

    [HttpPost("{estabelecimentoId:int}/servicos/{servicoId:int}/profissionais/{profissionalId:int}")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Servicos, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ServicoGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> VincularServicoProfissional(
        int estabelecimentoId,
        int servicoId,
        int profissionalId,
        [FromBody] VincularServicoProfissionalRequestDto request,
        CancellationToken cancellationToken)
    {
        var vinculo = await _profissionalServicoNegocioService.VincularAsync(
            estabelecimentoId,
            profissionalId,
            servicoId,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<ProfissionalServicoResponseDto>.From(
                "Servico vinculado ao profissional com sucesso.",
                vinculo));
    }

    [HttpGet("{estabelecimentoId:int}/horarios-funcionamento")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.HorariosAtendimento, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.HorarioVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ListarHorariosFuncionamento(
        int estabelecimentoId,
        [FromQuery] HorarioFuncionamentoFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var horarios = await _horarioFuncionamentoNegocioService.ListarAsync(
            estabelecimentoId,
            filtro,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<HorarioFuncionamentoResponseDto>>.From(
            "Horarios de funcionamento listados com sucesso.",
            horarios));
    }

    [HttpPost("{estabelecimentoId:int}/horarios-funcionamento")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.HorariosAtendimento, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.HorarioGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> CriarHorarioFuncionamento(
        int estabelecimentoId,
        [FromBody] CriarHorarioFuncionamentoRequestDto request,
        CancellationToken cancellationToken)
    {
        var horario = await _horarioFuncionamentoNegocioService.CriarAsync(
            estabelecimentoId,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<HorarioFuncionamentoResponseDto>.From(
                "Horario de funcionamento criado com sucesso.",
                horario));
    }

    [HttpPut("{estabelecimentoId:int}/horarios-funcionamento/{horarioId:int}")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.HorariosAtendimento, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.HorarioGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> AtualizarHorarioFuncionamento(
        int estabelecimentoId,
        int horarioId,
        [FromBody] AtualizarHorarioFuncionamentoRequestDto request,
        CancellationToken cancellationToken)
    {
        var horario = await _horarioFuncionamentoNegocioService.AtualizarAsync(
            estabelecimentoId,
            horarioId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<HorarioFuncionamentoResponseDto>.From(
            "Horario de funcionamento atualizado com sucesso.",
            horario));
    }

    [HttpPatch("{estabelecimentoId:int}/horarios-funcionamento/{horarioId:int}/status")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.HorariosAtendimento, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.HorarioGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> AtualizarStatusHorarioFuncionamento(
        int estabelecimentoId,
        int horarioId,
        [FromBody] AtualizarStatusHorarioFuncionamentoRequestDto request,
        CancellationToken cancellationToken)
    {
        var horario = await _horarioFuncionamentoNegocioService.AtualizarStatusAsync(
            estabelecimentoId,
            horarioId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<HorarioFuncionamentoResponseDto>.From(
            "Status do horario de funcionamento atualizado com sucesso.",
            horario));
    }

    [HttpPost("{estabelecimentoId:int}/profissionais/{profissionalId:int}/horarios")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.HorariosAtendimento, "estabelecimentoId")]
    public async Task<IActionResult> CriarHorarioProfissional(
        int estabelecimentoId,
        int profissionalId,
        [FromBody] CriarHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken)
    {
        var horario = await _horarioProfissionalNegocioService.CriarAsync(
            estabelecimentoId,
            profissionalId,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<HorarioProfissionalResponseDto>.From(
                "Horario do profissional criado com sucesso.",
                horario));
    }

    [HttpGet("{estabelecimentoId:int}/profissionais/horarios")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.HorariosAtendimento, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.HorarioVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ListarHorariosProfissionais(
        int estabelecimentoId,
        [FromQuery] HorarioProfissionalFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var horarios = await _horarioProfissionalNegocioService.ListarAsync(
            estabelecimentoId,
            filtro,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<HorarioProfissionalResponseDto>>.From(
            "Horarios dos profissionais listados com sucesso.",
            horarios));
    }

    [HttpPut("{estabelecimentoId:int}/profissionais/horarios/{horarioId:int}")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.HorariosAtendimento, "estabelecimentoId")]
    public async Task<IActionResult> AtualizarHorarioProfissional(
        int estabelecimentoId,
        int horarioId,
        [FromBody] AtualizarHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken)
    {
        var horario = await _horarioProfissionalNegocioService.AtualizarAsync(
            estabelecimentoId,
            horarioId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<HorarioProfissionalResponseDto>.From(
            "Horario do profissional atualizado com sucesso.",
            horario));
    }

    [HttpPatch("{estabelecimentoId:int}/profissionais/horarios/{horarioId:int}/status")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.HorariosAtendimento, "estabelecimentoId")]
    public async Task<IActionResult> AtualizarStatusHorarioProfissional(
        int estabelecimentoId,
        int horarioId,
        [FromBody] AtualizarStatusHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken)
    {
        var horario = await _horarioProfissionalNegocioService.AtualizarStatusAsync(
            estabelecimentoId,
            horarioId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<HorarioProfissionalResponseDto>.From(
            "Status do horario do profissional atualizado com sucesso.",
            horario));
    }

    [HttpGet("{estabelecimentoId:int}/disponibilidade")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.HorariosAtendimento, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.AgendaCriar, "estabelecimentoId")]
    public async Task<IActionResult> ConsultarDisponibilidade(
        int estabelecimentoId,
        [FromQuery] ConsultarDisponibilidadeAgendaDto request,
        CancellationToken cancellationToken)
    {
        var disponibilidade = await _disponibilidadeAgendaService.ConsultarPorEstabelecimentoAsync(
            estabelecimentoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<DisponibilidadeAgendaResponseDto>.From(
            "Disponibilidade consultada com sucesso.",
            disponibilidade));
    }
}
