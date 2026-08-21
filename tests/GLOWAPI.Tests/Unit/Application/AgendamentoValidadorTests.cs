using GLOWAPI.Application.DTOs.Agendamento;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Validators;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class AgendamentoValidadorTests
{
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentoRepository = new();
    private readonly Mock<IProfissionalRepository> _profissionalRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<IServicoRepository> _servicoRepository = new();
    private readonly Mock<IProfissionalServicoRepository> _profissionalServicoRepository = new();
    private readonly Mock<IHorarioFuncionamentoEstabelecimentoRepository> _horarioFuncionamentoRepository = new();
    private readonly Mock<IHorarioAtendimentoProfissionalRepository> _horarioAtendimentoProfissionalRepository = new();
    private readonly Mock<IAgendamentoItemRepository> _agendamentoItemRepository = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepository = new();

    [Fact]
    public async Task PrepararAsync_DeveIgnorarProprioAgendamentoNaRemarcacao()
    {
        ConfigurarCenarioBasico(ProfessionalType.VinculadoEstabelecimento);
        ConfigurarHorariosEstabelecimentoEVinculado();

        var data = ObterProximaSegunda();
        var inicio = data.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);
        var fim = inicio.AddMinutes(60);

        _agendamentoItemRepository
            .Setup(r => r.ListarOcupacaoAsync(1, 2, inicio, fim, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AgendamentoItem
                {
                    AgendamentoId = 42,
                    ProfissionalId = 2,
                    Inicio = inicio,
                    Fim = fim
                }
            ]);

        var validador = CreateValidador();
        var resultado = await validador.PrepararAsync(
            1,
            2,
            [5],
            data,
            new TimeOnly(10, 0),
            OrigemAgendamento.PublicoLoja,
            CriarDadosVisitante(),
            usuarioClienteId: null,
            agendamentoIgnorarId: 42);

        Assert.Equal(inicio, resultado.Inicio);
        Assert.Equal(fim, resultado.Fim);
    }

    [Fact]
    public async Task PrepararAsync_DeveRejeitarRemarcacao_QuandoConflitaComOutroAgendamento()
    {
        ConfigurarCenarioBasico(ProfessionalType.VinculadoEstabelecimento);
        ConfigurarHorariosEstabelecimentoEVinculado();

        var data = ObterProximaSegunda();
        var inicio = data.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);
        var fim = inicio.AddMinutes(60);

        _agendamentoItemRepository
            .Setup(r => r.ListarOcupacaoAsync(1, 2, inicio, fim, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AgendamentoItem
                {
                    AgendamentoId = 99,
                    ProfissionalId = 2,
                    Inicio = inicio,
                    Fim = fim
                }
            ]);

        var validador = CreateValidador();

        await Assert.ThrowsAsync<HorarioIndisponivelException>(() =>
            validador.PrepararAsync(
                1,
                2,
                [5],
                data,
                new TimeOnly(10, 0),
                OrigemAgendamento.PublicoLoja,
                CriarDadosVisitante(),
                usuarioClienteId: null,
                agendamentoIgnorarId: 42));
    }

    [Fact]
    public async Task PrepararAsync_DeveAceitarHorarioAutonomoSemFuncionamentoEstabelecimento()
    {
        ConfigurarCenarioBasico(ProfessionalType.Autonomo);
        ConfigurarHorariosAutonomo();

        var validador = CreateValidador();
        var data = ObterProximaSegunda();

        var resultado = await validador.PrepararAsync(
            1,
            2,
            [5],
            data,
            new TimeOnly(10, 0),
            OrigemAgendamento.PublicoProfissional,
            CriarDadosVisitante(),
            usuarioClienteId: null);

        Assert.Equal(new TimeOnly(10, 0), TimeOnly.FromDateTime(resultado.Inicio));
        _horarioFuncionamentoRepository.Verify(
            r => r.ListarAtivosPorEstabelecimentoEDiaAsync(
                It.IsAny<int>(),
                It.IsAny<DayOfWeek>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PrepararAsync_DeveRejeitarVinculadoSemFuncionamentoEstabelecimento()
    {
        ConfigurarCenarioBasico(ProfessionalType.VinculadoEstabelecimento);

        _horarioFuncionamentoRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoEDiaAsync(
                1,
                DayOfWeek.Monday,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _horarioAtendimentoProfissionalRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(
                1,
                2,
                DayOfWeek.Monday,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new HorarioAtendimentoProfissional
                {
                    HoraInicio = new TimeOnly(9, 0),
                    HoraFim = new TimeOnly(17, 0)
                }
            ]);

        var validador = CreateValidador();

        await Assert.ThrowsAsync<HorarioIndisponivelException>(() =>
            validador.PrepararAsync(
                1,
                2,
                [5],
                ObterProximaSegunda(),
                new TimeOnly(10, 0),
                OrigemAgendamento.PublicoLoja,
                CriarDadosVisitante(),
                usuarioClienteId: null));
    }

    [Fact]
    public async Task PrepararAsync_DeveRejeitarComboComIndividuais()
    {
        ConfigurarCenarioBasico(ProfessionalType.VinculadoEstabelecimento);

        var combo = new Servico
        {
            Id = 10,
            EstabelecimentoId = 1,
            Nome = "Combo Completo",
            DuracaoMinutos = 90,
            PrecoBase = 75,
            TipoServico = TipoServico.Combo,
            Ativo = true,
        };

        _servicoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(10, 1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(combo);

        _profissionalServicoRepository
            .Setup(r => r.ObterPorProfissionalEServicoAsync(2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalServico
            {
                ProfissionalId = 2,
                ServicoId = 10,
                DuracaoMinutos = 90,
                Preco = 75,
                Ativo = true,
            });

        var validador = CreateValidador();

        await Assert.ThrowsAsync<AgendamentoServicosInvalidosException>(() =>
            validador.PrepararAsync(
                1,
                2,
                [10, 5],
                ObterProximaSegunda(),
                new TimeOnly(10, 0),
                OrigemAgendamento.PublicoLoja,
                CriarDadosVisitante(),
                usuarioClienteId: null));
    }

    private void ConfigurarCenarioBasico(ProfessionalType tipoProfissional)
    {
        _estabelecimentoRepository
            .Setup(r => r.ObterPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Estabelecimento { Id = 1, Ativo = true });

        _profissionalRepository
            .Setup(r => r.ObterPorIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Profissional
            {
                Id = 2,
                Ativo = true,
                TipoProfissional = tipoProfissional
            });

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterPorProfissionalAsync(2, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalEstabelecimento
            {
                Ativo = true,
                PodeReceberAgendamento = true
            });

        var servico = new Servico
        {
            Id = 5,
            EstabelecimentoId = 1,
            Nome = "Corte",
            DuracaoMinutos = 60,
            PrecoBase = 50,
            Ativo = true
        };

        _servicoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(5, 1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(servico);

        _profissionalServicoRepository
            .Setup(r => r.ObterPorProfissionalEServicoAsync(2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalServico
            {
                ProfissionalId = 2,
                ServicoId = 5,
                DuracaoMinutos = 60,
                Preco = 55,
                Ativo = true
            });

        _agendamentoItemRepository
            .Setup(r => r.ListarOcupacaoAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private void ConfigurarHorariosEstabelecimentoEVinculado()
    {
        _horarioFuncionamentoRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoEDiaAsync(
                1,
                DayOfWeek.Monday,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new HorarioFuncionamentoEstabelecimento
                {
                    HoraInicio = new TimeOnly(8, 0),
                    HoraFim = new TimeOnly(18, 0)
                }
            ]);

        ConfigurarHorarioProfissional();
    }

    private void ConfigurarHorariosAutonomo()
    {
        ConfigurarHorarioProfissional();
    }

    private void ConfigurarHorarioProfissional()
    {
        _horarioAtendimentoProfissionalRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(
                1,
                2,
                DayOfWeek.Monday,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new HorarioAtendimentoProfissional
                {
                    HoraInicio = new TimeOnly(9, 0),
                    HoraFim = new TimeOnly(17, 0)
                }
            ]);
    }

    private static CriarAgendamentoRequestDto CriarDadosVisitante() =>
        new()
        {
            ClienteNome = "Visitante",
            ClienteEmail = "visitante@email.com",
            ClienteTelefone = "11988887777"
        };

    private static DateOnly ObterProximaSegunda()
    {
        var data = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        while (data.DayOfWeek != DayOfWeek.Monday)
        {
            data = data.AddDays(1);
        }

        return data;
    }

    private AgendamentoValidador CreateValidador() =>
        new(
            _estabelecimentoRepository.Object,
            _profissionalRepository.Object,
            _profissionalEstabelecimentoRepository.Object,
            _servicoRepository.Object,
            _profissionalServicoRepository.Object,
            _horarioFuncionamentoRepository.Object,
            _horarioAtendimentoProfissionalRepository.Object,
            _agendamentoItemRepository.Object,
            _usuarioRepository.Object);
}
