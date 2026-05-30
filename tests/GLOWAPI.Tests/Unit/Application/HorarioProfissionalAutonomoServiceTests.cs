using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Domain.Exceptions.Negocios;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class HorarioProfissionalAutonomoServiceTests
{
    private readonly Mock<IProfissionalRepository> _profissionalRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<IHorarioProfissionalNegocioService> _horarioProfissionalNegocioService = new();
    private readonly Mock<ICurrentUserContext> _currentUserContext = new();

    public HorarioProfissionalAutonomoServiceTests()
    {
        _currentUserContext.SetupGet(c => c.UserId).Returns(10);
        _profissionalRepository
            .Setup(r => r.ObterPorIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Profissional
            {
                Id = 30,
                UsuarioId = 10,
                TipoProfissional = ProfessionalType.Autonomo,
                Ativo = true
            });
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterAtivoPorProfissionalAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalEstabelecimento
            {
                ProfissionalId = 30,
                EstabelecimentoId = 20,
                Ativo = true
            });
        _horarioProfissionalNegocioService
            .Setup(s => s.CriarAsync(20, 30, It.IsAny<CriarHorarioProfissionalRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HorarioProfissionalResponseDto
            {
                Id = 70,
                EstabelecimentoId = 20,
                ProfissionalId = 30,
                DiaSemana = "Monday",
                HoraInicio = new TimeOnly(9, 0),
                HoraFim = new TimeOnly(12, 0),
                Ativo = true
            });
        _horarioProfissionalNegocioService
            .Setup(s => s.ListarAsync(
                20,
                It.Is<HorarioProfissionalFiltroDto>(filtro => filtro.ProfissionalId == 30),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new HorarioProfissionalResponseDto
                {
                    Id = 70,
                    EstabelecimentoId = 20,
                    ProfissionalId = 30,
                    DiaSemana = "Monday",
                    HoraInicio = new TimeOnly(9, 0),
                    HoraFim = new TimeOnly(12, 0),
                    Ativo = true
                }
            ]);
        _horarioProfissionalNegocioService
            .Setup(s => s.AtualizarAsync(20, 70, It.IsAny<AtualizarHorarioProfissionalRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HorarioProfissionalResponseDto
            {
                Id = 70,
                EstabelecimentoId = 20,
                ProfissionalId = 30,
                DiaSemana = "Monday",
                HoraInicio = new TimeOnly(10, 0),
                HoraFim = new TimeOnly(13, 0),
                Ativo = true
            });
        _horarioProfissionalNegocioService
            .Setup(s => s.AtualizarStatusAsync(20, 70, It.IsAny<AtualizarStatusHorarioProfissionalRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HorarioProfissionalResponseDto
            {
                Id = 70,
                EstabelecimentoId = 20,
                ProfissionalId = 30,
                DiaSemana = "Monday",
                HoraInicio = new TimeOnly(9, 0),
                HoraFim = new TimeOnly(12, 0),
                Ativo = false
            });
    }

    [Fact]
    public async Task ListarAsync_DeveListarHorariosDoTenantDoProfissionalAutonomo()
    {
        _horarioProfissionalNegocioService
            .Setup(s => s.ListarAsync(
                20,
                It.Is<HorarioProfissionalFiltroDto>(filtro =>
                    filtro.ProfissionalId == 30
                    && filtro.DiaSemana == DayOfWeek.Monday
                    && filtro.Ativo == true),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new HorarioProfissionalResponseDto
                {
                    Id = 70,
                    EstabelecimentoId = 20,
                    ProfissionalId = 30,
                    DiaSemana = "Monday",
                    HoraInicio = new TimeOnly(9, 0),
                    HoraFim = new TimeOnly(12, 0),
                    Ativo = true
                }
            ]);

        var service = CreateService();

        var response = await service.ListarAsync(
            30,
            new HorarioProfissionalAutonomoFiltroDto
            {
                DiaSemana = DayOfWeek.Monday,
                Ativo = true
            });

        Assert.Single(response);
        Assert.Equal(30, response[0].ProfissionalId);
        Assert.Equal(20, response[0].EstabelecimentoId);
    }

    [Fact]
    public async Task ListarAsync_DeveLancarExcecao_QuandoProfissionalPertenceAOutroUsuario()
    {
        _profissionalRepository
            .Setup(r => r.ObterPorIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Profissional
            {
                Id = 30,
                UsuarioId = 99,
                TipoProfissional = ProfessionalType.Autonomo,
                Ativo = true
            });

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoAssinaturaException>(() =>
            service.ListarAsync(30, new HorarioProfissionalAutonomoFiltroDto()));

        _horarioProfissionalNegocioService.Verify(s => s.ListarAsync(
            It.IsAny<int>(),
            It.IsAny<HorarioProfissionalFiltroDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListarAsync_DeveLancarExcecao_QuandoProfissionalNaoPossuiTenantVinculado()
    {
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterAtivoPorProfissionalAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfissionalEstabelecimento?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<ProfissionalAutonomoAssinaturaInvalidoException>(() =>
            service.ListarAsync(30, new HorarioProfissionalAutonomoFiltroDto()));
    }

    [Fact]
    public async Task CriarAsync_DeveCadastrarHorarioNoTenantDoProfissionalAutonomo()
    {
        var request = CriarRequestValido();
        var service = CreateService();

        var response = await service.CriarAsync(30, request);

        Assert.Equal(70, response.Id);
        Assert.Equal(20, response.EstabelecimentoId);
        Assert.Equal(30, response.ProfissionalId);
        _horarioProfissionalNegocioService.Verify(s => s.CriarAsync(
            20,
            30,
            request,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_DeveAtualizarHorarioDoProfissionalAutonomo()
    {
        var request = CriarAtualizacaoValida();
        var service = CreateService();

        var response = await service.AtualizarAsync(30, 70, request);

        Assert.Equal(70, response.Id);
        Assert.Equal(new TimeOnly(10, 0), response.HoraInicio);
        _horarioProfissionalNegocioService.Verify(s => s.AtualizarAsync(
            20,
            70,
            request,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_DeveBloquearHorarioQueNaoPertenceAoProfissionalAutonomo()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoAssinaturaException>(() =>
            service.AtualizarAsync(30, 99, CriarAtualizacaoValida()));

        _horarioProfissionalNegocioService.Verify(s => s.AtualizarAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<AtualizarHorarioProfissionalRequestDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_DevePropagarExcecao_QuandoNovoHorarioTemConflito()
    {
        _horarioProfissionalNegocioService
            .Setup(s => s.AtualizarAsync(20, 70, It.IsAny<AtualizarHorarioProfissionalRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HorarioAtendimentoConflitanteException());

        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAtendimentoConflitanteException>(() =>
            service.AtualizarAsync(30, 70, CriarAtualizacaoValida()));
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveInativarHorarioDoProfissionalAutonomo()
    {
        var request = new AtualizarStatusHorarioProfissionalRequestDto { Ativo = false };
        var service = CreateService();

        var response = await service.AtualizarStatusAsync(30, 70, request);

        Assert.False(response.Ativo);
        _horarioProfissionalNegocioService.Verify(s => s.AtualizarStatusAsync(
            20,
            70,
            request,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveReativarHorarioDoProfissionalAutonomo()
    {
        var request = new AtualizarStatusHorarioProfissionalRequestDto { Ativo = true };
        _horarioProfissionalNegocioService
            .Setup(s => s.AtualizarStatusAsync(20, 70, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HorarioProfissionalResponseDto
            {
                Id = 70,
                EstabelecimentoId = 20,
                ProfissionalId = 30,
                DiaSemana = "Monday",
                HoraInicio = new TimeOnly(9, 0),
                HoraFim = new TimeOnly(12, 0),
                Ativo = true
            });

        var service = CreateService();

        var response = await service.AtualizarStatusAsync(30, 70, request);

        Assert.True(response.Ativo);
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveBloquearHorarioQueNaoPertenceAoProfissionalAutonomo()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoAssinaturaException>(() =>
            service.AtualizarStatusAsync(
                30,
                99,
                new AtualizarStatusHorarioProfissionalRequestDto { Ativo = false }));

        _horarioProfissionalNegocioService.Verify(s => s.AtualizarStatusAsync(
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<AtualizarStatusHorarioProfissionalRequestDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarStatusAsync_DevePropagarExcecao_QuandoInativacaoImpactaAgenda()
    {
        _horarioProfissionalNegocioService
            .Setup(s => s.AtualizarStatusAsync(20, 70, It.IsAny<AtualizarStatusHorarioProfissionalRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HorarioAlteracaoImpactaAgendamentosFuturosException(
                "A inativacao impactaria agendamentos futuros do profissional.",
                []));

        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAlteracaoImpactaAgendamentosFuturosException>(() =>
            service.AtualizarStatusAsync(
                30,
                70,
                new AtualizarStatusHorarioProfissionalRequestDto { Ativo = false }));
    }

    [Fact]
    public async Task CriarAsync_DeveLancarExcecao_QuandoProfissionalNaoForAutonomo()
    {
        _profissionalRepository
            .Setup(r => r.ObterPorIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Profissional
            {
                Id = 30,
                UsuarioId = 10,
                TipoProfissional = ProfessionalType.VinculadoEstabelecimento,
                Ativo = true
            });

        var service = CreateService();

        await Assert.ThrowsAsync<ProfissionalAutonomoAssinaturaInvalidoException>(() =>
            service.CriarAsync(30, CriarRequestValido()));
    }

    [Fact]
    public async Task CriarAsync_DeveLancarExcecao_QuandoProfissionalPertenceAOutroUsuario()
    {
        _profissionalRepository
            .Setup(r => r.ObterPorIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Profissional
            {
                Id = 30,
                UsuarioId = 99,
                TipoProfissional = ProfessionalType.Autonomo,
                Ativo = true
            });

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoAssinaturaException>(() =>
            service.CriarAsync(30, CriarRequestValido()));
    }

    [Fact]
    public async Task CriarAsync_DevePropagarExcecao_QuandoHorarioForInvalido()
    {
        _horarioProfissionalNegocioService
            .Setup(s => s.CriarAsync(20, 30, It.IsAny<CriarHorarioProfissionalRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HorarioAtendimentoInvalidoException("A hora de inicio deve ser menor que a hora de fim."));

        var service = CreateService();

        await Assert.ThrowsAsync<HorarioAtendimentoInvalidoException>(() =>
            service.CriarAsync(30, new CriarHorarioProfissionalRequestDto
            {
                DiaSemana = DayOfWeek.Monday,
                HoraInicio = new TimeOnly(12, 0),
                HoraFim = new TimeOnly(12, 0)
            }));
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

    private HorarioProfissionalAutonomoService CreateService() =>
        new(
            _profissionalRepository.Object,
            _profissionalEstabelecimentoRepository.Object,
            _horarioProfissionalNegocioService.Object,
            _currentUserContext.Object);
}
