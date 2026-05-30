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

    [Fact]
    public async Task AgendamentoCriadoAsync_DeveEnfileirarWhatsAppParaEstabelecimentoEProfissional()
    {
        var service = new AgendamentoNotificacaoService(_mensagemNotificacaoService.Object);
        var agendamento = CriarAgendamento();
        var estabelecimento = new Estabelecimento { Id = 1, Nome = "Loja", Telefone = "11999999999" };
        var profissional = new Profissional { Id = 2, NomePublico = "Joao", Telefone = "11988887777" };

        await service.AgendamentoCriadoAsync(agendamento, estabelecimento, profissional);

        _mensagemNotificacaoService.Verify(
            s => s.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Canal == CanalMensagemNotificacao.WhatsApp
                    && dto.Destinatario == "11999999999"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mensagemNotificacaoService.Verify(
            s => s.RegistrarAsync(
                It.Is<RegistrarMensagemNotificacaoDto>(dto =>
                    dto.Canal == CanalMensagemNotificacao.WhatsApp
                    && dto.Destinatario == "11988887777"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Agendamento CriarAgendamento()
    {
        var agendamento = new Agendamento
        {
            Id = 10,
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
}
