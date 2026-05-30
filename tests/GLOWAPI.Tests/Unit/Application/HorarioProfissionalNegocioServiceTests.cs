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
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();

    public HorarioProfissionalNegocioServiceTests()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.ProfissionalGerenciar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Owner,
                false,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.ProfissionalGerenciar }));

        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterPorProfissionalAsync(40, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalEstabelecimento
            {
                EstabelecimentoId = 20,
                ProfissionalId = 40,
                Ativo = true
            });

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
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.ProfissionalGerenciar,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsuarioSemPermissaoNegocioException());

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
            _autorizacaoNegocioService.Object);
}
