using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class AgendamentoNotificacaoServiceTests
{
    private readonly Mock<IMensagemNotificacaoService> _mensagemNotificacaoService = new();
    private readonly Mock<IModulosAssinaturaService> _modulosAssinaturaService = new();

    public AgendamentoNotificacaoServiceTests()
    {
        _modulosAssinaturaService
            .Setup(s => s.PossuiModuloPorEstabelecimentoAsync(
                It.IsAny<int>(),
                ModuloAssinatura.WhatsApp,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    [Fact]
    public async Task AgendamentoCriadoAsync_DeveEnfileirarWhatsAppParaEstabelecimentoEProfissional_QuandoConfirmados()
    {
        var service = new AgendamentoNotificacaoService(
            _mensagemNotificacaoService.Object,
            _modulosAssinaturaService.Object);
        var agendamento = CriarAgendamento();
        var estabelecimento = CriarEstabelecimentoConfirmado("11999999999");
        var profissional = CriarProfissionalConfirmado("11988887777");

        await service.AgendamentoCriadoAsync(agendamento, estabelecimento, profissional);

        _mensagemNotificacaoService.Verify(
            s => s.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Canal == CanalMensagemNotificacao.WhatsApp
                    && dto.Destinatario == "5511999999999"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mensagemNotificacaoService.Verify(
            s => s.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Canal == CanalMensagemNotificacao.WhatsApp
                    && dto.Destinatario == "5511988887777"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AgendamentoCriadoAsync_NaoDeveEnfileirarWhatsApp_SemConfirmacao()
    {
        var service = new AgendamentoNotificacaoService(
            _mensagemNotificacaoService.Object,
            _modulosAssinaturaService.Object);
        var agendamento = CriarAgendamento();
        var estabelecimento = new Estabelecimento { Id = 1, Nome = "Loja", Telefone = "11999999999" };
        var profissional = new Profissional
        {
            Id = 2,
            NomePublico = "Joao",
            Telefone = "11988887777",
            Usuario = new Usuario { Telefone = "11988887777" }
        };

        await service.AgendamentoCriadoAsync(agendamento, estabelecimento, profissional);

        _mensagemNotificacaoService.Verify(
            s => s.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AgendamentoConfirmadoAsync_DeveEnfileirarWhatsAppParaCliente_QuandoConfirmadoEOptIn()
    {
        var service = new AgendamentoNotificacaoService(
            _mensagemNotificacaoService.Object,
            _modulosAssinaturaService.Object);

        var agendamento = CriarAgendamento();
        agendamento.UsuarioCliente = new Usuario
        {
            Id = 5,
            Nome = "Maria",
            Telefone = "11977776666",
            WhatsAppConfirmadoEm = DateTime.UtcNow,
            WhatsAppOptIn = true
        };

        var estabelecimento = CriarEstabelecimentoConfirmado("11999999999");
        var profissional = CriarProfissionalConfirmado("11988887777");

        await service.AgendamentoConfirmadoAsync(agendamento, estabelecimento, profissional);

        _mensagemNotificacaoService.Verify(
            s => s.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Canal == CanalMensagemNotificacao.WhatsApp
                    && dto.Destinatario == "5511977776666"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AgendamentoConfirmadoAsync_NaoDeveEnfileirarWhatsAppParaCliente_SemOptIn()
    {
        var service = new AgendamentoNotificacaoService(
            _mensagemNotificacaoService.Object,
            _modulosAssinaturaService.Object);

        var agendamento = CriarAgendamento();
        agendamento.UsuarioCliente = new Usuario
        {
            Id = 5,
            Nome = "Maria",
            Telefone = "11977776666",
            WhatsAppConfirmadoEm = DateTime.UtcNow,
            WhatsAppOptIn = false
        };

        var estabelecimento = CriarEstabelecimentoConfirmado("11999999999");
        var profissional = CriarProfissionalConfirmado("11988887777");

        await service.AgendamentoConfirmadoAsync(agendamento, estabelecimento, profissional);

        _mensagemNotificacaoService.Verify(
            s => s.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto => dto.Destinatario == "5511977776666"),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Agendamento CriarAgendamento()
    {
        var agendamento = new Agendamento
        {
            Id = 10,
            EstabelecimentoId = 1,
            Status = AgendamentoStatus.PendenteConfirmacao,
            ValorTotal = 55,
            ClienteNome = "Maria"
        };

        agendamento.Itens.Add(new AgendamentoItem
        {
            Inicio = DateTime.UtcNow.AddDays(1),
            Fim = DateTime.UtcNow.AddDays(1).AddHours(1),
            Servico = new Servico { Nome = "Corte" }
        });

        return agendamento;
    }

    private static Estabelecimento CriarEstabelecimentoConfirmado(string telefone) =>
        new()
        {
            Id = 1,
            Nome = "Loja",
            Telefone = telefone,
            WhatsAppConfirmadoEm = DateTime.UtcNow,
            WhatsAppOptIn = true
        };

    private static Profissional CriarProfissionalConfirmado(string telefone) =>
        new()
        {
            Id = 2,
            NomePublico = "Joao",
            Telefone = telefone,
            Usuario = new Usuario
            {
                Telefone = telefone,
                WhatsAppConfirmadoEm = DateTime.UtcNow,
                WhatsAppOptIn = true
            }
        };
}
