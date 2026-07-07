using GLOWAPI.API.Attributes;
using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Agendamento;
using GLOWAPI.Application.DTOs.Auditoria;
using GLOWAPI.Application.DTOs.Avaliacao;
using GLOWAPI.Application.DTOs.Clientes;
using GLOWAPI.Application.DTOs.Agenda;
using GLOWAPI.Application.DTOs.Caixa;
using GLOWAPI.Application.DTOs.Financeiro;
using GLOWAPI.Application.DTOs.Equipe;
using GLOWAPI.Application.DTOs.Estabelecimentos;
using GLOWAPI.Application.DTOs.Servicos;
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
    private readonly IServicoNegocioService _servicoNegocioService;
    private readonly IHorarioProfissionalNegocioService _horarioProfissionalNegocioService;
    private readonly IDisponibilidadeAgendaService _disponibilidadeAgendaService;
    private readonly IAgendamentoNegocioService _agendamentoNegocioService;
    private readonly IFinanceiroNegocioService _financeiroNegocioService;
    private readonly IRecebimentoAgendamentoService _recebimentoAgendamentoService;
    private readonly ISessaoCaixaNegocioService _sessaoCaixaNegocioService;
    private readonly IClienteNegocioService _clienteNegocioService;
    private readonly IAuditoriaConsultaNegocioService _auditoriaConsultaNegocioService;
    private readonly IAvaliacaoResumoService _avaliacaoResumoService;

    public EstabelecimentosController(
        IEstabelecimentoPerfilService estabelecimentoPerfilService,
        IEquipeNegocioService equipeNegocioService,
        IAgendaNegocioService agendaNegocioService,
        IAtendimentoProfissionalService atendimentoProfissionalService,
        ICaixaNegocioService caixaNegocioService,
        IProfissionalServicoNegocioService profissionalServicoNegocioService,
        IHorarioFuncionamentoNegocioService horarioFuncionamentoNegocioService,
        IServicoNegocioService servicoNegocioService,
        IHorarioProfissionalNegocioService horarioProfissionalNegocioService,
        IDisponibilidadeAgendaService disponibilidadeAgendaService,
        IAgendamentoNegocioService agendamentoNegocioService,
        IFinanceiroNegocioService financeiroNegocioService,
        IRecebimentoAgendamentoService recebimentoAgendamentoService,
        ISessaoCaixaNegocioService sessaoCaixaNegocioService,
        IClienteNegocioService clienteNegocioService,
        IAuditoriaConsultaNegocioService auditoriaConsultaNegocioService,
        IAvaliacaoResumoService avaliacaoResumoService)
    {
        _estabelecimentoPerfilService = estabelecimentoPerfilService;
        _equipeNegocioService = equipeNegocioService;
        _agendaNegocioService = agendaNegocioService;
        _atendimentoProfissionalService = atendimentoProfissionalService;
        _caixaNegocioService = caixaNegocioService;
        _profissionalServicoNegocioService = profissionalServicoNegocioService;
        _horarioFuncionamentoNegocioService = horarioFuncionamentoNegocioService;
        _servicoNegocioService = servicoNegocioService;
        _horarioProfissionalNegocioService = horarioProfissionalNegocioService;
        _disponibilidadeAgendaService = disponibilidadeAgendaService;
        _agendamentoNegocioService = agendamentoNegocioService;
        _financeiroNegocioService = financeiroNegocioService;
        _recebimentoAgendamentoService = recebimentoAgendamentoService;
        _sessaoCaixaNegocioService = sessaoCaixaNegocioService;
        _clienteNegocioService = clienteNegocioService;
        _auditoriaConsultaNegocioService = auditoriaConsultaNegocioService;
        _avaliacaoResumoService = avaliacaoResumoService;
    }

    [HttpGet("{estabelecimentoId:int}/perfil")]
    [RequerPermissaoNegocio(PermissaoNegocio.NegocioEditar, "estabelecimentoId")]
    public async Task<IActionResult> ObterPerfil(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var perfil = await _estabelecimentoPerfilService.ObterAsync(estabelecimentoId, cancellationToken);
        return Ok(ApiSuccessResponse<EstabelecimentoPerfilResponseDto>.From(
            "Perfil do estabelecimento obtido com sucesso.",
            perfil));
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

    [HttpPost("{estabelecimentoId:int}/whatsapp/solicitar-confirmacao")]
    [RequerPermissaoNegocio(PermissaoNegocio.NegocioEditar, "estabelecimentoId")]
    public async Task<IActionResult> SolicitarConfirmacaoWhatsAppEstabelecimento(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var instrucoes = await _estabelecimentoPerfilService.SolicitarConfirmacaoWhatsAppAsync(
            estabelecimentoId,
            cancellationToken);

        return Ok(ApiSuccessResponse<object>.From(
            "Verifique o WhatsApp e seu e-mail para confirmar o WhatsApp comercial.",
            instrucoes));
    }

    [HttpPost("{estabelecimentoId:int}/whatsapp/confirmar")]
    [RequerPermissaoNegocio(PermissaoNegocio.NegocioEditar, "estabelecimentoId")]
    public IActionResult ConfirmarWhatsAppEstabelecimento() =>
        StatusCode(StatusCodes.Status410Gone, ApiErrorResponse.From(
            "Confirmacao manual por codigo foi descontinuada. Use o link enviado por WhatsApp ou e-mail.",
            "CONFIRMACAO_WHATSAPP_DESCONTINUADA"));

    [HttpPost("{estabelecimentoId:int}/whatsapp/opt-in")]
    [RequerPermissaoNegocio(PermissaoNegocio.NegocioEditar, "estabelecimentoId")]
    public async Task<IActionResult> AtualizarWhatsAppOptInEstabelecimento(
        int estabelecimentoId,
        [FromBody] WhatsAppOptInEstabelecimentoRequestDto request,
        CancellationToken cancellationToken)
    {
        await _estabelecimentoPerfilService.AtualizarWhatsAppOptInAsync(
            estabelecimentoId,
            request.OptIn,
            cancellationToken);

        return NoContent();
    }

    [HttpGet("{estabelecimentoId:int}/equipe/usuarios")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Profissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.EquipeGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> ListarUsuariosEquipe(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var usuarios = await _equipeNegocioService.ListarUsuariosAsync(estabelecimentoId, cancellationToken);
        return Ok(ApiSuccessResponse<IReadOnlyList<UsuarioEquipeResponseDto>>.From(
            "Usuarios da equipe listados com sucesso.",
            usuarios));
    }

    [HttpGet("{estabelecimentoId:int}/equipe/profissionais")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Profissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ProfissionalGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> ListarProfissionaisEquipe(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var profissionais = await _equipeNegocioService.ListarProfissionaisAsync(
            estabelecimentoId,
            cancellationToken);
        return Ok(ApiSuccessResponse<IReadOnlyList<ProfissionalEquipeResponseDto>>.From(
            "Profissionais da equipe listados com sucesso.",
            profissionais));
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

    [HttpPost("{estabelecimentoId:int}/profissionais/vitrine")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.HorariosAtendimento, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ProfissionalGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> CadastrarProfissionalVitrine(
        int estabelecimentoId,
        [FromBody] CadastrarProfissionalVitrineRequestDto request,
        CancellationToken cancellationToken)
    {
        var profissional = await _equipeNegocioService.CadastrarProfissionalVitrineAsync(
            estabelecimentoId,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<ProfissionalVitrineResponseDto>.From(
                "Profissional de vitrine cadastrado com sucesso.",
                profissional));
    }

    [HttpGet("{estabelecimentoId:int}/profissionais/vitrine")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.HorariosAtendimento, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ProfissionalGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> ListarProfissionaisVitrine(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var profissionais = await _equipeNegocioService.ListarProfissionaisVitrineAsync(
            estabelecimentoId,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ProfissionalVitrineResponseDto>>.From(
            "Profissionais de vitrine listados com sucesso.",
            profissionais));
    }

    [HttpPatch("{estabelecimentoId:int}/profissionais/vitrine/{profissionalId:int}")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.HorariosAtendimento, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ProfissionalGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> AtualizarProfissionalVitrine(
        int estabelecimentoId,
        int profissionalId,
        [FromBody] AtualizarProfissionalVitrineRequestDto request,
        CancellationToken cancellationToken)
    {
        var profissional = await _equipeNegocioService.AtualizarProfissionalVitrineAsync(
            estabelecimentoId,
            profissionalId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<ProfissionalVitrineResponseDto>.From(
            "Profissional de vitrine atualizado com sucesso.",
            profissional));
    }

    [HttpPatch("{estabelecimentoId:int}/profissionais/vitrine/{profissionalId:int}/status")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.HorariosAtendimento, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ProfissionalGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> AtualizarStatusProfissionalVitrine(
        int estabelecimentoId,
        int profissionalId,
        [FromBody] AtualizarStatusProfissionalVitrineRequestDto request,
        CancellationToken cancellationToken)
    {
        var profissional = await _equipeNegocioService.AtualizarStatusProfissionalVitrineAsync(
            estabelecimentoId,
            profissionalId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<ProfissionalVitrineResponseDto>.From(
            "Status do profissional de vitrine atualizado com sucesso.",
            profissional));
    }

    [HttpGet("{estabelecimentoId:int}/equipe/profissionais/{profissionalId:int}/agendamentos-futuros")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Profissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ProfissionalGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> ListarAgendamentosFuturosProfissionalEquipe(
        int estabelecimentoId,
        int profissionalId,
        CancellationToken cancellationToken)
    {
        var agendamentos = await _equipeNegocioService.ListarAgendamentosFuturosProfissionalAsync(
            estabelecimentoId,
            profissionalId,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<AgendamentoFuturoEquipeResponseDto>>.From(
            "Agendamentos futuros listados com sucesso.",
            agendamentos));
    }

    [HttpPost("{estabelecimentoId:int}/equipe/profissionais/{profissionalId:int}/agendamentos-futuros/cancelar")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Profissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ProfissionalGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> CancelarAgendamentosFuturosProfissionalEquipe(
        int estabelecimentoId,
        int profissionalId,
        [FromBody] CancelarAgendamentosFuturosProfissionalEquipeRequestDto request,
        CancellationToken cancellationToken)
    {
        var resultado = await _equipeNegocioService.CancelarAgendamentosFuturosProfissionalAsync(
            estabelecimentoId,
            profissionalId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<CancelarAgendamentosFuturosProfissionalEquipeResponseDto>.From(
            "Agendamentos futuros cancelados com sucesso.",
            resultado));
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

        return Ok(ApiSuccessResponse<AgendaPaginadaResponseDto<AgendaGeralResponseDto>>.From(
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

        return Ok(ApiSuccessResponse<AgendaPaginadaResponseDto<AgendaProfissionalResponseDto>>.From(
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
        var resultado = await _caixaNegocioService.ListarLancamentosAsync(
            estabelecimentoId,
            filtro,
            cancellationToken);

        return Ok(ApiSuccessResponse<LancamentoCaixaPaginadoResponseDto>.From(
            "Lancamentos do caixa listados com sucesso.",
            resultado));
    }

    [HttpGet("{estabelecimentoId:int}/financeiro/resumo")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Financeiro, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ObterFinanceiroResumo(
        int estabelecimentoId,
        [FromQuery] FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var resumo = await _financeiroNegocioService.ObterResumoAsync(
            estabelecimentoId,
            filtro,
            cancellationToken);

        return Ok(ApiSuccessResponse<FinanceiroResumoResponseDto>.From(
            "Resumo financeiro obtido com sucesso.",
            resumo));
    }

    [HttpGet("{estabelecimentoId:int}/financeiro/relatorios")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Financeiro, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ListarRelatorioFinanceiro(
        int estabelecimentoId,
        [FromQuery] FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var lancamentos = await _financeiroNegocioService.ListarRelatorioAsync(
            estabelecimentoId,
            filtro,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<LancamentoCaixaResponseDto>>.From(
            "Relatorio financeiro listado com sucesso.",
            lancamentos));
    }

    [HttpGet("{estabelecimentoId:int}/financeiro/comissoes")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.ComissaoProfissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ListarComissoesFinanceiro(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var comissoes = await _financeiroNegocioService.ListarComissoesAsync(
            estabelecimentoId,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ComissaoProfissionalResponseDto>>.From(
            "Comissoes listadas com sucesso.",
            comissoes));
    }

    [HttpPost("{estabelecimentoId:int}/caixa/lancamentos")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Caixa, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> RegistrarAjusteCaixa(
        int estabelecimentoId,
        [FromBody] RegistrarAjusteCaixaRequestDto request,
        CancellationToken cancellationToken)
    {
        var lancamento = await _caixaNegocioService.RegistrarAjusteManualAsync(
            estabelecimentoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<LancamentoCaixaResponseDto>.From(
            "Lancamento registrado com sucesso.",
            lancamento));
    }

    [HttpPost("{estabelecimentoId:int}/caixa/lancamentos/{lancamentoId:int}/estornar")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Caixa, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> EstornarLancamentoCaixa(
        int estabelecimentoId,
        int lancamentoId,
        [FromBody] EstornarLancamentoCaixaRequestDto request,
        CancellationToken cancellationToken)
    {
        var lancamento = await _caixaNegocioService.EstornarLancamentoAsync(
            estabelecimentoId,
            lancamentoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<LancamentoCaixaResponseDto>.From(
            "Lancamento estornado com sucesso.",
            lancamento));
    }

    [HttpGet("{estabelecimentoId:int}/caixa/sessoes/atual")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Caixa, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ObterSessaoCaixaAtual(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var sessao = await _sessaoCaixaNegocioService.ObterSessaoAtualAsync(
            estabelecimentoId,
            cancellationToken);

        return Ok(ApiSuccessResponse<SessaoCaixaResponseDto>.From(
            "Sessao de caixa obtida com sucesso.",
            sessao));
    }

    [HttpPost("{estabelecimentoId:int}/caixa/sessoes/abrir")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Caixa, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> AbrirSessaoCaixa(
        int estabelecimentoId,
        [FromBody] AbrirSessaoCaixaRequestDto request,
        CancellationToken cancellationToken)
    {
        var sessao = await _sessaoCaixaNegocioService.AbrirSessaoAsync(
            estabelecimentoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<SessaoCaixaResponseDto>.From(
            "Sessao de caixa aberta com sucesso.",
            sessao));
    }

    [HttpPost("{estabelecimentoId:int}/caixa/sessoes/{sessaoId:int}/fechar")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Caixa, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> FecharSessaoCaixa(
        int estabelecimentoId,
        int sessaoId,
        [FromBody] FecharSessaoCaixaRequestDto request,
        CancellationToken cancellationToken)
    {
        var sessao = await _sessaoCaixaNegocioService.FecharSessaoAsync(
            estabelecimentoId,
            sessaoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<SessaoCaixaResponseDto>.From(
            "Sessao de caixa fechada com sucesso.",
            sessao));
    }

    [HttpPost("{estabelecimentoId:int}/financeiro/comissoes")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.ComissaoProfissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> CriarComissaoFinanceiro(
        int estabelecimentoId,
        [FromBody] CriarComissaoProfissionalRequestDto request,
        CancellationToken cancellationToken)
    {
        var comissao = await _financeiroNegocioService.CriarComissaoAsync(
            estabelecimentoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<ComissaoProfissionalResponseDto>.From(
            "Comissao criada com sucesso.",
            comissao));
    }

    [HttpPut("{estabelecimentoId:int}/financeiro/comissoes/{comissaoId:int}")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.ComissaoProfissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> AtualizarComissaoFinanceiro(
        int estabelecimentoId,
        int comissaoId,
        [FromBody] AtualizarComissaoProfissionalRequestDto request,
        CancellationToken cancellationToken)
    {
        var comissao = await _financeiroNegocioService.AtualizarComissaoAsync(
            estabelecimentoId,
            comissaoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<ComissaoProfissionalResponseDto>.From(
            "Comissao atualizada com sucesso.",
            comissao));
    }

    [HttpPatch("{estabelecimentoId:int}/financeiro/comissoes/{comissaoId:int}/desativar")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.ComissaoProfissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> DesativarComissaoFinanceiro(
        int estabelecimentoId,
        int comissaoId,
        CancellationToken cancellationToken)
    {
        await _financeiroNegocioService.DesativarComissaoAsync(
            estabelecimentoId,
            comissaoId,
            cancellationToken);

        return Ok(ApiSuccessResponse<object>.From(
            "Comissao desativada com sucesso.",
            new { comissaoId }));
    }

    [HttpGet("{estabelecimentoId:int}/financeiro/comissoes/minhas")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.ComissaoProfissionais, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ComissaoVisualizarPropria, "estabelecimentoId")]
    public async Task<IActionResult> ListarMinhasComissoes(
        int estabelecimentoId,
        [FromQuery] FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var extrato = await _financeiroNegocioService.ListarMinhasComissoesAsync(
            estabelecimentoId,
            filtro,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ComissaoProfissionalExtratoDto>>.From(
            "Extrato de comissoes obtido com sucesso.",
            extrato));
    }

    [HttpGet("{estabelecimentoId:int}/financeiro/relatorios/analitico")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Financeiro, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ObterRelatorioAnalitico(
        int estabelecimentoId,
        [FromQuery] FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var relatorio = await _financeiroNegocioService.ObterRelatorioAnaliticoAsync(
            estabelecimentoId,
            filtro,
            cancellationToken);

        return Ok(ApiSuccessResponse<RelatorioAnaliticoResponseDto>.From(
            "Relatorio analitico obtido com sucesso.",
            relatorio));
    }

    [HttpGet("{estabelecimentoId:int}/financeiro/fluxo-caixa")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Financeiro, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ObterFluxoCaixa(
        int estabelecimentoId,
        [FromQuery] FinanceiroFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var fluxo = await _financeiroNegocioService.ObterFluxoCaixaAsync(
            estabelecimentoId,
            filtro,
            cancellationToken);

        return Ok(ApiSuccessResponse<FluxoCaixaResponseDto>.From(
            "Fluxo de caixa obtido com sucesso.",
            fluxo));
    }

    [HttpGet("{estabelecimentoId:int}/financeiro/relatorios/export")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Financeiro, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ExportarRelatorioFinanceiro(
        int estabelecimentoId,
        [FromQuery] FinanceiroFiltroDto filtro,
        [FromQuery] string formato = "csv",
        CancellationToken cancellationToken = default)
    {
        var exportacao = await _financeiroNegocioService.ExportarRelatorioAsync(
            estabelecimentoId,
            filtro,
            formato,
            cancellationToken);

        return File(exportacao.Conteudo, exportacao.ContentType, exportacao.NomeArquivo);
    }

    [HttpGet("{estabelecimentoId:int}/financeiro/busca")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Financeiro, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> BuscarFinanceiro(
        int estabelecimentoId,
        [FromQuery] string q,
        [FromQuery] string? tipo,
        CancellationToken cancellationToken)
    {
        var resultado = await _financeiroNegocioService.BuscarAsync(
            estabelecimentoId,
            q,
            tipo,
            cancellationToken);

        return Ok(ApiSuccessResponse<FinanceiroBuscaResponseDto>.From(
            "Busca financeira realizada com sucesso.",
            resultado));
    }

    [HttpGet("{estabelecimentoId:int}/financeiro/contas-receber")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Financeiro, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ListarContasReceber(
        int estabelecimentoId,
        [FromQuery] ContaFinanceiraStatus? status,
        CancellationToken cancellationToken)
    {
        var contas = await _financeiroNegocioService.ListarContasReceberAsync(
            estabelecimentoId,
            status,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ContaReceberResponseDto>>.From(
            "Contas a receber listadas com sucesso.",
            contas));
    }

    [HttpPost("{estabelecimentoId:int}/financeiro/contas-receber")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Financeiro, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> CriarContaReceber(
        int estabelecimentoId,
        [FromBody] CriarContaReceberRequestDto request,
        CancellationToken cancellationToken)
    {
        var conta = await _financeiroNegocioService.CriarContaReceberAsync(
            estabelecimentoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<ContaReceberResponseDto>.From(
            "Conta a receber criada com sucesso.",
            conta));
    }

    [HttpPost("{estabelecimentoId:int}/financeiro/contas-receber/{contaId:int}/baixar")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Financeiro, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> BaixarContaReceber(
        int estabelecimentoId,
        int contaId,
        [FromBody] BaixarContaRequestDto request,
        CancellationToken cancellationToken)
    {
        var conta = await _financeiroNegocioService.BaixarContaReceberAsync(
            estabelecimentoId,
            contaId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<ContaReceberResponseDto>.From(
            "Conta a receber baixada com sucesso.",
            conta));
    }

    [HttpPatch("{estabelecimentoId:int}/financeiro/contas-receber/{contaId:int}/cancelar")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Financeiro, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> CancelarContaReceber(
        int estabelecimentoId,
        int contaId,
        CancellationToken cancellationToken)
    {
        var conta = await _financeiroNegocioService.CancelarContaReceberAsync(
            estabelecimentoId,
            contaId,
            cancellationToken);

        return Ok(ApiSuccessResponse<ContaReceberResponseDto>.From(
            "Conta a receber cancelada com sucesso.",
            conta));
    }

    [HttpGet("{estabelecimentoId:int}/financeiro/contas-pagar")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Financeiro, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ListarContasPagar(
        int estabelecimentoId,
        [FromQuery] ContaFinanceiraStatus? status,
        CancellationToken cancellationToken)
    {
        var contas = await _financeiroNegocioService.ListarContasPagarAsync(
            estabelecimentoId,
            status,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ContaPagarResponseDto>>.From(
            "Contas a pagar listadas com sucesso.",
            contas));
    }

    [HttpPost("{estabelecimentoId:int}/financeiro/contas-pagar")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Financeiro, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> CriarContaPagar(
        int estabelecimentoId,
        [FromBody] CriarContaPagarRequestDto request,
        CancellationToken cancellationToken)
    {
        var conta = await _financeiroNegocioService.CriarContaPagarAsync(
            estabelecimentoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<ContaPagarResponseDto>.From(
            "Conta a pagar criada com sucesso.",
            conta));
    }

    [HttpPost("{estabelecimentoId:int}/financeiro/contas-pagar/{contaId:int}/baixar")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Financeiro, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> BaixarContaPagar(
        int estabelecimentoId,
        int contaId,
        [FromBody] BaixarContaRequestDto request,
        CancellationToken cancellationToken)
    {
        var conta = await _financeiroNegocioService.BaixarContaPagarAsync(
            estabelecimentoId,
            contaId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<ContaPagarResponseDto>.From(
            "Conta a pagar baixada com sucesso.",
            conta));
    }

    [HttpPatch("{estabelecimentoId:int}/financeiro/contas-pagar/{contaId:int}/cancelar")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Financeiro, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> CancelarContaPagar(
        int estabelecimentoId,
        int contaId,
        CancellationToken cancellationToken)
    {
        var conta = await _financeiroNegocioService.CancelarContaPagarAsync(
            estabelecimentoId,
            contaId,
            cancellationToken);

        return Ok(ApiSuccessResponse<ContaPagarResponseDto>.From(
            "Conta a pagar cancelada com sucesso.",
            conta));
    }

    [HttpGet("{estabelecimentoId:int}/financeiro/conciliacao")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Financeiro, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ListarConciliacao(
        int estabelecimentoId,
        [FromQuery] bool? conciliado,
        CancellationToken cancellationToken)
    {
        var itens = await _financeiroNegocioService.ListarConciliacaoAsync(
            estabelecimentoId,
            conciliado,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ConciliacaoItemResponseDto>>.From(
            "Itens de conciliacao listados com sucesso.",
            itens));
    }

    [HttpPost("{estabelecimentoId:int}/financeiro/conciliacao/importar")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Financeiro, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> ImportarConciliacao(
        int estabelecimentoId,
        [FromBody] ConciliacaoImportacaoRequestDto request,
        CancellationToken cancellationToken)
    {
        var itens = await _financeiroNegocioService.ImportarConciliacaoCsvAsync(
            estabelecimentoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ConciliacaoItemResponseDto>>.From(
            "Conciliacao importada com sucesso.",
            itens));
    }

    [HttpPost("{estabelecimentoId:int}/agendamentos/{agendamentoId:int}/receber")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Caixa, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.CaixaGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> ReceberAgendamentoPresencial(
        int estabelecimentoId,
        int agendamentoId,
        [FromBody] ReceberAgendamentoRequestDto request,
        CancellationToken cancellationToken)
    {
        var resultado = await _recebimentoAgendamentoService.ReceberPresencialAsync(
            estabelecimentoId,
            agendamentoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<ReceberAgendamentoResponseDto>.From(
            "Agendamento recebido com sucesso.",
            resultado));
    }

    [HttpGet("{estabelecimentoId:int}/servicos")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Servicos, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ServicoVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ListarServicos(
        int estabelecimentoId,
        [FromQuery] ServicoFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var servicos = await _servicoNegocioService.ListarAsync(
            estabelecimentoId,
            filtro,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ServicoResponseDto>>.From(
            "Servicos listados com sucesso.",
            servicos));
    }

    [HttpPost("{estabelecimentoId:int}/servicos")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Servicos, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ServicoGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> CriarServico(
        int estabelecimentoId,
        [FromBody] CriarServicoRequestDto request,
        CancellationToken cancellationToken)
    {
        var servico = await _servicoNegocioService.CriarAsync(
            estabelecimentoId,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiSuccessResponse<ServicoResponseDto>.From(
                "Servico criado com sucesso.",
                servico));
    }

    [HttpPut("{estabelecimentoId:int}/servicos/{servicoId:int}")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Servicos, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ServicoGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> AtualizarServico(
        int estabelecimentoId,
        int servicoId,
        [FromBody] AtualizarServicoRequestDto request,
        CancellationToken cancellationToken)
    {
        var servico = await _servicoNegocioService.AtualizarAsync(
            estabelecimentoId,
            servicoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<ServicoResponseDto>.From(
            "Servico atualizado com sucesso.",
            servico));
    }

    [HttpPatch("{estabelecimentoId:int}/servicos/{servicoId:int}/status")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Servicos, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ServicoGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> AtualizarStatusServico(
        int estabelecimentoId,
        int servicoId,
        [FromBody] AtualizarStatusServicoRequestDto request,
        CancellationToken cancellationToken)
    {
        var servico = await _servicoNegocioService.AtualizarStatusAsync(
            estabelecimentoId,
            servicoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<ServicoResponseDto>.From(
            "Status do servico atualizado com sucesso.",
            servico));
    }

    [HttpPut("{estabelecimentoId:int}/servicos/{servicoId:int}/profissionais/{profissionalId:int}")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Servicos, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ServicoGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> AtualizarVinculoServicoProfissional(
        int estabelecimentoId,
        int servicoId,
        int profissionalId,
        [FromBody] AtualizarProfissionalServicoRequestDto request,
        CancellationToken cancellationToken)
    {
        var vinculo = await _profissionalServicoNegocioService.AtualizarAsync(
            estabelecimentoId,
            profissionalId,
            servicoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<ProfissionalServicoResponseDto>.From(
            "Vinculo do servico com o profissional atualizado com sucesso.",
            vinculo));
    }

    [HttpPatch("{estabelecimentoId:int}/servicos/{servicoId:int}/profissionais/{profissionalId:int}/status")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Servicos, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ServicoGerenciar, "estabelecimentoId")]
    public async Task<IActionResult> DesvincularServicoProfissional(
        int estabelecimentoId,
        int servicoId,
        int profissionalId,
        CancellationToken cancellationToken)
    {
        var vinculo = await _profissionalServicoNegocioService.DesvincularAsync(
            estabelecimentoId,
            profissionalId,
            servicoId,
            cancellationToken);

        return Ok(ApiSuccessResponse<ProfissionalServicoResponseDto>.From(
            "Profissional desvinculado do servico com sucesso.",
            vinculo));
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

    [HttpPost("{estabelecimentoId:int}/agendamentos/{agendamentoId:int}/confirmar")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Agenda, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.AgendaCriar, "estabelecimentoId")]
    public async Task<IActionResult> ConfirmarAgendamento(
        int estabelecimentoId,
        int agendamentoId,
        CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoNegocioService.ConfirmarAsync(
            estabelecimentoId,
            agendamentoId,
            cancellationToken);

        return Ok(ApiSuccessResponse<AgendamentoCriadoResponseDto>.From(
            "Agendamento confirmado com sucesso.",
            agendamento));
    }

    [HttpPost("{estabelecimentoId:int}/agendamentos/{agendamentoId:int}/cancelar")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Agenda, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.AgendaCancelar, "estabelecimentoId")]
    public async Task<IActionResult> CancelarAgendamento(
        int estabelecimentoId,
        int agendamentoId,
        [FromBody] CancelarAgendamentoRequestDto request,
        CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoNegocioService.CancelarAsync(
            estabelecimentoId,
            agendamentoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<AgendamentoCriadoResponseDto>.From(
            "Agendamento cancelado com sucesso.",
            agendamento));
    }

    [HttpPost("{estabelecimentoId:int}/agendamentos/{agendamentoId:int}/sugerir-remarcacao")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Agenda, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.AgendaReagendar, "estabelecimentoId")]
    public async Task<IActionResult> SugerirRemarcacaoAgendamento(
        int estabelecimentoId,
        int agendamentoId,
        [FromBody] RemarcarAgendamentoRequestDto request,
        CancellationToken cancellationToken)
    {
        var proposta = await _agendamentoNegocioService.SugerirRemarcacaoAsync(
            estabelecimentoId,
            agendamentoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<PropostaRemarcacaoResponseDto>.From(
            "Sugestao de remarcacao enviada com sucesso.",
            proposta));
    }

    [HttpPost("{estabelecimentoId:int}/agendamentos/{agendamentoId:int}/remarcar")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Agenda, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.AgendaReagendar, "estabelecimentoId")]
    public async Task<IActionResult> RemarcarAgendamento(
        int estabelecimentoId,
        int agendamentoId,
        [FromBody] RemarcarAgendamentoRequestDto request,
        CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoNegocioService.RemarcarAsync(
            estabelecimentoId,
            agendamentoId,
            request,
            cancellationToken);

        return Ok(ApiSuccessResponse<AgendamentoCriadoResponseDto>.From(
            "Agendamento remarcado com sucesso.",
            agendamento));
    }

    [HttpPatch("{estabelecimentoId:int}/agendamentos/{agendamentoId:int}/nao-compareceu")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Agenda, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.AgendaCancelar, "estabelecimentoId")]
    public async Task<IActionResult> MarcarNaoCompareceu(
        int estabelecimentoId,
        int agendamentoId,
        CancellationToken cancellationToken)
    {
        var agendamento = await _agendamentoNegocioService.MarcarNaoCompareceuAsync(
            estabelecimentoId,
            agendamentoId,
            cancellationToken);

        return Ok(ApiSuccessResponse<AgendamentoCriadoResponseDto>.From(
            "Agendamento marcado como nao compareceu.",
            agendamento));
    }

    [HttpGet("{estabelecimentoId:int}/agendamentos/{agendamentoId:int}/historico")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Agenda, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.AgendaVisualizarGeral, "estabelecimentoId")]
    public async Task<IActionResult> ObterHistoricoAgendamento(
        int estabelecimentoId,
        int agendamentoId,
        CancellationToken cancellationToken)
    {
        var historico = await _agendamentoNegocioService.ObterHistoricoAsync(
            estabelecimentoId,
            agendamentoId,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<AgendamentoHistoricoResponseDto>>.From(
            "Historico do agendamento listado com sucesso.",
            historico));
    }

    [HttpGet("{estabelecimentoId:int}/clientes")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Clientes, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.ClienteVisualizarGeral, "estabelecimentoId")]
    public async Task<IActionResult> ListarClientes(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var clientes = await _clienteNegocioService.ListarPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<ClienteNegocioResponseDto>>.From(
            "Clientes listados com sucesso.",
            clientes));
    }

    [HttpGet("{estabelecimentoId:int}/auditoria")]
    [RequerModuloAssinatura(TipoAssinatura.Estabelecimento, ModuloAssinatura.Financeiro, "estabelecimentoId")]
    [RequerPermissaoNegocio(PermissaoNegocio.NegocioVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ListarAuditoria(
        int estabelecimentoId,
        [FromQuery] int limite = 50,
        CancellationToken cancellationToken = default)
    {
        var registros = await _auditoriaConsultaNegocioService.ListarRecentesAsync(
            estabelecimentoId,
            limite,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<AuditoriaNegocioResponseDto>>.From(
            "Auditoria listada com sucesso.",
            registros));
    }

    [HttpGet("{estabelecimentoId:int}/avaliacoes")]
    [RequerPermissaoNegocio(PermissaoNegocio.NegocioVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ListarAvaliacoes(
        int estabelecimentoId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _avaliacaoResumoService.ListarNegocioAsync(
            estabelecimentoId,
            pagina,
            tamanhoPagina,
            cancellationToken);

        return Ok(ApiSuccessResponse<AvaliacoesNegocioPaginadasResponseDto>.From(
            "Avaliacoes listadas com sucesso.",
            resultado));
    }

    [HttpGet("{estabelecimentoId:int}/avaliacoes/resumo")]
    [RequerPermissaoNegocio(PermissaoNegocio.NegocioVisualizar, "estabelecimentoId")]
    public async Task<IActionResult> ObterResumoAvaliacoes(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var resumo = await _avaliacaoResumoService.ObterResumoEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        return Ok(ApiSuccessResponse<AvaliacaoResumoPublicoDto>.From(
            "Resumo de avaliacoes obtido com sucesso.",
            resumo));
    }
}
