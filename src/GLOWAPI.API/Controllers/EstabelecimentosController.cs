using GLOWAPI.API.Attributes;
using GLOWAPI.API.Models;
using GLOWAPI.Application.DTOs.Agendamento;
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
    private readonly IConfirmacaoWhatsAppEstabelecimentoService _confirmacaoWhatsAppEstabelecimentoService;
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

    public EstabelecimentosController(
        IEstabelecimentoPerfilService estabelecimentoPerfilService,
        IConfirmacaoWhatsAppEstabelecimentoService confirmacaoWhatsAppEstabelecimentoService,
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
        IFinanceiroNegocioService financeiroNegocioService)
    {
        _estabelecimentoPerfilService = estabelecimentoPerfilService;
        _confirmacaoWhatsAppEstabelecimentoService = confirmacaoWhatsAppEstabelecimentoService;
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
            "Verifique seu e-mail para confirmar o WhatsApp comercial.",
            instrucoes));
    }

    [HttpPost("{estabelecimentoId:int}/whatsapp/confirmar")]
    [RequerPermissaoNegocio(PermissaoNegocio.NegocioEditar, "estabelecimentoId")]
    public async Task<IActionResult> ConfirmarWhatsAppEstabelecimento(
        int estabelecimentoId,
        [FromBody] ConfirmarWhatsAppEstabelecimentoRequestDto request,
        CancellationToken cancellationToken)
    {
        await _confirmacaoWhatsAppEstabelecimentoService.ConfirmarPorCodigoAsync(
            estabelecimentoId,
            request.Codigo,
            cancellationToken);

        return Ok(ApiSuccessResponse.From("WhatsApp do estabelecimento confirmado com sucesso."));
    }

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
        var lancamentos = await _caixaNegocioService.ListarLancamentosAsync(
            estabelecimentoId,
            filtro,
            cancellationToken);

        return Ok(ApiSuccessResponse<IReadOnlyList<LancamentoCaixaResponseDto>>.From(
            "Lancamentos do caixa listados com sucesso.",
            lancamentos));
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
}
