using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class AtendimentoProfissionalServiceTests
{
    private readonly Mock<IAgendamentoItemRepository> _agendamentoItemRepository = new();
    private readonly Mock<IAgendamentoHistoricoRepository> _agendamentoHistoricoRepository = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();
    private readonly Mock<IProfissionalEscopoAcessoService> _profissionalEscopoAcessoService = new();
    private readonly Mock<ICurrentUserContext> _currentUserContext = new();
    private readonly Mock<IAvaliacaoAtendimentoService> _avaliacaoAtendimentoService = new();

    public AtendimentoProfissionalServiceTests()
    {
        _currentUserContext.SetupGet(c => c.IsAuthenticated).Returns(true);
        _currentUserContext.SetupGet(c => c.UserId).Returns(10);

        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                It.IsAny<PermissaoNegocio>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GLOWAPI.Application.Models.Autorizacao.AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Profissional,
                true,
                new HashSet<PermissaoNegocio>
                {
                    PermissaoNegocio.AtendimentoIniciar,
                    PermissaoNegocio.AtendimentoFinalizar
                }));

        _autorizacaoNegocioService
            .Setup(s => s.ObterContextoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GLOWAPI.Application.Models.Autorizacao.AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Profissional,
                true,
                new HashSet<PermissaoNegocio>
                {
                    PermissaoNegocio.AtendimentoIniciar,
                    PermissaoNegocio.AtendimentoFinalizar
                }));

        _profissionalEscopoAcessoService
            .Setup(s => s.AutorizarAgendamentoItemAsync(20, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GLOWAPI.Application.Models.Autorizacao.EscopoProfissionalResultado(
                20,
                10,
                70,
                90,
                true));

        _agendamentoItemRepository
            .Setup(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _agendamentoHistoricoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<AgendamentoHistorico>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task IniciarAsync_DeveAlterarItemEAgendamentoParaEmAtendimento()
    {
        var item = CriarItem(AgendamentoItemStatus.Confirmado);
        _agendamentoItemRepository
            .Setup(r => r.ObterPorIdComAgendamentoAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var service = CreateService();

        var response = await service.IniciarAsync(20, 100);

        Assert.Equal(AgendamentoItemStatus.EmAtendimento, item.Status);
        Assert.Equal(AgendamentoStatus.EmAtendimento, item.Agendamento!.Status);
        Assert.NotNull(item.UpdatedAt);
        Assert.Equal("EmAtendimento", response.StatusItem);
        _agendamentoItemRepository.Verify(r => r.Atualizar(item), Times.Once);
        _agendamentoHistoricoRepository.Verify(
            r => r.AdicionarAsync(It.IsAny<AgendamentoHistorico>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IniciarAsync_DeveLancarExcecao_QuandoStatusNaoForConfirmado()
    {
        var item = CriarItem(AgendamentoItemStatus.Pendente);
        _agendamentoItemRepository
            .Setup(r => r.ObterPorIdComAgendamentoAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var service = CreateService();

        await Assert.ThrowsAsync<AtendimentoStatusInvalidoException>(() =>
            service.IniciarAsync(20, 100));
    }

    [Fact]
    public async Task IniciarAsync_DeveLancarExcecao_QuandoHorarioAindaNaoChegou()
    {
        var item = CriarItem(AgendamentoItemStatus.Confirmado);
        item.Inicio = DateTime.UtcNow.AddHours(2);
        item.Fim = DateTime.UtcNow.AddHours(3);
        _agendamentoItemRepository
            .Setup(r => r.ObterPorIdComAgendamentoAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var service = CreateService();

        await Assert.ThrowsAsync<AtendimentoStatusInvalidoException>(() =>
            service.IniciarAsync(20, 100));
    }

    [Fact]
    public async Task IniciarAsync_DevePermitirInicio_AposHorarioFimDoSlot()
    {
        var item = CriarItem(AgendamentoItemStatus.Confirmado);
        item.Inicio = DateTime.UtcNow.AddHours(-5);
        item.Fim = DateTime.UtcNow.AddHours(-4);
        _agendamentoItemRepository
            .Setup(r => r.ObterPorIdComAgendamentoAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var service = CreateService();

        var response = await service.IniciarAsync(20, 100);

        Assert.Equal(AgendamentoItemStatus.EmAtendimento, item.Status);
        Assert.Equal("EmAtendimento", response.StatusItem);
    }

    [Fact]
    public async Task IniciarAsync_DeveLancarExcecao_QuandoAgendamentoCancelado()
    {
        var item = CriarItem(AgendamentoItemStatus.Confirmado);
        item.Agendamento!.Status = AgendamentoStatus.Cancelado;
        _agendamentoItemRepository
            .Setup(r => r.ObterPorIdComAgendamentoAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var service = CreateService();

        await Assert.ThrowsAsync<AtendimentoStatusInvalidoException>(() =>
            service.IniciarAsync(20, 100));
    }

    [Fact]
    public async Task IniciarAsync_DeveBloquearItemForaDoEscopo()
    {
        _profissionalEscopoAcessoService
            .Setup(s => s.AutorizarAgendamentoItemAsync(20, 100, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RecursoForaEscopoProfissionalException());

        var service = CreateService();

        await Assert.ThrowsAsync<RecursoForaEscopoProfissionalException>(() =>
            service.IniciarAsync(20, 100));

        _agendamentoItemRepository.Verify(
            r => r.ObterPorIdComAgendamentoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IniciarAsync_DevePermitirLojaSemEscopoProfissional()
    {
        _autorizacaoNegocioService
            .Setup(s => s.ObterContextoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GLOWAPI.Application.Models.Autorizacao.AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Owner,
                false,
                new HashSet<PermissaoNegocio>
                {
                    PermissaoNegocio.AgendaVisualizarGeral,
                    PermissaoNegocio.AtendimentoIniciar
                }));

        var item = CriarItem(AgendamentoItemStatus.Confirmado);
        _agendamentoItemRepository
            .Setup(r => r.ObterPorIdComAgendamentoAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var service = CreateService();

        await service.IniciarAsync(20, 100);

        _profissionalEscopoAcessoService.Verify(
            s => s.AutorizarAgendamentoItemAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task FinalizarAsync_DeveConcluirTodoAtendimento_IncluindoItensConfirmadosPendentes()
    {
        var item = CriarItem(AgendamentoItemStatus.EmAtendimento);
        item.Agendamento!.Status = AgendamentoStatus.EmAtendimento;
        var outro = new AgendamentoItem
        {
            Id = 101,
            AgendamentoId = item.AgendamentoId,
            ProfissionalId = 71,
            Status = AgendamentoItemStatus.Confirmado
        };
        item.Agendamento.Itens.Add(outro);
        _agendamentoItemRepository
            .Setup(r => r.ObterPorIdComAgendamentoAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _avaliacaoAtendimentoService
            .Setup(s => s.SolicitarAposConclusaoAsync(item.Agendamento, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.FinalizarAsync(20, 100);

        Assert.Equal(AgendamentoItemStatus.Concluido, item.Status);
        Assert.Equal(AgendamentoItemStatus.Concluido, outro.Status);
        Assert.Equal(AgendamentoStatus.Concluido, item.Agendamento.Status);
        Assert.Equal("Concluido", response.StatusItem);
        Assert.Equal("Concluido", response.StatusAgendamento);
        _agendamentoHistoricoRepository.Verify(
            r => r.AdicionarAsync(It.IsAny<AgendamentoHistorico>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _avaliacaoAtendimentoService.Verify(
            s => s.SolicitarAposConclusaoAsync(item.Agendamento, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FinalizarAsync_DeveConcluirAgendamento_IgnorandoItensCancelados()
    {
        var item = CriarItem(AgendamentoItemStatus.EmAtendimento);
        item.Agendamento!.Status = AgendamentoStatus.EmAtendimento;
        item.Agendamento.Itens.Add(new AgendamentoItem
        {
            Id = 101,
            AgendamentoId = item.AgendamentoId,
            ProfissionalId = 71,
            Status = AgendamentoItemStatus.Cancelado
        });
        _agendamentoItemRepository
            .Setup(r => r.ObterPorIdComAgendamentoAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        _avaliacaoAtendimentoService
            .Setup(s => s.SolicitarAposConclusaoAsync(item.Agendamento, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var response = await service.FinalizarAsync(20, 100);

        Assert.Equal(AgendamentoItemStatus.Concluido, item.Status);
        Assert.Equal(AgendamentoStatus.Concluido, item.Agendamento.Status);
        Assert.Equal("Concluido", response.StatusAgendamento);
        _avaliacaoAtendimentoService.Verify(
            s => s.SolicitarAposConclusaoAsync(item.Agendamento, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FinalizarAsync_DeveLancarExcecao_QuandoNaoHaItemEmAtendimento()
    {
        var item = CriarItem(AgendamentoItemStatus.Confirmado);
        item.Agendamento!.Status = AgendamentoStatus.EmAtendimento;
        _agendamentoItemRepository
            .Setup(r => r.ObterPorIdComAgendamentoAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var service = CreateService();

        await Assert.ThrowsAsync<AtendimentoStatusInvalidoException>(() =>
            service.FinalizarAsync(20, 100));
    }

    [Fact]
    public async Task FinalizarAsync_DeveLancarExcecao_QuandoStatusNaoForEmAtendimento()
    {
        var item = CriarItem(AgendamentoItemStatus.Confirmado);
        _agendamentoItemRepository
            .Setup(r => r.ObterPorIdComAgendamentoAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var service = CreateService();

        await Assert.ThrowsAsync<AtendimentoStatusInvalidoException>(() =>
            service.FinalizarAsync(20, 100));
    }

    [Fact]
    public async Task FinalizarAsync_DeveLancarNaoEncontrado_QuandoItemNaoExistir()
    {
        _agendamentoItemRepository
            .Setup(r => r.ObterPorIdComAgendamentoAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgendamentoItem?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<RecursoProfissionalNaoEncontradoException>(() =>
            service.FinalizarAsync(20, 100));
    }

    [Fact]
    public async Task IniciarAsync_DeveLancarExcecao_QuandoUsuarioNaoTemPermissao()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.AtendimentoIniciar,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsuarioSemPermissaoNegocioException());

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoNegocioException>(() =>
            service.IniciarAsync(20, 100));

        _profissionalEscopoAcessoService.Verify(
            s => s.AutorizarAgendamentoItemAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static AgendamentoItem CriarItem(AgendamentoItemStatus status)
    {
        var agora = DateTime.UtcNow;
        var item = new AgendamentoItem
        {
            Id = 100,
            AgendamentoId = 50,
            ProfissionalId = 70,
            Status = status,
            Inicio = agora.AddMinutes(-10),
            Fim = agora.AddMinutes(50),
            Agendamento = new Agendamento
            {
                Id = 50,
                EstabelecimentoId = 20,
                UsuarioClienteId = 200,
                Status = status == AgendamentoItemStatus.EmAtendimento
                    ? AgendamentoStatus.EmAtendimento
                    : AgendamentoStatus.Confirmado
            }
        };

        item.Agendamento.Itens.Add(item);
        return item;
    }

    private AtendimentoProfissionalService CreateService() =>
        new(
            _agendamentoItemRepository.Object,
            _agendamentoHistoricoRepository.Object,
            _autorizacaoNegocioService.Object,
            _profissionalEscopoAcessoService.Object,
            _currentUserContext.Object,
            _avaliacaoAtendimentoService.Object);
}
