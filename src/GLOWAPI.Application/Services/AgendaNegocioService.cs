using GLOWAPI.Application.DTOs.Agenda;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Agenda;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Services;

public class AgendaNegocioService : IAgendaNegocioService
{
    private readonly IAgendamentoRepository _agendamentoRepository;
    private readonly IAgendamentoItemRepository _agendamentoItemRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IProfissionalEscopoAcessoService _profissionalEscopoAcessoService;

    public AgendaNegocioService(
        IAgendamentoRepository agendamentoRepository,
        IAgendamentoItemRepository agendamentoItemRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IProfissionalEscopoAcessoService profissionalEscopoAcessoService)
    {
        _agendamentoRepository = agendamentoRepository;
        _agendamentoItemRepository = agendamentoItemRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _profissionalEscopoAcessoService = profissionalEscopoAcessoService;
    }

    public async Task<IReadOnlyList<AgendaGeralResponseDto>> ListarAgendaGeralAsync(
        int estabelecimentoId,
        AgendaGeralFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.AgendaVisualizarGeral,
            cancellationToken);

        var agendamentos = await _agendamentoRepository.ListarAgendaGeralAsync(
            new AgendaGeralFiltro(
                estabelecimentoId,
                filtro.ProfissionalId,
                filtro.ClienteId,
                filtro.Status,
                filtro.Inicio,
                filtro.Fim),
            cancellationToken);

        return agendamentos.Select(AgendaGeralResponseDto.From).ToList();
    }

    public async Task<IReadOnlyList<AgendaProfissionalResponseDto>> ListarAgendaProfissionalAsync(
        int estabelecimentoId,
        AgendaProfissionalFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.AgendaVisualizarPropria,
            cancellationToken);

        var escopo = await _profissionalEscopoAcessoService.ObterEscopoAsync(
            estabelecimentoId,
            cancellationToken);

        var itens = await _agendamentoItemRepository.ListarAgendaProfissionalAsync(
            new AgendaProfissionalFiltro(
                estabelecimentoId,
                escopo.ProfissionalId,
                filtro.Status,
                filtro.Inicio,
                filtro.Fim),
            cancellationToken);

        return itens.Select(AgendaProfissionalResponseDto.From).ToList();
    }
}
