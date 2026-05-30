using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class DisponibilidadeAgendaServiceTests
{
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentoRepository = new();
    private readonly Mock<IProfissionalRepository> _profissionalRepository = new();
    private readonly Mock<IServicoRepository> _servicoRepository = new();
    private readonly Mock<IProfissionalServicoRepository> _profissionalServicoRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<IHorarioFuncionamentoEstabelecimentoRepository> _horarioFuncionamentoRepository = new();
    private readonly Mock<IHorarioAtendimentoProfissionalRepository> _horarioAtendimentoRepository = new();
    private readonly Mock<IAgendamentoItemRepository> _agendamentoItemRepository = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();

    [Fact]
    public async Task ConsultarPorEstabelecimentoAsync_DeveRetornarSlotsDisponiveis()
    {
        var segunda = ObterProximaSegunda();
        ConfigurarCenarioBasico(segunda);

        var service = CreateService();
        var response = await service.ConsultarPorEstabelecimentoAsync(
            20,
            new ConsultarDisponibilidadeAgendaDto
            {
                DataInicio = segunda,
                DataFim = segunda,
                ServicoId = 5,
                ProfissionalId = 40
            });

        Assert.NotEmpty(response.Slots);
        Assert.All(response.Slots, slot => Assert.Equal(40, slot.ProfissionalId));
    }

    [Fact]
    public async Task ConsultarPublicoPorEstabelecimentoAsync_DeveLancarExcecao_QuandoNegocioNaoExiste()
    {
        _estabelecimentoRepository
            .Setup(r => r.ObterPorPublicGuidAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Estabelecimento?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<NegocioNaoEncontradoException>(() =>
            service.ConsultarPublicoPorEstabelecimentoAsync(
                Guid.NewGuid(),
                new ConsultarDisponibilidadeAgendaDto
                {
                    DataInicio = DateOnly.FromDateTime(DateTime.UtcNow),
                    DataFim = DateOnly.FromDateTime(DateTime.UtcNow),
                    ServicoId = 5
                }));
    }

    [Fact]
    public async Task ConsultarPorEstabelecimentoAsync_DeveUsarDuracaoEfetivaDoVinculo()
    {
        var segunda = ObterProximaSegunda();
        ConfigurarCenarioBasico(segunda);

        _profissionalServicoRepository
            .Setup(r => r.ObterPorProfissionalEServicoAsync(40, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalServico
            {
                ProfissionalId = 40,
                ServicoId = 5,
                DuracaoMinutos = 30,
                Preco = 100,
                Ativo = true
            });

        var service = CreateService();
        var response = await service.ConsultarPorEstabelecimentoAsync(
            20,
            new ConsultarDisponibilidadeAgendaDto
            {
                DataInicio = segunda,
                DataFim = segunda,
                ServicoId = 5,
                ProfissionalId = 40
            });

        Assert.Equal(30, response.DuracaoMinutos);
        Assert.NotEmpty(response.Slots);
    }

    private void ConfigurarCenarioBasico(DateOnly segunda)
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(20, PermissaoNegocio.AgendaCriar, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GLOWAPI.Application.Models.Autorizacao.AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Owner,
                false,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.AgendaCriar }));

        _servicoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(5, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Servico
            {
                Id = 5,
                EstabelecimentoId = 20,
                DuracaoMinutos = 60,
                Ativo = true
            });

        _profissionalServicoRepository
            .Setup(r => r.ExisteAtivoAsync(40, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterPorProfissionalAsync(40, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalEstabelecimento
            {
                ProfissionalId = 40,
                EstabelecimentoId = 20,
                Ativo = true,
                PodeReceberAgendamento = true
            });

        _horarioFuncionamentoRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoEDiaAsync(20, DayOfWeek.Monday, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new HorarioFuncionamentoEstabelecimento
                {
                    DiaSemana = DayOfWeek.Monday,
                    HoraInicio = new TimeOnly(8, 0),
                    HoraFim = new TimeOnly(18, 0),
                    Ativo = true
                }
            ]);

        _horarioAtendimentoRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(20, 40, DayOfWeek.Monday, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new HorarioAtendimentoProfissional
                {
                    ProfissionalId = 40,
                    EstabelecimentoId = 20,
                    DiaSemana = DayOfWeek.Monday,
                    HoraInicio = new TimeOnly(9, 0),
                    HoraFim = new TimeOnly(17, 0),
                    Ativo = true
                }
            ]);

        _agendamentoItemRepository
            .Setup(r => r.ListarOcupacaoAsync(20, 40, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private static DateOnly ObterProximaSegunda()
    {
        var data = DateOnly.FromDateTime(DateTime.UtcNow);
        while (data.DayOfWeek != DayOfWeek.Monday)
        {
            data = data.AddDays(1);
        }

        return data;
    }

    private DisponibilidadeAgendaService CreateService() =>
        new(
            _estabelecimentoRepository.Object,
            _profissionalRepository.Object,
            _servicoRepository.Object,
            _profissionalServicoRepository.Object,
            _profissionalEstabelecimentoRepository.Object,
            _horarioFuncionamentoRepository.Object,
            _horarioAtendimentoRepository.Object,
            _agendamentoItemRepository.Object,
            _autorizacaoNegocioService.Object);
}
