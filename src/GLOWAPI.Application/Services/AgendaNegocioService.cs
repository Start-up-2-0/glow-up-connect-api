using GLOWAPI.Application.DTOs.Agenda;
using GLOWAPI.Application.Helpers;
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

    public async Task<AgendaPaginadaResponseDto<AgendaGeralResponseDto>> ListarAgendaGeralAsync(
        int estabelecimentoId,
        AgendaGeralFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.AgendaVisualizarGeral,
            cancellationToken);

        var (inicio, fim) = AgendaPeriodoConsulta.ResolverIntervaloMesAtualUtc(filtro.Inicio, filtro.Fim);
        var (pagina, tamanhoPagina) = AgendaPeriodoConsulta.ResolverPaginacao(filtro.Pagina, filtro.TamanhoPagina);

        var (agendamentos, total) = await _agendamentoRepository.ListarAgendaGeralAsync(
            new AgendaGeralFiltro(
                estabelecimentoId,
                filtro.ProfissionalId,
                filtro.ClienteId,
                filtro.Status,
                inicio,
                fim,
                pagina,
                tamanhoPagina,
                AgendaOrdenacaoConsulta.Normalizar(filtro.Ordenacao)),
            cancellationToken);

        return new AgendaPaginadaResponseDto<AgendaGeralResponseDto>(
            total,
            pagina,
            tamanhoPagina,
            agendamentos.Select(AgendaGeralResponseDto.From).ToList());
    }

    public async Task<AgendaPaginadaResponseDto<AgendaProfissionalResponseDto>> ListarAgendaProfissionalAsync(
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

        var (inicio, fim) = AgendaPeriodoConsulta.ResolverIntervaloMesAtualUtc(filtro.Inicio, filtro.Fim);
        var (pagina, tamanhoPagina) = AgendaPeriodoConsulta.ResolverPaginacao(filtro.Pagina, filtro.TamanhoPagina);

        var (itens, total) = await _agendamentoItemRepository.ListarAgendaProfissionalAsync(
            new AgendaProfissionalFiltro(
                estabelecimentoId,
                escopo.ProfissionalId,
                filtro.Status,
                inicio,
                fim,
                pagina,
                tamanhoPagina,
                AgendaOrdenacaoConsulta.Normalizar(filtro.Ordenacao)),
            cancellationToken);

        return new AgendaPaginadaResponseDto<AgendaProfissionalResponseDto>(
            total,
            pagina,
            tamanhoPagina,
            itens.Select(AgendaProfissionalResponseDto.From).ToList());
    }
}
