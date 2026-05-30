using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using System.Text.Json;

namespace GLOWAPI.Application.Services;

public class AgendamentoNotificacaoService : IAgendamentoNotificacaoService
{
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;

    public AgendamentoNotificacaoService(IMensagemNotificacaoService mensagemNotificacaoService)
    {
        _mensagemNotificacaoService = mensagemNotificacaoService;
    }

    public Task AgendamentoCriadoAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional,
        CancellationToken cancellationToken = default) =>
        EnfileirarAsync(
            estabelecimento,
            profissional,
            "Novo agendamento recebido",
            MontarMensagemCriacao(agendamento, estabelecimento, profissional),
            "agendamento-criado",
            agendamento,
            cancellationToken);

    public Task AgendamentoConfirmadoAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional,
        CancellationToken cancellationToken = default) =>
        EnfileirarAsync(
            estabelecimento,
            profissional,
            "Agendamento confirmado",
            MontarMensagemStatus(agendamento, "confirmado"),
            "agendamento-confirmado",
            agendamento,
            cancellationToken);

    public Task AgendamentoCanceladoAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional,
        string motivo,
        CancellationToken cancellationToken = default) =>
        EnfileirarAsync(
            estabelecimento,
            profissional,
            "Agendamento cancelado",
            $"{MontarMensagemStatus(agendamento, "cancelado")} Motivo: {motivo}",
            "agendamento-cancelado",
            agendamento,
            cancellationToken);

    public Task AgendamentoRemarcadoAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional,
        string? motivo,
        CancellationToken cancellationToken = default)
    {
        var conteudo = MontarMensagemStatus(agendamento, "remarcado");
        if (!string.IsNullOrWhiteSpace(motivo))
        {
            conteudo = $"{conteudo} Motivo: {motivo}";
        }

        return EnfileirarAsync(
            estabelecimento,
            profissional,
            "Agendamento remarcado",
            conteudo,
            "agendamento-remarcado",
            agendamento,
            cancellationToken);
    }

    private async Task EnfileirarAsync(
        Estabelecimento estabelecimento,
        Profissional profissional,
        string assunto,
        string conteudo,
        string evento,
        Agendamento agendamento,
        CancellationToken cancellationToken)
    {
        var destinatarios = new[]
        {
            estabelecimento.Telefone,
            profissional.Telefone
        };

        foreach (var destinatario in destinatarios.Where(telefone => !string.IsNullOrWhiteSpace(telefone)).Distinct())
        {
            await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
            {
                Canal = CanalMensagemNotificacao.WhatsApp,
                Destinatario = destinatario!.Trim(),
                Assunto = assunto,
                Conteudo = conteudo,
                EstabelecimentoId = estabelecimento.Id,
                Prioridade = 1,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    evento,
                    agendamentoId = agendamento.Id,
                    cliente = agendamento.ClienteNome ?? agendamento.UsuarioCliente?.Nome,
                    inicio = agendamento.Itens.OrderBy(item => item.Inicio).FirstOrDefault()?.Inicio,
                    valorTotal = agendamento.ValorTotal
                })
            }, cancellationToken);
        }
    }

    private static string MontarMensagemCriacao(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional)
    {
        var cliente = agendamento.ClienteNome ?? agendamento.UsuarioCliente?.Nome ?? "Cliente";
        var inicio = agendamento.Itens.OrderBy(item => item.Inicio).FirstOrDefault()?.Inicio;
        var servicos = string.Join(", ", agendamento.Itens.Select(item => item.Servico?.Nome ?? "Servico"));

        return $"Novo agendamento em {estabelecimento.Nome} com {profissional.NomePublico}. Cliente: {cliente}. Data: {inicio:dd/MM/yyyy HH:mm}. Servicos: {servicos}. Valor: R$ {agendamento.ValorTotal:F2}.";
    }

    private static string MontarMensagemStatus(Agendamento agendamento, string acao)
    {
        var cliente = agendamento.ClienteNome ?? agendamento.UsuarioCliente?.Nome ?? "Cliente";
        var inicio = agendamento.Itens.OrderBy(item => item.Inicio).FirstOrDefault()?.Inicio;
        return $"Agendamento {acao} para {cliente} em {inicio:dd/MM/yyyy HH:mm}. Valor: R$ {agendamento.ValorTotal:F2}.";
    }
}
