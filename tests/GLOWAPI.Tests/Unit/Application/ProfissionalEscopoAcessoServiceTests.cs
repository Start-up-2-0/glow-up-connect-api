using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Domain.Exceptions.Negocios;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class ProfissionalEscopoAcessoServiceTests
{
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<IAgendamentoItemRepository> _agendamentoItemRepository = new();
    private readonly Mock<ICurrentUserContext> _currentUserContext = new();

    public ProfissionalEscopoAcessoServiceTests()
    {
        _currentUserContext.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserContext.Setup(c => c.UserId).Returns(10);
    }

    [Fact]
    public async Task ObterEscopoAsync_DeveRetornarEscopo_QuandoUsuarioTemVinculoProfissionalAtivo()
    {
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterAtivoPorUsuarioAsync(10, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalEstabelecimento
            {
                Id = 90,
                EstabelecimentoId = 20,
                ProfissionalId = 70,
                PodeReceberAgendamento = true,
                Ativo = true
            });

        var service = CreateService();

        var escopo = await service.ObterEscopoAsync(20);

        Assert.Equal(20, escopo.EstabelecimentoId);
        Assert.Equal(10, escopo.UsuarioId);
        Assert.Equal(70, escopo.ProfissionalId);
        Assert.Equal(90, escopo.ProfissionalEstabelecimentoId);
        Assert.True(escopo.PodeReceberAgendamento);
    }

    [Fact]
    public async Task ObterEscopoAsync_DeveLancarExcecao_QuandoUsuarioNaoTemVinculoProfissional()
    {
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterAtivoPorUsuarioAsync(10, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfissionalEstabelecimento?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<ProfissionalSemVinculoNegocioException>(() =>
            service.ObterEscopoAsync(20));
    }

    [Fact]
    public async Task ObterEscopoAsync_DeveLancarUnauthorized_QuandoUsuarioNaoAutenticado()
    {
        _currentUserContext.Setup(c => c.IsAuthenticated).Returns(false);
        _currentUserContext.Setup(c => c.UserId).Returns((int?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.ObterEscopoAsync(20));
    }

    [Fact]
    public async Task AutorizarAgendamentoItemAsync_DevePermitirItemDoProprioProfissional()
    {
        ConfigurarEscopoProfissional();
        _agendamentoItemRepository
            .Setup(r => r.ObterPorIdComAgendamentoAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoItem
            {
                Id = 100,
                ProfissionalId = 70,
                Agendamento = new Agendamento
                {
                    Id = 50,
                    EstabelecimentoId = 20,
                    UsuarioClienteId = 200
                }
            });

        var service = CreateService();

        var escopo = await service.AutorizarAgendamentoItemAsync(20, 100);

        Assert.Equal(70, escopo.ProfissionalId);
    }

    [Fact]
    public async Task AutorizarAgendamentoItemAsync_DeveBloquearItemDeOutroProfissional()
    {
        ConfigurarEscopoProfissional();
        _agendamentoItemRepository
            .Setup(r => r.ObterPorIdComAgendamentoAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoItem
            {
                Id = 100,
                ProfissionalId = 71,
                Agendamento = new Agendamento
                {
                    Id = 50,
                    EstabelecimentoId = 20,
                    UsuarioClienteId = 200
                }
            });

        var service = CreateService();

        await Assert.ThrowsAsync<RecursoForaEscopoProfissionalException>(() =>
            service.AutorizarAgendamentoItemAsync(20, 100));
    }

    [Fact]
    public async Task AutorizarAgendamentoItemAsync_DeveBloquearItemDeOutroNegocio()
    {
        ConfigurarEscopoProfissional();
        _agendamentoItemRepository
            .Setup(r => r.ObterPorIdComAgendamentoAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgendamentoItem
            {
                Id = 100,
                ProfissionalId = 70,
                Agendamento = new Agendamento
                {
                    Id = 50,
                    EstabelecimentoId = 21,
                    UsuarioClienteId = 200
                }
            });

        var service = CreateService();

        await Assert.ThrowsAsync<RecursoForaEscopoProfissionalException>(() =>
            service.AutorizarAgendamentoItemAsync(20, 100));
    }

    [Fact]
    public async Task AutorizarAgendamentoItemAsync_DeveLancarNaoEncontrado_QuandoItemNaoExiste()
    {
        ConfigurarEscopoProfissional();
        _agendamentoItemRepository
            .Setup(r => r.ObterPorIdComAgendamentoAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgendamentoItem?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<RecursoProfissionalNaoEncontradoException>(() =>
            service.AutorizarAgendamentoItemAsync(20, 100));
    }

    [Fact]
    public async Task AutorizarClienteAsync_DevePermitirClienteComAgendamentoDoProfissional()
    {
        ConfigurarEscopoProfissional();
        _agendamentoItemRepository
            .Setup(r => r.ExisteClienteVinculadoAoProfissionalAsync(20, 70, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        var escopo = await service.AutorizarClienteAsync(20, 200);

        Assert.Equal(70, escopo.ProfissionalId);
    }

    [Fact]
    public async Task AutorizarClienteAsync_DeveBloquearClienteSemAgendamentoDoProfissional()
    {
        ConfigurarEscopoProfissional();
        _agendamentoItemRepository
            .Setup(r => r.ExisteClienteVinculadoAoProfissionalAsync(20, 70, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        await Assert.ThrowsAsync<RecursoForaEscopoProfissionalException>(() =>
            service.AutorizarClienteAsync(20, 200));
    }

    private void ConfigurarEscopoProfissional()
    {
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterAtivoPorUsuarioAsync(10, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalEstabelecimento
            {
                Id = 90,
                EstabelecimentoId = 20,
                ProfissionalId = 70,
                PodeReceberAgendamento = true,
                Ativo = true
            });
    }

    private ProfissionalEscopoAcessoService CreateService() =>
        new(
            _profissionalEstabelecimentoRepository.Object,
            _agendamentoItemRepository.Object,
            _currentUserContext.Object);
}
