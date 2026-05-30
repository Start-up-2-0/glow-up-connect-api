using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Autorizacao;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class HorarioProfissionalNegocioServiceTests
{
    private readonly Mock<IHorarioAtendimentoProfissionalRepository> _horarioAtendimentoRepository = new();
    private readonly Mock<IHorarioFuncionamentoEstabelecimentoRepository> _horarioFuncionamentoRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<IAgendamentoItemRepository> _agendamentoItemRepository = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();
    private readonly Mock<IProfissionalEscopoAcessoService> _profissionalEscopoAcessoService = new();
    private readonly Mock<IAuditoriaNegocioService> _auditoriaNegocioService = new();

    public HorarioProfissionalNegocioServiceTests()
    {
        _autorizacaoNegocioService
            .Setup(s => s.PossuiPermissaoAsync(
                20,
                PermissaoNegocio.HorarioGerenciar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.HorarioVisualizar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Manager,
                false,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.HorarioVisualizar }));

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterPorProfissionalAsync(40, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalEstabelecimento
            {
                EstabelecimentoId = 20,
                ProfissionalId = 40,
                Ativo = true,
                Profissional = new Profissional
                {
                    Id = 40,
                    Ativo = true
                }
            });
        _profissionalEscopoAcessoService
            .Setup(s => s.ObterEscopoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EscopoProfissionalResultado(
                20,
                10,
                40,
                50,
                true));
        _agendamentoItemRepository
            .Setup(r => r.ListarAgendamentosFuturosImpactadosPorAlteracaoHorarioAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DayOfWeek>(),
                It.IsAny<TimeOnly>(),
                It.IsAny<TimeOnly>(),
                It.IsAny<DayOfWeek>(),
                It.IsAny<TimeOnly>(),
                It.IsAny<TimeOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _agendamentoItemRepository
            .Setup(r => r.ListarAgendamentosFuturosNoHorarioAtivoAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DayOfWeek>(),
                It.IsAny<TimeOnly>(),
                It.IsAny<TimeOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _horarioFuncionamentoRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoEDiaAsync(
                20,
                DayOfWeek.Monday,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new HorarioFuncionamentoEstabelecimento
                {
                    EstabelecimentoId = 20,
                    DiaSemana = DayOfWeek.Monday,
                    HoraInicio = new TimeOnly(8, 0),
                    HoraFim = new TimeOnly(18, 0),
                    Ativo = true
                }
            ]);
    }

    [Fact]
    public async Task ListarAsync_DeveListarHorariosComFiltros()
    {
        _horarioAtendimentoRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(
                20,
                40,
                DayOfWeek.Monday,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CriarHorarioExistente(ativo: true)
            ]);

        var service = CreateService();

        var response = await service.ListarAsync(
            20,
            new HorarioProfissionalFiltroDto
            {
                ProfissionalId = 40,
                DiaSemana = DayOfWeek.Monday,
                Ativo = true
            });

        Assert.Single(response);
        Assert.Equal(40, response[0].ProfissionalId);
        Assert.Equal("Monday", response[0].DiaSemana);
    }

    [Fact]
    public async Task ListarAsync_DeveRestringirProfissionalAoProprioHorario()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.HorarioVisualizar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Profissional,
                true,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.HorarioVisualizar }));
        _horarioAtendimentoRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(
                20,
                40,
                null,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CriarHorarioExistente(ativo: true)
            ]);

        var service = CreateService();

        var response = await service.ListarAsync(
            20,
            new HorarioProfissionalFiltroDto
            {
                ProfissionalId = 99,
                Ativo = true
            });

        Assert.Single(response);
        Assert.Equal(40, response[0].ProfissionalId);
        _horarioAtendimentoRepository.Verify(r => r.ListarPorEstabelecimentoAsync(
            20,
            40,
            null,
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarAsync_DeveLancarExcecao_QuandoProfissionalNaoTemVinculoAtivo()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.HorarioVisualizar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Profissional,
                true,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.HorarioVisualizar }));
        _profissionalEscopoAcessoService
            .Setup(s => s.ObterEscopoAsync(20, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProfissionalSemVinculoNegocioException());

        var service = CreateService();

        await Assert.ThrowsAsync<ProfissionalSemVinculoNegocioException>(() =>
            service.ListarAsync(
                20,
                new HorarioProfissionalFiltroDto { ProfissionalId = 40 }));
    }

    [Fact]
    public async Task CriarAsync_DeveCriarHorarioDentroDoFuncionamento()
    {
        HorarioAtendimentoProfissional? capturado = null;
        _horarioAtendimentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<HorarioAtendimentoProfissional>(), It.IsAny<CancellationToken>()))
            .Callback<HorarioAtendimentoProfissional, CancellationToken>((horario, _) => capturado = horario);

        var service = CreateService();

        var response = await service.CriarAsync(
            20,
            40,
            new CriarHorarioProfissionalRequestDto
            {
                DiaSemana = DayOfWeek.Monday,
                HoraInicio = new TimeOnly(9, 0),
                HoraFim = new TimeOnly(12, 0)
            });

        Assert.NotNull(capturado);
        Assert.Equal(20, capturado!.EstabelecimentoId);
        Assert.Equal(40, capturado.ProfissionalId);
        Assert.Equal(DayOfWeek.Monday, capturado.DiaSemana);
        Assert.True(capturado.Ativo);
        Assert.Equal(new TimeOnly(9, 0), response.HoraInicio);
        Assert.Equal(new TimeOnly(12, 0), response.HoraFim);
        _horarioAtendimentoRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_DeveLancarExcecao_QuandoProfissionalNaoTemVinculoAtivo()
    {
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterPorProfissionalAsync(40, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfissionalEstabelecimento?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<ProfissionalSemVinculoNegocioException>(() =>
            service.CriarAsync(20, 40, CriarRequestValido()));
    }

    [Fact]
    public async Task CriarAsync_DeveLancarExcecao_QuandoPerfilProfissionalEstaInativo()
    {
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterPorProfissionalAsync(40, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalEstabelecimento
            {
                EstabelecimentoId = 20,
                ProfissionalId = 40,
                Ativo = true,
                Profissional = new Profissional
                {
                    Id = 40,
                    Ativo = false
                }
            });

        var service = CreateService();

        await Assert.ThrowsAsync<ProfissionalSemVinculoNegocioException>(() =>
            service.CriarAsync(20, 40, CriarRequestValido()));
    }

    [Fact]
    public async Task CriarAsync_DeveLancarExcecao_QuandoHorarioEstaForaDoFuncionamento()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAtendimentoInvalidoException>(() =>
            service.CriarAsync(
                20,
                40,
                new CriarHorarioProfissionalRequestDto
                {
                    DiaSemana = DayOfWeek.Monday,
                    HoraInicio = new TimeOnly(7, 0),
                    HoraFim = new TimeOnly(9, 0)
                }));
    }

    [Fact]
    public async Task CriarAsync_DeveLancarExcecao_QuandoHorarioTemConflito()
    {
        _horarioAtendimentoRepository
            .Setup(r => r.ExisteConflitoAtivoAsync(
                40,
                20,
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(12, 0),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAtendimentoConflitanteException>(() =>
            service.CriarAsync(20, 40, CriarRequestValido()));
    }

    [Fact]
    public async Task CriarAsync_DeveLancarExcecao_QuandoHoraInicioForMaiorOuIgualHoraFim()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAtendimentoInvalidoException>(() =>
            service.CriarAsync(
                20,
                40,
                new CriarHorarioProfissionalRequestDto
                {
                    DiaSemana = DayOfWeek.Monday,
                    HoraInicio = new TimeOnly(12, 0),
                    HoraFim = new TimeOnly(12, 0)
                }));
    }

    [Fact]
    public async Task AtualizarAsync_DeveEditarHorarioValido()
    {
        var horario = CriarHorarioExistente(ativo: true);
        _horarioAtendimentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(horario);

        var service = CreateService();

        var response = await service.AtualizarAsync(
            20,
            70,
            CriarAtualizacaoValida());

        Assert.Equal(DayOfWeek.Monday, horario.DiaSemana);
        Assert.Equal(new TimeOnly(10, 0), horario.HoraInicio);
        Assert.Equal(new TimeOnly(13, 0), horario.HoraFim);
        Assert.NotNull(horario.UpdatedAt);
        Assert.Equal(new TimeOnly(10, 0), response.HoraInicio);
        _horarioAtendimentoRepository.Verify(r => r.Atualizar(horario), Times.Once);
        _horarioAtendimentoRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_DeveLancarExcecao_QuandoHorarioNaoExiste()
    {
        _horarioAtendimentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HorarioAtendimentoProfissional?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAtendimentoNaoEncontradoException>(() =>
            service.AtualizarAsync(20, 70, CriarAtualizacaoValida()));
    }

    [Fact]
    public async Task AtualizarAsync_DeveLancarExcecao_QuandoNovoHorarioEstaForaDoFuncionamento()
    {
        var horario = CriarHorarioExistente(ativo: true);
        _horarioAtendimentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(horario);

        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAtendimentoInvalidoException>(() =>
            service.AtualizarAsync(
                20,
                70,
                new AtualizarHorarioProfissionalRequestDto
                {
                    DiaSemana = DayOfWeek.Monday,
                    HoraInicio = new TimeOnly(7, 0),
                    HoraFim = new TimeOnly(9, 0)
                }));
    }

    [Fact]
    public async Task AtualizarAsync_DeveLancarExcecao_QuandoNovoHorarioTemConflito()
    {
        var horario = CriarHorarioExistente(ativo: true);
        _horarioAtendimentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(horario);
        _horarioAtendimentoRepository
            .Setup(r => r.ExisteConflitoAtivoAsync(
                40,
                20,
                DayOfWeek.Monday,
                new TimeOnly(10, 0),
                new TimeOnly(13, 0),
                70,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAtendimentoConflitanteException>(() =>
            service.AtualizarAsync(20, 70, CriarAtualizacaoValida()));
    }

    [Fact]
    public async Task AtualizarAsync_DeveLancarExcecao_QuandoImpactaAgendamentoFuturo()
    {
        var horario = CriarHorarioExistente(ativo: true);
        _horarioAtendimentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(horario);
        _agendamentoItemRepository
            .Setup(r => r.ListarAgendamentosFuturosImpactadosPorAlteracaoHorarioAsync(
                20,
                40,
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(12, 0),
                DayOfWeek.Monday,
                new TimeOnly(10, 0),
                new TimeOnly(13, 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AgendamentoFuturoImpactadoDto
                {
                    AgendamentoItemId = 1,
                    AgendamentoId = 2,
                    ProfissionalId = 40,
                    Inicio = DateTime.UtcNow.AddDays(2),
                    Fim = DateTime.UtcNow.AddDays(2).AddHours(1)
                }
            ]);

        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAlteracaoImpactaAgendamentosFuturosException>(() =>
            service.AtualizarAsync(20, 70, CriarAtualizacaoValida()));
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveInativarHorario()
    {
        var horario = CriarHorarioExistente(ativo: true);
        _horarioAtendimentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(horario);

        var service = CreateService();

        var response = await service.AtualizarStatusAsync(
            20,
            70,
            new AtualizarStatusHorarioProfissionalRequestDto { Ativo = false });

        Assert.False(horario.Ativo);
        Assert.False(response.Ativo);
        Assert.NotNull(horario.UpdatedAt);
        _horarioAtendimentoRepository.Verify(r => r.Atualizar(horario), Times.Once);
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveBloquearInativacaoQuandoImpactaAgendamentoFuturo()
    {
        var horario = CriarHorarioExistente(ativo: true);
        _horarioAtendimentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(horario);
        _agendamentoItemRepository
            .Setup(r => r.ListarAgendamentosFuturosNoHorarioAtivoAsync(
                20,
                40,
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(12, 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AgendamentoFuturoImpactadoDto
                {
                    AgendamentoItemId = 1,
                    AgendamentoId = 2,
                    ProfissionalId = 40,
                    Inicio = DateTime.UtcNow.AddDays(2),
                    Fim = DateTime.UtcNow.AddDays(2).AddHours(1)
                }
            ]);

        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAlteracaoImpactaAgendamentosFuturosException>(() =>
            service.AtualizarStatusAsync(
                20,
                70,
                new AtualizarStatusHorarioProfissionalRequestDto { Ativo = false }));

        Assert.True(horario.Ativo);
        _horarioAtendimentoRepository.Verify(r => r.Atualizar(It.IsAny<HorarioAtendimentoProfissional>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveValidarConflitoAoReativar()
    {
        var horario = CriarHorarioExistente(ativo: false);
        _horarioAtendimentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(horario);
        _horarioAtendimentoRepository
            .Setup(r => r.ExisteConflitoAtivoAsync(
                40,
                20,
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(12, 0),
                70,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAtendimentoConflitanteException>(() =>
            service.AtualizarStatusAsync(
                20,
                70,
                new AtualizarStatusHorarioProfissionalRequestDto { Ativo = true }));
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveLancarExcecao_QuandoHorarioNaoExiste()
    {
        _horarioAtendimentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HorarioAtendimentoProfissional?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAtendimentoNaoEncontradoException>(() =>
            service.AtualizarStatusAsync(
                20,
                70,
                new AtualizarStatusHorarioProfissionalRequestDto { Ativo = false }));
    }

    [Fact]
    public async Task CriarAsync_DeveLancarExcecao_QuandoUsuarioNaoTemPermissao()
    {
        _autorizacaoNegocioService
            .Setup(s => s.PossuiPermissaoAsync(
                20,
                PermissaoNegocio.HorarioGerenciar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _autorizacaoNegocioService
            .Setup(s => s.PossuiPermissaoAsync(
                20,
                PermissaoNegocio.HorarioGerenciarProprio,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoNegocioException>(() =>
            service.CriarAsync(20, 40, CriarRequestValido()));

        _profissionalEstabelecimentoRepository.Verify(
            r => r.ObterPorProfissionalAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static CriarHorarioProfissionalRequestDto CriarRequestValido() =>
        new()
        {
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(9, 0),
            HoraFim = new TimeOnly(12, 0)
        };

    private static AtualizarHorarioProfissionalRequestDto CriarAtualizacaoValida() =>
        new()
        {
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(10, 0),
            HoraFim = new TimeOnly(13, 0)
        };

    private static HorarioAtendimentoProfissional CriarHorarioExistente(bool ativo) =>
        new()
        {
            Id = 70,
            EstabelecimentoId = 20,
            ProfissionalId = 40,
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(9, 0),
            HoraFim = new TimeOnly(12, 0),
            Ativo = ativo
        };

    private HorarioProfissionalNegocioService CreateService() =>
        new(
            _horarioAtendimentoRepository.Object,
            _horarioFuncionamentoRepository.Object,
            _profissionalEstabelecimentoRepository.Object,
            _agendamentoItemRepository.Object,
            _autorizacaoNegocioService.Object,
            _profissionalEscopoAcessoService.Object,
            _auditoriaNegocioService.Object);
}
