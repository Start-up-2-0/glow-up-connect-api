using GLOWAPI.Application.DTOs.Agendamento;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Agendamento;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class AgendamentoNegocioServiceTests
{
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentoRepository = new();
    private readonly Mock<IProfissionalRepository> _profissionalRepository = new();
    private readonly Mock<IProfissionalEstabelecimentoRepository> _profissionalEstabelecimentoRepository = new();
    private readonly Mock<IAgendamentoRepository> _agendamentoRepository = new();
    private readonly Mock<IAgendamentoItemRepository> _agendamentoItemRepository = new();
    private readonly Mock<IAgendamentoHistoricoRepository> _agendamentoHistoricoRepository = new();
    private readonly Mock<IAgendamentoValidador> _agendamentoValidador = new();
    private readonly Mock<IAgendamentoNotificacaoService> _agendamentoNotificacaoService = new();
    private readonly Mock<IDisponibilidadeAgendaService> _disponibilidadeAgendaService = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();
    private readonly Mock<IAuditoriaNegocioService> _auditoriaNegocioService = new();
    private readonly Mock<ICurrentUserContext> _currentUserContext = new();
    private readonly Mock<IUsuarioService> _usuarioService = new();
    private readonly Mock<IAuthSessionService> _authSessionService = new();
    private readonly Mock<IAgendamentoPropostaRemarcacaoRepository> _propostaRemarcacaoRepository = new();
    private readonly Mock<IAvaliacaoAtendimentoRepository> _avaliacaoAtendimentoRepository = new();

    [Fact]
    public async Task ConfirmarAsync_DeveAlterarStatusParaConfirmado()
    {
        var agendamento = CriarAgendamentoPendente();
        _agendamentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoComItensAsync(10, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agendamento);

        var service = CreateService();
        var response = await service.ConfirmarAsync(1, 10);

        Assert.Equal("Confirmado", response.Status);
        Assert.Equal(AgendamentoStatus.Confirmado, agendamento.Status);
        _agendamentoRepository.Verify(r => r.SalvarAlteracoesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemarcarAsync_DevePassarAgendamentoIgnorarIdParaPreparar()
    {
        var agendamento = CriarAgendamentoPendente();
        var item = agendamento.Itens.First();
        item.ServicoId = 5;
        item.Inicio = DateTime.UtcNow.AddDays(3);
        item.Fim = item.Inicio.AddMinutes(60);

        _agendamentoRepository
            .Setup(r => r.ObterPorIdEEstabelecimentoComItensAsync(10, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agendamento);

        var preparacao = CriarPreparacao(
            agendamento.Estabelecimento!,
            item.Profissional!);

        _agendamentoValidador
            .Setup(v => v.PrepararAsync(
                1,
                2,
                It.IsAny<int[]>(),
                It.IsAny<DateOnly>(),
                It.IsAny<TimeOnly>(),
                It.IsAny<OrigemAgendamento>(),
                It.IsAny<CriarAgendamentoRequestDto>(),
                null,
                10,
                It.IsAny<DateTime?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(preparacao);

        var service = CreateService();
        await service.RemarcarAsync(
            1,
            10,
            new RemarcarAgendamentoRequestDto
            {
                Data = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                HorarioInicio = new TimeOnly(10, 0),
                Motivo = "Cliente pediu mesmo horario"
            });

        _agendamentoValidador.Verify(
            v => v.PrepararAsync(
                1,
                2,
                It.IsAny<int[]>(),
                It.IsAny<DateOnly>(),
                It.IsAny<TimeOnly>(),
                It.IsAny<OrigemAgendamento>(),
                It.IsAny<CriarAgendamentoRequestDto>(),
                null,
                10,
                It.IsAny<DateTime?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelarAsync_DeveExigirMotivo()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<AgendamentoStatusInvalidoException>(() =>
            service.CancelarAsync(1, 10, new CancelarAgendamentoRequestDto { Motivo = "  " }));
    }

    [Fact]
    public async Task CriarPublicoPorLojaAsync_DevePersistirAgendamentoVisitante()
    {
        var estabelecimento = new Estabelecimento { Id = 1, PublicGuid = Guid.NewGuid(), Ativo = true };
        var profissional = new Profissional { Id = 2, PublicGuid = Guid.NewGuid(), Ativo = true, Telefone = "11999999999" };

        _estabelecimentoRepository
            .Setup(r => r.ObterPorPublicGuidAsync(estabelecimento.PublicGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estabelecimento);
        _profissionalRepository
            .Setup(r => r.ObterPorPublicGuidAsync(profissional.PublicGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profissional);
        _profissionalEstabelecimentoRepository
            .Setup(r => r.ObterPorProfissionalAsync(2, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfissionalEstabelecimento { Ativo = true, PodeReceberAgendamento = true });

        _agendamentoValidador
            .Setup(v => v.PrepararAsync(
                1,
                2,
                It.IsAny<int[]>(),
                It.IsAny<DateOnly>(),
                It.IsAny<TimeOnly>(),
                OrigemAgendamento.PublicoLoja,
                It.IsAny<CriarAgendamentoRequestDto>(),
                null,
                It.IsAny<int?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CriarPreparacao(estabelecimento, profissional));

        Agendamento? agendamentoSalvo = null;
        _agendamentoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Agendamento>(), It.IsAny<CancellationToken>()))
            .Callback<Agendamento, CancellationToken>((agendamento, _) =>
            {
                agendamentoSalvo = agendamento;
                agendamento.Id = 99;
            })
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var response = await service.CriarPublicoPorLojaAsync(
            estabelecimento.PublicGuid,
            new CriarAgendamentoRequestDto
            {
                ProfissionalPublicGuid = profissional.PublicGuid,
                ServicoIds = [5],
                Data = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                HorarioInicio = new TimeOnly(10, 0),
                ClienteNome = "Visitante",
                ClienteEmail = "visitante@email.com",
                ClienteTelefone = "11988887777"
            });

        Assert.NotNull(agendamentoSalvo);
        Assert.Null(agendamentoSalvo!.UsuarioClienteId);
        Assert.Equal("Visitante", agendamentoSalvo.ClienteNome);
        Assert.Equal(99, response.Id);
        _agendamentoNotificacaoService.Verify(
            s => s.AgendamentoCriadoAsync(
                It.IsAny<Agendamento>(),
                estabelecimento,
                profissional,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private AgendamentoNegocioService CreateService() =>
        new(
            _estabelecimentoRepository.Object,
            _profissionalRepository.Object,
            _profissionalEstabelecimentoRepository.Object,
            _agendamentoRepository.Object,
            _agendamentoItemRepository.Object,
            _agendamentoHistoricoRepository.Object,
            _agendamentoValidador.Object,
            _agendamentoNotificacaoService.Object,
            _disponibilidadeAgendaService.Object,
            _autorizacaoNegocioService.Object,
            _auditoriaNegocioService.Object,
            _currentUserContext.Object,
            _usuarioService.Object,
            _authSessionService.Object,
            _propostaRemarcacaoRepository.Object,
            _avaliacaoAtendimentoRepository.Object,
            new Base64ImageThumbnailer(Options.Create(new AvatarOptions())),
            Options.Create(new AuthOptions { FrontendBaseUrl = "http://localhost:5173" }));

    private static Agendamento CriarAgendamentoPendente()
    {
        var agendamento = new Agendamento
        {
            Id = 10,
            EstabelecimentoId = 1,
            Status = AgendamentoStatus.PendenteConfirmacao,
            Estabelecimento = new Estabelecimento { Id = 1, Nome = "Loja", Telefone = "11999999999" }
        };

        agendamento.Itens.Add(new AgendamentoItem
        {
            ProfissionalId = 2,
            Profissional = new Profissional { Id = 2, Telefone = "11988887777" },
            Status = AgendamentoItemStatus.Pendente
        });

        return agendamento;
    }

    private static AgendamentoPreparacaoResultado CriarPreparacao(
        Estabelecimento estabelecimento,
        Profissional profissional)
    {
        var servico = new Servico { Id = 5, Nome = "Corte", DuracaoMinutos = 60, PrecoBase = 50 };
        var inicio = DateTime.UtcNow.AddDays(2);

        return new AgendamentoPreparacaoResultado
        {
            Estabelecimento = estabelecimento,
            Profissional = profissional,
            Servicos = [servico],
            VinculosProfissionalServico = [],
            Inicio = inicio,
            Fim = inicio.AddMinutes(60),
            ValorTotal = 55,
            DuracaoTotalMinutos = 60,
            ClienteNome = "Visitante",
            ClienteEmail = "visitante@email.com",
            ClienteTelefone = "11988887777",
            Itens =
            [
                new AgendamentoItemPreparacao
                {
                    Servico = servico,
                    Vinculo = null,
                    Inicio = inicio,
                    Fim = inicio.AddMinutes(60),
                    Valor = 55
                }
            ]
        };
    }
}
