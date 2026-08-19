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

public class HorarioFuncionamentoNegocioServiceTests
{
    private readonly Mock<IHorarioFuncionamentoEstabelecimentoRepository> _horarioFuncionamentoRepository = new();
    private readonly Mock<IHorarioAtendimentoProfissionalRepository> _horarioAtendimentoProfissionalRepository = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();
    private readonly Mock<IAuditoriaNegocioService> _auditoriaNegocioService = new();
    private readonly Mock<IOnboardingPublicacaoService> _onboardingPublicacaoService = new();

    public HorarioFuncionamentoNegocioServiceTests()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.HorarioGerenciar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Owner,
                false,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.HorarioGerenciar }));
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.HorarioVisualizar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Owner,
                false,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.HorarioVisualizar }));
        _horarioFuncionamentoRepository
            .Setup(r => r.ExisteConflitoAtivoAsync(
                It.IsAny<int>(),
                It.IsAny<DayOfWeek>(),
                It.IsAny<TimeOnly>(),
                It.IsAny<TimeOnly>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _horarioAtendimentoProfissionalRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoEDiaAsync(
                It.IsAny<int>(),
                It.IsAny<DayOfWeek>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _horarioFuncionamentoRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoEDiaAsync(
                It.IsAny<int>(),
                It.IsAny<DayOfWeek>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task ListarAsync_DeveConsultarHorariosComFiltros()
    {
        _horarioFuncionamentoRepository
            .Setup(r => r.ListarPorEstabelecimentoAsync(
                20,
                DayOfWeek.Monday,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new HorarioFuncionamentoEstabelecimento
                {
                    Id = 70,
                    EstabelecimentoId = 20,
                    DiaSemana = DayOfWeek.Monday,
                    HoraInicio = new TimeOnly(9, 0),
                    HoraFim = new TimeOnly(18, 0),
                    Ativo = true
                }
            ]);

        var service = CreateService();

        var response = await service.ListarAsync(
            20,
            new HorarioFuncionamentoFiltroDto
            {
                DiaSemana = DayOfWeek.Monday,
                Ativo = true
            });

        Assert.Single(response);
        Assert.Equal(70, response[0].Id);
        Assert.Equal("Monday", response[0].DiaSemana);
    }

    [Fact]
    public async Task ListarAsync_DeveLancarExcecao_QuandoUsuarioNaoTemPermissao()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.HorarioVisualizar,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsuarioSemPermissaoNegocioException());

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoNegocioException>(() =>
            service.ListarAsync(20, new HorarioFuncionamentoFiltroDto()));

        _horarioFuncionamentoRepository.Verify(
            r => r.ListarPorEstabelecimentoAsync(
                It.IsAny<int>(),
                It.IsAny<DayOfWeek?>(),
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CriarAsync_DeveCriarHorarioAtivoPorPadrao()
    {
        HorarioFuncionamentoEstabelecimento? capturado = null;
        _horarioFuncionamentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<HorarioFuncionamentoEstabelecimento>(), It.IsAny<CancellationToken>()))
            .Callback<HorarioFuncionamentoEstabelecimento, CancellationToken>((horario, _) =>
            {
                horario.Id = 70;
                capturado = horario;
            })
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.CriarAsync(20, CriarRequestValido());

        Assert.NotNull(capturado);
        Assert.Equal(20, capturado!.EstabelecimentoId);
        Assert.Equal(DayOfWeek.Monday, capturado.DiaSemana);
        Assert.Equal(new TimeOnly(9, 0), capturado.HoraInicio);
        Assert.Equal(new TimeOnly(18, 0), capturado.HoraFim);
        Assert.True(capturado.Ativo);
        Assert.Equal(70, response.Id);
        Assert.True(response.Ativo);
        _horarioFuncionamentoRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_DeveLancarExcecao_QuandoHoraInicioForMaiorOuIgualHoraFim()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAtendimentoInvalidoException>(() =>
            service.CriarAsync(
                20,
                new CriarHorarioFuncionamentoRequestDto
                {
                    DiaSemana = DayOfWeek.Monday,
                    HoraInicio = new TimeOnly(18, 0),
                    HoraFim = new TimeOnly(18, 0)
                }));

        _horarioFuncionamentoRepository.Verify(
            r => r.AdicionarAsync(It.IsAny<HorarioFuncionamentoEstabelecimento>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CriarAsync_DeveLancarExcecao_QuandoUsuarioNaoTemPermissao()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.HorarioGerenciar,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsuarioSemPermissaoNegocioException());

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoNegocioException>(() =>
            service.CriarAsync(20, CriarRequestValido()));

        _horarioFuncionamentoRepository.Verify(
            r => r.AdicionarAsync(It.IsAny<HorarioFuncionamentoEstabelecimento>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_DeveEditarHorarioValido()
    {
        var horario = CriarHorarioExistente();
        _horarioFuncionamentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(horario);

        var service = CreateService();

        var response = await service.AtualizarAsync(
            20,
            70,
            new AtualizarHorarioFuncionamentoRequestDto
            {
                DiaSemana = DayOfWeek.Tuesday,
                HoraInicio = new TimeOnly(10, 0),
                HoraFim = new TimeOnly(17, 0)
            });

        Assert.Equal(DayOfWeek.Tuesday, horario.DiaSemana);
        Assert.Equal(new TimeOnly(10, 0), horario.HoraInicio);
        Assert.Equal(new TimeOnly(17, 0), horario.HoraFim);
        Assert.NotNull(horario.UpdatedAt);
        Assert.Equal("Tuesday", response.DiaSemana);
        _horarioFuncionamentoRepository.Verify(r => r.Atualizar(horario), Times.Once);
        _horarioFuncionamentoRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_DeveLancarExcecao_QuandoHorarioNaoExiste()
    {
        _horarioFuncionamentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HorarioFuncionamentoEstabelecimento?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<HorarioFuncionamentoNaoEncontradoException>(() =>
            service.AtualizarAsync(20, 70, CriarAtualizacaoValida()));
    }

    [Fact]
    public async Task AtualizarAsync_DeveLancarExcecao_QuandoHorarioAtualizadoForInvalido()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAtendimentoInvalidoException>(() =>
            service.AtualizarAsync(
                20,
                70,
                new AtualizarHorarioFuncionamentoRequestDto
                {
                    DiaSemana = DayOfWeek.Monday,
                    HoraInicio = new TimeOnly(18, 0),
                    HoraFim = new TimeOnly(9, 0)
                }));

        _horarioFuncionamentoRepository.Verify(
            r => r.ObterPorIdEEstabelecimentoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_DeveBloquearAlteracaoQueDeixaProfissionalForaDoFuncionamento()
    {
        var horario = CriarHorarioExistente();
        _horarioFuncionamentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(horario);
        _horarioFuncionamentoRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoEDiaAsync(20, DayOfWeek.Monday, It.IsAny<CancellationToken>()))
            .ReturnsAsync([horario]);
        _horarioAtendimentoProfissionalRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoEDiaAsync(20, DayOfWeek.Monday, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new HorarioAtendimentoProfissional
                {
                    Id = 90,
                    EstabelecimentoId = 20,
                    ProfissionalId = 40,
                    DiaSemana = DayOfWeek.Monday,
                    HoraInicio = new TimeOnly(10, 0),
                    HoraFim = new TimeOnly(17, 0),
                    Ativo = true
                }
            ]);

        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAtendimentoInvalidoException>(() =>
            service.AtualizarAsync(
                20,
                70,
                new AtualizarHorarioFuncionamentoRequestDto
                {
                    DiaSemana = DayOfWeek.Monday,
                    HoraInicio = new TimeOnly(9, 0),
                    HoraFim = new TimeOnly(16, 0)
                }));

        _horarioFuncionamentoRepository.Verify(r => r.Atualizar(It.IsAny<HorarioFuncionamentoEstabelecimento>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveInativarHorarioSemImpactoEmProfissionais()
    {
        var horario = CriarHorarioExistente();
        _horarioFuncionamentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(horario);

        var service = CreateService();

        var response = await service.AtualizarStatusAsync(
            20,
            70,
            new AtualizarStatusHorarioFuncionamentoRequestDto { Ativo = false });

        Assert.False(horario.Ativo);
        Assert.False(response.Ativo);
        Assert.NotNull(horario.UpdatedAt);
        _horarioFuncionamentoRepository.Verify(r => r.Atualizar(horario), Times.Once);
        _horarioFuncionamentoRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveReativarHorario()
    {
        var horario = CriarHorarioExistente();
        horario.Ativo = false;
        _horarioFuncionamentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(horario);

        var service = CreateService();

        var response = await service.AtualizarStatusAsync(
            20,
            70,
            new AtualizarStatusHorarioFuncionamentoRequestDto { Ativo = true });

        Assert.True(horario.Ativo);
        Assert.True(response.Ativo);
        Assert.NotNull(horario.UpdatedAt);
        _horarioFuncionamentoRepository.Verify(r => r.Atualizar(horario), Times.Once);
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveBloquearInativacaoQueDeixaProfissionalForaDoFuncionamento()
    {
        var horario = CriarHorarioExistente();
        _horarioFuncionamentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(horario);
        _horarioFuncionamentoRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoEDiaAsync(20, DayOfWeek.Monday, It.IsAny<CancellationToken>()))
            .ReturnsAsync([horario]);
        _horarioAtendimentoProfissionalRepository
            .Setup(r => r.ListarAtivosPorEstabelecimentoEDiaAsync(20, DayOfWeek.Monday, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new HorarioAtendimentoProfissional
                {
                    Id = 90,
                    EstabelecimentoId = 20,
                    ProfissionalId = 40,
                    DiaSemana = DayOfWeek.Monday,
                    HoraInicio = new TimeOnly(10, 0),
                    HoraFim = new TimeOnly(17, 0),
                    Ativo = true
                }
            ]);

        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAtendimentoInvalidoException>(() =>
            service.AtualizarStatusAsync(
                20,
                70,
                new AtualizarStatusHorarioFuncionamentoRequestDto { Ativo = false }));

        Assert.True(horario.Ativo);
        _horarioFuncionamentoRepository.Verify(r => r.Atualizar(It.IsAny<HorarioFuncionamentoEstabelecimento>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveLancarExcecao_QuandoHorarioNaoExiste()
    {
        _horarioFuncionamentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoAsync(70, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HorarioFuncionamentoEstabelecimento?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<HorarioFuncionamentoNaoEncontradoException>(() =>
            service.AtualizarStatusAsync(
                20,
                70,
                new AtualizarStatusHorarioFuncionamentoRequestDto { Ativo = false }));
    }

    private static CriarHorarioFuncionamentoRequestDto CriarRequestValido() =>
        new()
        {
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(9, 0),
            HoraFim = new TimeOnly(18, 0)
        };

    private static AtualizarHorarioFuncionamentoRequestDto CriarAtualizacaoValida() =>
        new()
        {
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(9, 0),
            HoraFim = new TimeOnly(18, 0)
        };

    private static HorarioFuncionamentoEstabelecimento CriarHorarioExistente() =>
        new()
        {
            Id = 70,
            EstabelecimentoId = 20,
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(9, 0),
            HoraFim = new TimeOnly(18, 0),
            Ativo = true
        };

    [Fact]
    public async Task CriarAsync_DeveLancarExcecao_QuandoHorarioConflitaComOutroAtivo()
    {
        _horarioFuncionamentoRepository
            .Setup(r => r.ExisteConflitoAtivoAsync(
                20,
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(18, 0),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAtendimentoConflitanteException>(() =>
            service.CriarAsync(20, CriarRequestValido()));
    }

    private HorarioFuncionamentoNegocioService CreateService() =>
        new(
            _horarioFuncionamentoRepository.Object,
            _horarioAtendimentoProfissionalRepository.Object,
            _autorizacaoNegocioService.Object,
            _auditoriaNegocioService.Object,
            _onboardingPublicacaoService.Object);
}
