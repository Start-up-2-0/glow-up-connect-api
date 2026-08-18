using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.Helpers;
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
        Assert.Contains(segunda, response.DatasAtendimento);
    }

    [Fact]
    public async Task ConsultarPorEstabelecimentoAsync_NaoDeveIncluirDiaSemSlotsLivres()
    {
        var segunda = ObterProximaSegunda();
        ConfigurarCenarioBasico(segunda);

        _agendamentoItemRepository
            .Setup(r => r.ListarOcupacaoAsync(20, 40, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AgendamentoItem
                {
                    ProfissionalId = 40,
                    Inicio = segunda.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc),
                    Fim = segunda.ToDateTime(new TimeOnly(17, 0), DateTimeKind.Utc),
                    Agendamento = new Agendamento { Status = AgendamentoStatus.Confirmado }
                }
            ]);

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

        Assert.Empty(response.Slots);
        Assert.Empty(response.DatasAtendimento);
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
    public async Task ConsultarPorEstabelecimentoAsync_NaoDeveUsarHorarioDaLoja_QuandoProfissionalNaoAtendeNoDia()
    {
        var segunda = ObterProximaSegunda();
        var terca = segunda.AddDays(1);
        ConfigurarCenarioBasico(segunda);

        _horarioFuncionamentoRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoEDiaAsync(20, DayOfWeek.Tuesday, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new HorarioFuncionamentoEstabelecimento
                {
                    DiaSemana = DayOfWeek.Tuesday,
                    HoraInicio = new TimeOnly(8, 0),
                    HoraFim = new TimeOnly(18, 0),
                    Ativo = true
                }
            ]);

        _horarioAtendimentoRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(20, 40, DayOfWeek.Tuesday, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = CreateService();
        var response = await service.ConsultarPorEstabelecimentoAsync(
            20,
            new ConsultarDisponibilidadeAgendaDto
            {
                DataInicio = segunda,
                DataFim = terca,
                ServicoId = 5,
                ProfissionalId = 40
            });

        Assert.Contains(segunda, response.DatasAtendimento);
        Assert.DoesNotContain(terca, response.DatasAtendimento);
        Assert.All(response.Slots, slot => Assert.Equal(DayOfWeek.Monday, DateOnly.FromDateTime(slot.Inicio).DayOfWeek));
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

    [Fact]
    public async Task ConsultarPublicoPorEstabelecimentoAsync_SemPreferencia_DeveUsarProfissionaisDaLoja_QuandoServicoNaoTemVinculos()
    {
        var segunda = ObterProximaSegunda();
        ConfigurarCenarioBasico(segunda, incluirVinculoServico: false);

        _profissionalServicoRepository
            .Setup(r => r.ListarProfissionaisAtivosPorServicoAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ListarAtivosComAgendamentoPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ProfissionalEstabelecimento
                {
                    ProfissionalId = 40,
                    EstabelecimentoId = 20,
                    Ativo = true,
                    PodeReceberAgendamento = true
                }
            ]);

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ProfissionalEstabelecimento
                {
                    ProfissionalId = 40,
                    EstabelecimentoId = 20,
                    Ativo = true,
                    PodeReceberAgendamento = true
                }
            ]);

        var service = CreateService();
        var response = await service.ConsultarPublicoPorEstabelecimentoAsync(
            Guid.NewGuid(),
            new ConsultarDisponibilidadeAgendaDto
            {
                DataInicio = segunda,
                DataFim = segunda,
                ServicoId = 5
            });

        Assert.NotEmpty(response.Slots);
        Assert.Contains(segunda, response.DatasAtendimento);
    }

    [Fact]
    public async Task ConsultarPublicoPorEstabelecimentoAsync_SemPreferencia_DeveUsarProfissionalVitrine_QuandoNaoHaEquipeComAgendamento()
    {
        var segunda = ObterProximaSegunda();
        ConfigurarCenarioBasico(segunda, incluirVinculoServico: false);

        _servicoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(5, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Servico
            {
                Id = 5,
                EstabelecimentoId = 20,
                DuracaoMinutos = 60,
                Ativo = true
            });

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ListarAtivosComAgendamentoPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ProfissionalEstabelecimento
                {
                    ProfissionalId = 40,
                    EstabelecimentoId = 20,
                    Ativo = true,
                    SomenteExibicao = true,
                    PodeReceberAgendamento = false
                }
            ]);

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterPorProfissionalAsync(40, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalEstabelecimento
            {
                ProfissionalId = 40,
                EstabelecimentoId = 20,
                Ativo = true,
                SomenteExibicao = true,
                PodeReceberAgendamento = false
            });

        var service = CreateService();
        var response = await service.ConsultarPublicoPorEstabelecimentoAsync(
            Guid.NewGuid(),
            new ConsultarDisponibilidadeAgendaDto
            {
                DataInicio = segunda,
                DataFim = segunda,
                ServicoId = 5
            });

        Assert.NotEmpty(response.Slots);
        Assert.Contains(segunda, response.DatasAtendimento);
    }

    [Fact]
    public async Task ConsultarPublicoPorEstabelecimentoAsync_SemPreferencia_DeveUsarHorarioDaLoja_QuandoNaoHaProfissionais()
    {
        var segunda = ObterProximaSegunda();
        ConfigurarCenarioBasico(segunda, incluirVinculoServico: false);

        _servicoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(5, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Servico
            {
                Id = 5,
                EstabelecimentoId = 20,
                DuracaoMinutos = 60,
                Ativo = true
            });

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ListarAtivosComAgendamentoPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = CreateService();
        var response = await service.ConsultarPublicoPorEstabelecimentoAsync(
            Guid.NewGuid(),
            new ConsultarDisponibilidadeAgendaDto
            {
                DataInicio = segunda,
                DataFim = segunda,
                ServicoId = 5
            });

        Assert.NotEmpty(response.Slots);
        Assert.All(response.Slots, slot =>
            Assert.Equal(AgendaSemPreferenciaHelper.ProfissionalIdEstabelecimento, slot.ProfissionalId));
        Assert.Contains(segunda, response.DatasAtendimento);
    }

    [Fact]
    public async Task ConsultarPublicoPorEstabelecimentoAsync_SemPreferencia_DeveHerdarHorarioDaLoja_QuandoProfissionalNaoTemAgenda()
    {
        var segunda = ObterProximaSegunda();
        ConfigurarCenarioBasico(segunda, incluirAgendaProfissional: false);

        _horarioAtendimentoRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(20, 40, DayOfWeek.Monday, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _horarioAtendimentoRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(20, 40, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var service = CreateService();
        var response = await service.ConsultarPublicoPorEstabelecimentoAsync(
            Guid.NewGuid(),
            new ConsultarDisponibilidadeAgendaDto
            {
                DataInicio = segunda,
                DataFim = segunda,
                ServicoId = 5,
                ProfissionalId = 40
            });

        Assert.NotEmpty(response.Slots);
        Assert.Contains(segunda, response.DatasAtendimento);
    }

    private void ConfigurarCenarioBasico(
        DateOnly segunda,
        bool incluirVinculoServico = true,
        bool incluirAgendaProfissional = true)
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(20, PermissaoNegocio.AgendaCriar, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GLOWAPI.Application.Models.Autorizacao.AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Owner,
                false,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.AgendaCriar }));

        _estabelecimentoRepository
            .Setup(r => r.ObterPorPublicGuidAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Estabelecimento
            {
                Id = 20,
                PublicGuid = Guid.NewGuid(),
                Ativo = true
            });

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
            .ReturnsAsync(incluirVinculoServico);

        if (incluirVinculoServico)
        {
            _profissionalServicoRepository
                .Setup(r => r.ListarProfissionaisAtivosPorServicoAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync([40]);
        }

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterPorProfissionalAsync(40, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalEstabelecimento
            {
                ProfissionalId = 40,
                EstabelecimentoId = 20,
                Ativo = true,
                PodeReceberAgendamento = true
            });

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ProfissionalEstabelecimento
                {
                    ProfissionalId = 40,
                    EstabelecimentoId = 20,
                    Ativo = true,
                    PodeReceberAgendamento = true
                }
            ]);

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

        if (incluirAgendaProfissional)
        {
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
        }
        else
        {
            _horarioAtendimentoRepository
                .Setup(r => r.ListarPorEstabelecimentoAsync(20, 40, DayOfWeek.Monday, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
        }

        _horarioAtendimentoRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(20, 40, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incluirAgendaProfissional
                ? [
                    new HorarioAtendimentoProfissional
                    {
                        ProfissionalId = 40,
                        EstabelecimentoId = 20,
                        DiaSemana = DayOfWeek.Monday,
                        HoraInicio = new TimeOnly(9, 0),
                        HoraFim = new TimeOnly(17, 0),
                        Ativo = true
                    }
                ]
                : []);

        _agendamentoItemRepository
            .Setup(r => r.ListarOcupacaoAsync(20, It.IsAny<int?>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
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
