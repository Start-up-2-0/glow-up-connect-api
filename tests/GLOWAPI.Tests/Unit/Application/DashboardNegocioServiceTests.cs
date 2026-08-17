using GLOWAPI.Application.DTOs.Agenda;
using GLOWAPI.Application.DTOs.Avaliacao;
using GLOWAPI.Application.DTOs.Financeiro;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Autorizacao;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class DashboardNegocioServiceTests
{
    private readonly Mock<IAgendaNegocioService> _agendaNegocioService = new();
    private readonly Mock<IMovimentosFinanceirosService> _movimentosFinanceirosService = new();
    private readonly Mock<IAvaliacaoResumoService> _avaliacaoResumoService = new();
    private readonly Mock<IAgendamentoRepository> _agendamentoRepository = new();
    private readonly Mock<IServicoRepository> _servicoRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();
    private readonly Mock<IModulosAssinaturaService> _modulosAssinaturaService = new();
    private readonly Mock<ILogger<DashboardNegocioService>> _logger = new();

    public DashboardNegocioServiceTests()
    {
        _autorizacaoNegocioService
            .Setup(s => s.ObterContextoAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutorizacaoNegocioResultado(
                10,
                1,
                EstablishmentUserRole.Owner,
                false,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.NegocioVisualizar, PermissaoNegocio.CaixaVisualizar }));

        var agendaVazia = new AgendaPaginadaResponseDto<AgendaGeralResponseDto>(
            0,
            1,
            50,
            Array.Empty<AgendaGeralResponseDto>());

        _agendaNegocioService
            .Setup(s => s.ListarAgendaGeralAsync(10, It.IsAny<AgendaGeralFiltroDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(agendaVazia);

        _agendamentoRepository
            .Setup(r => r.ContarPorEstabelecimentoNoPeriodoAsync(
                10,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _agendamentoRepository
            .Setup(r => r.ContarClientesDistintosPorEstabelecimentoAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _servicoRepository
            .Setup(r => r.ContarAtivosPorEstabelecimentoAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ProfissionalEstabelecimento>());
        _avaliacaoResumoService
            .Setup(s => s.ObterResumoEstabelecimentoAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AvaliacaoResumoPublicoDto(0, 0, 90, Array.Empty<AvaliacaoDistribuicaoItemDto>()));
    }

    [Fact]
    public async Task ObterAsync_NaoDeveConsultarFinanceiro_QuandoPlanoNaoTemModuloCaixaNemFinanceiro()
    {
        _modulosAssinaturaService
            .Setup(s => s.PossuiModuloPorEstabelecimentoAsync(
                10,
                ModuloAssinatura.Caixa,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _modulosAssinaturaService
            .Setup(s => s.PossuiModuloPorEstabelecimentoAsync(
                10,
                ModuloAssinatura.Financeiro,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        var resultado = await service.ObterAsync(10);

        Assert.Equal(0, resultado.TotalGanhoMes);
        Assert.Equal(0, resultado.TotalGanhoHoje);
        Assert.Equal(0, resultado.TotalGanhoSemana);
        _movimentosFinanceirosService.Verify(
            s => s.ObterDashboardAsync(
                It.IsAny<int>(),
                It.IsAny<FinanceiroFiltroDto>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ObterAsync_DeveConsultarFinanceiro_QuandoPossuiModuloCaixa()
    {
        _modulosAssinaturaService
            .Setup(s => s.PossuiModuloPorEstabelecimentoAsync(
                10,
                ModuloAssinatura.Caixa,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _modulosAssinaturaService
            .Setup(s => s.PossuiModuloPorEstabelecimentoAsync(
                10,
                ModuloAssinatura.Financeiro,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _movimentosFinanceirosService
            .Setup(s => s.ObterDashboardAsync(10, It.IsAny<FinanceiroFiltroDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinanceiroDashboardResponseDto(
                100,
                250,
                50,
                200,
                0,
                0,
                0,
                null,
                null));

        var service = CreateService();

        var resultado = await service.ObterAsync(10);

        Assert.Equal(250, resultado.TotalGanhoMes);
        _movimentosFinanceirosService.Verify(
            s => s.ObterDashboardAsync(10, It.IsAny<FinanceiroFiltroDto>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    private DashboardNegocioService CreateService() =>
        new(
            _agendaNegocioService.Object,
            _movimentosFinanceirosService.Object,
            _avaliacaoResumoService.Object,
            _agendamentoRepository.Object,
            _servicoRepository.Object,
            _profissionalEstabelecimentoRepository.Object,
            _autorizacaoNegocioService.Object,
            _modulosAssinaturaService.Object,
            _logger.Object);
}
