using GLOWAPI.Application.DTOs.Agenda;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Agenda;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class AgendaNegocioServiceTests
{
    private readonly Mock<IAgendamentoRepository> _agendamentoRepository = new();
    private readonly Mock<IAgendamentoItemRepository> _agendamentoItemRepository = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();
    private readonly Mock<IProfissionalEscopoAcessoService> _profissionalEscopoAcessoService = new();

    public AgendaNegocioServiceTests()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.AgendaVisualizarGeral,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GLOWAPI.Application.Models.Autorizacao.AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Receptionist,
                false,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.AgendaVisualizarGeral }));
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.AgendaVisualizarPropria,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GLOWAPI.Application.Models.Autorizacao.AutorizacaoNegocioResultado(
                20,
                10,
                EstablishmentUserRole.Profissional,
                true,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.AgendaVisualizarPropria }));
        _profissionalEscopoAcessoService
            .Setup(s => s.ObterEscopoAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GLOWAPI.Application.Models.Autorizacao.EscopoProfissionalResultado(
                20,
                10,
                70,
                90,
                true));
    }

    [Fact]
    public async Task ListarAgendaGeralAsync_DeveAutorizarEMapearAgendamentos()
    {
        _agendamentoRepository
            .Setup(r => r.ListarAgendaGeralAsync(It.IsAny<AgendaGeralFiltro>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Agendamento
                {
                    Id = 100,
                    EstabelecimentoId = 20,
                    UsuarioClienteId = 200,
                    UsuarioCliente = new Usuario { Id = 200, Nome = "Cliente Teste" },
                    Status = AgendamentoStatus.Confirmado,
                    ValorTotal = 120,
                    Observacao = "obs",
                    Itens =
                    [
                        new AgendamentoItem
                        {
                            Id = 300,
                            ServicoId = 400,
                            Servico = new Servico { Id = 400, Nome = "Corte" },
                            ProfissionalId = 500,
                            Profissional = new Profissional { Id = 500, NomePublico = "Maria" },
                            Inicio = new DateTime(2026, 5, 29, 10, 0, 0, DateTimeKind.Utc),
                            Fim = new DateTime(2026, 5, 29, 11, 0, 0, DateTimeKind.Utc),
                            Valor = 120,
                            Status = AgendamentoItemStatus.Confirmado
                        }
                    ]
                }
            ]);

        var service = CreateService();

        var response = await service.ListarAgendaGeralAsync(20, new AgendaGeralFiltroDto());

        Assert.Single(response);
        Assert.Equal(100, response[0].Id);
        Assert.Equal("Cliente Teste", response[0].ClienteNome);
        Assert.Equal("Confirmado", response[0].Status);
        Assert.Single(response[0].Itens);
        Assert.Equal("Corte", response[0].Itens[0].ServicoNome);
        Assert.Equal("Maria", response[0].Itens[0].ProfissionalNome);
    }

    [Fact]
    public async Task ListarAgendaGeralAsync_DeveRepassarFiltrosParaRepositorio()
    {
        AgendaGeralFiltro? filtroCapturado = null;
        var inicio = new DateTime(2026, 5, 29, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddDays(1);

        _agendamentoRepository
            .Setup(r => r.ListarAgendaGeralAsync(It.IsAny<AgendaGeralFiltro>(), It.IsAny<CancellationToken>()))
            .Callback<AgendaGeralFiltro, CancellationToken>((filtro, _) => filtroCapturado = filtro)
            .ReturnsAsync([]);

        var service = CreateService();

        await service.ListarAgendaGeralAsync(20, new AgendaGeralFiltroDto
        {
            ProfissionalId = 70,
            ClienteId = 30,
            Status = AgendamentoStatus.Confirmado,
            Inicio = inicio,
            Fim = fim
        });

        Assert.NotNull(filtroCapturado);
        Assert.Equal(20, filtroCapturado!.EstabelecimentoId);
        Assert.Equal(70, filtroCapturado.ProfissionalId);
        Assert.Equal(30, filtroCapturado.ClienteId);
        Assert.Equal(AgendamentoStatus.Confirmado, filtroCapturado.Status);
        Assert.Equal(inicio, filtroCapturado.Inicio);
        Assert.Equal(fim, filtroCapturado.Fim);
    }

    [Fact]
    public async Task ListarAgendaGeralAsync_DeveLancarExcecao_QuandoUsuarioNaoTemPermissao()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.AgendaVisualizarGeral,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsuarioSemPermissaoNegocioException());

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoNegocioException>(() =>
            service.ListarAgendaGeralAsync(20, new AgendaGeralFiltroDto()));

        _agendamentoRepository.Verify(
            r => r.ListarAgendaGeralAsync(It.IsAny<AgendaGeralFiltro>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ListarAgendaProfissionalAsync_DeveAutorizarEscopoEMapearItensSemDadosFinanceiros()
    {
        _agendamentoItemRepository
            .Setup(r => r.ListarAgendaProfissionalAsync(It.IsAny<AgendaProfissionalFiltro>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AgendamentoItem
                {
                    Id = 300,
                    AgendamentoId = 100,
                    ProfissionalId = 70,
                    ServicoId = 400,
                    Servico = new Servico { Id = 400, Nome = "Corte" },
                    Agendamento = new Agendamento
                    {
                        Id = 100,
                        EstabelecimentoId = 20,
                        UsuarioClienteId = 200,
                        UsuarioCliente = new Usuario { Id = 200, Nome = "Cliente Teste" }
                    },
                    Inicio = new DateTime(2026, 5, 29, 10, 0, 0, DateTimeKind.Utc),
                    Fim = new DateTime(2026, 5, 29, 11, 0, 0, DateTimeKind.Utc),
                    Valor = 999,
                    Status = AgendamentoItemStatus.Confirmado
                }
            ]);

        var service = CreateService();

        var response = await service.ListarAgendaProfissionalAsync(20, new AgendaProfissionalFiltroDto());

        Assert.Single(response);
        Assert.Equal(300, response[0].AgendamentoItemId);
        Assert.Equal(100, response[0].AgendamentoId);
        Assert.Equal(200, response[0].UsuarioClienteId);
        Assert.Equal("Cliente Teste", response[0].ClienteNome);
        Assert.Equal("Corte", response[0].ServicoNome);
        Assert.Equal("Confirmado", response[0].Status);
    }

    [Fact]
    public async Task ListarAgendaProfissionalAsync_DeveUsarProfissionalDoEscopo()
    {
        AgendaProfissionalFiltro? filtroCapturado = null;
        var inicio = new DateTime(2026, 5, 29, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddDays(1);

        _agendamentoItemRepository
            .Setup(r => r.ListarAgendaProfissionalAsync(It.IsAny<AgendaProfissionalFiltro>(), It.IsAny<CancellationToken>()))
            .Callback<AgendaProfissionalFiltro, CancellationToken>((filtro, _) => filtroCapturado = filtro)
            .ReturnsAsync([]);

        var service = CreateService();

        await service.ListarAgendaProfissionalAsync(20, new AgendaProfissionalFiltroDto
        {
            Status = AgendamentoItemStatus.Confirmado,
            Inicio = inicio,
            Fim = fim
        });

        Assert.NotNull(filtroCapturado);
        Assert.Equal(20, filtroCapturado!.EstabelecimentoId);
        Assert.Equal(70, filtroCapturado.ProfissionalId);
        Assert.Equal(AgendamentoItemStatus.Confirmado, filtroCapturado.Status);
        Assert.Equal(inicio, filtroCapturado.Inicio);
        Assert.Equal(fim, filtroCapturado.Fim);
    }

    [Fact]
    public async Task ListarAgendaProfissionalAsync_DeveLancarExcecao_QuandoNaoTemPermissao()
    {
        _autorizacaoNegocioService
            .Setup(s => s.AutorizarAsync(
                20,
                PermissaoNegocio.AgendaVisualizarPropria,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsuarioSemPermissaoNegocioException());

        var service = CreateService();

        await Assert.ThrowsAsync<UsuarioSemPermissaoNegocioException>(() =>
            service.ListarAgendaProfissionalAsync(20, new AgendaProfissionalFiltroDto()));

        _profissionalEscopoAcessoService.Verify(
            s => s.ObterEscopoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private AgendaNegocioService CreateService() =>
        new(
            _agendamentoRepository.Object,
            _agendamentoItemRepository.Object,
            _autorizacaoNegocioService.Object,
            _profissionalEscopoAcessoService.Object);
}
