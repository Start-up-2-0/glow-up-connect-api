using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Mensageria;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using System.Text.Json;

namespace GLOWAPI.Application.Services;

public class AgendamentoNotificacaoService : IAgendamentoNotificacaoService
{
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;
    private readonly IModulosAssinaturaService _modulosAssinaturaService;

    public AgendamentoNotificacaoService(
        IMensagemNotificacaoService mensagemNotificacaoService,
        IModulosAssinaturaService modulosAssinaturaService)
    {
        _mensagemNotificacaoService = mensagemNotificacaoService;
        _modulosAssinaturaService = modulosAssinaturaService;
    }

    public async Task AgendamentoCriadoAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional,
        CancellationToken cancellationToken = default)
    {
        await EnfileirarNegocioAsync(
            estabelecimento,
            profissional,
            "Novo agendamento recebido",
            MontarMensagemCriacao(agendamento, estabelecimento, profissional),
            "agendamento-criado",
            agendamento,
            cancellationToken);

        var inicio = AgendamentoHorarioHelper.ObterInicio(agendamento);
        await EnfileirarClienteContatoAsync(
            agendamento,
            estabelecimento,
            "Agendamento recebido",
            $"Recebemos seu pedido de agendamento em {estabelecimento.Nome} com {profissional.NomePublico} para {inicio:dd/MM/yyyy HH:mm}. Aguarde a confirmacao da loja.",
            "agendamento-cliente-criado",
            cancellationToken);
    }

    public async Task AgendamentoConfirmadoAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional,
        CancellationToken cancellationToken = default)
    {
        await EnfileirarNegocioAsync(
            estabelecimento,
            profissional,
            "Agendamento confirmado",
            MontarMensagemStatus(agendamento, "confirmado"),
            "agendamento-confirmado",
            agendamento,
            cancellationToken);

        await EnfileirarClienteAsync(
            agendamento,
            estabelecimento,
            "Agendamento confirmado",
            AgendamentoClienteWhatsAppTemplate.Confirmado(agendamento, estabelecimento),
            "agendamento-cliente-confirmado",
            cancellationToken);
    }

    public async Task AgendamentoCanceladoAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        await EnfileirarNegocioAsync(
            estabelecimento,
            profissional,
            "Agendamento cancelado",
            $"{MontarMensagemStatus(agendamento, "cancelado")} Motivo: {motivo}",
            "agendamento-cancelado",
            agendamento,
            cancellationToken);

        await EnfileirarClienteAsync(
            agendamento,
            estabelecimento,
            "Agendamento cancelado",
            AgendamentoClienteWhatsAppTemplate.Cancelado(agendamento, estabelecimento, motivo),
            "agendamento-cliente-cancelado",
            cancellationToken);
    }

    public async Task PropostaRemarcacaoEnviadaAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional,
        AgendamentoPropostaRemarcacao proposta,
        string linkResposta,
        CancellationToken cancellationToken = default)
    {
        var inicioAtual = AgendamentoHorarioHelper.ObterInicio(agendamento);
        var mensagem =
            $"A loja {estabelecimento.Nome} sugeriu um novo horario para seu agendamento: " +
            $"{proposta.DataSugerida:dd/MM/yyyy} as {proposta.HorarioInicioSugerido:HH:mm}. " +
            $"Horario atual: {inicioAtual:dd/MM/yyyy HH:mm}. Motivo: {proposta.Motivo}. " +
            $"Responda em: {linkResposta}";

        await EnfileirarClienteContatoAsync(
            agendamento,
            estabelecimento,
            "Sugestao de reagendamento",
            mensagem,
            "agendamento-proposta-remarcacao",
            cancellationToken);
    }

    public Task PropostaRemarcacaoRespondidaAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional,
        bool aceita,
        CancellationToken cancellationToken = default)
    {
        var acao = aceita ? "aceitou" : "recusou";
        return EnfileirarNegocioAsync(
            estabelecimento,
            profissional,
            $"Cliente {acao} reagendamento",
            $"O cliente {acao} a sugestao de reagendamento do agendamento #{agendamento.Id}.",
            $"agendamento-proposta-{acao}",
            agendamento,
            cancellationToken);
    }

    public async Task AgendamentoRemarcadoAsync(
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

        await EnfileirarNegocioAsync(
            estabelecimento,
            profissional,
            "Agendamento remarcado",
            conteudo,
            "agendamento-remarcado",
            agendamento,
            cancellationToken);

        await EnfileirarClienteAsync(
            agendamento,
            estabelecimento,
            "Agendamento remarcado",
            AgendamentoClienteWhatsAppTemplate.Remarcado(agendamento, estabelecimento),
            "agendamento-cliente-remarcado",
            cancellationToken);
    }

    private async Task EnfileirarNegocioAsync(
        Estabelecimento estabelecimento,
        Profissional profissional,
        string assunto,
        string conteudo,
        string evento,
        Agendamento agendamento,
        CancellationToken cancellationToken)
    {
        var possuiWhatsApp = await _modulosAssinaturaService.PossuiModuloPorEstabelecimentoAsync(
            estabelecimento.Id,
            ModuloAssinatura.WhatsApp,
            cancellationToken);

        if (!possuiWhatsApp)
        {
            return;
        }

        var destinatarios = new List<(string Telefone, int EstabelecimentoId)>();

        if (estabelecimento.PodeReceberAlertasWhatsApp())
        {
            var telefoneEstabelecimento = TelefoneHelper.NormalizarParaWhatsApp(estabelecimento.Telefone);
            if (!string.IsNullOrWhiteSpace(telefoneEstabelecimento))
            {
                destinatarios.Add((telefoneEstabelecimento, estabelecimento.Id));
            }
        }

        var usuarioProfissional = profissional.Usuario;
        if (usuarioProfissional?.PodeReceberAlertasWhatsApp() == true)
        {
            var telefoneProfissional = TelefoneHelper.NormalizarParaWhatsApp(usuarioProfissional.Telefone);
            if (!string.IsNullOrWhiteSpace(telefoneProfissional))
            {
                destinatarios.Add((telefoneProfissional, estabelecimento.Id));
            }
        }

        foreach (var (destinatario, estabelecimentoId) in destinatarios.Distinct())
        {
            await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
            {
                Canal = CanalMensagemNotificacao.WhatsApp,
                Destinatario = destinatario,
                Assunto = assunto,
                Conteudo = conteudo,
                EstabelecimentoId = estabelecimentoId,
                Prioridade = 1,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    evento,
                    agendamentoId = agendamento.Id,
                    cliente = agendamento.ClienteNome ?? agendamento.UsuarioCliente?.Nome,
                    inicio = AgendamentoHorarioHelper.ObterInicio(agendamento),
                    valorTotal = agendamento.ValorTotal
                })
            }, cancellationToken);
        }
    }

    private Task EnfileirarClienteAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        string assunto,
        string conteudo,
        string evento,
        CancellationToken cancellationToken) =>
        EnfileirarClienteContatoAsync(
            agendamento,
            estabelecimento,
            assunto,
            conteudo,
            evento,
            cancellationToken);

    private async Task EnfileirarClienteContatoAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        string assunto,
        string conteudo,
        string evento,
        CancellationToken cancellationToken)
    {
        if (!agendamento.EstabelecimentoId.HasValue)
        {
            return;
        }

        var email = agendamento.UsuarioCliente?.Email ?? agendamento.ClienteEmail;
        if (!string.IsNullOrWhiteSpace(email))
        {
            await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
            {
                Canal = CanalMensagemNotificacao.Email,
                Destinatario = email.Trim(),
                Assunto = assunto,
                Conteudo = conteudo,
                EstabelecimentoId = estabelecimento.Id,
                Prioridade = 1,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    evento,
                    agendamentoId = agendamento.Id
                })
            }, cancellationToken);
        }

        var possuiModulo = await _modulosAssinaturaService.PossuiModuloPorEstabelecimentoAsync(
            agendamento.EstabelecimentoId.Value,
            ModuloAssinatura.WhatsApp,
            cancellationToken);

        if (!possuiModulo)
        {
            return;
        }

        var telefoneRaw = agendamento.UsuarioCliente?.Telefone ?? agendamento.ClienteTelefone;
        var usuarioCliente = agendamento.UsuarioCliente;
        if (usuarioCliente is not null && !usuarioCliente.PodeReceberAlertasWhatsApp())
        {
            return;
        }

        var telefone = TelefoneHelper.NormalizarParaWhatsApp(telefoneRaw ?? string.Empty);
        if (string.IsNullOrWhiteSpace(telefone))
        {
            return;
        }

        await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.WhatsApp,
            Destinatario = telefone,
            Assunto = assunto,
            Conteudo = conteudo,
            EstabelecimentoId = estabelecimento.Id,
            Prioridade = 1,
            PayloadJson = JsonSerializer.Serialize(new
            {
                evento,
                agendamentoId = agendamento.Id,
                inicio = AgendamentoHorarioHelper.ObterInicio(agendamento)
            })
        }, cancellationToken);
    }

    private static string MontarMensagemCriacao(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional)
    {
        var cliente = agendamento.ClienteNome ?? agendamento.UsuarioCliente?.Nome ?? "Cliente";
        var inicio = AgendamentoHorarioHelper.ObterInicio(agendamento);
        var servicos = string.Join(", ", agendamento.Itens.Select(item => item.Servico?.Nome ?? "Servico"));

        return $"Novo agendamento em {estabelecimento.Nome} com {profissional.NomePublico}. Cliente: {cliente}. Data: {inicio:dd/MM/yyyy HH:mm}. Servicos: {servicos}. Valor: R$ {agendamento.ValorTotal:F2}.";
    }

    private static string MontarMensagemStatus(Agendamento agendamento, string acao)
    {
        var cliente = agendamento.ClienteNome ?? agendamento.UsuarioCliente?.Nome ?? "Cliente";
        var inicio = AgendamentoHorarioHelper.ObterInicio(agendamento);
        return $"Agendamento {acao} para {cliente} em {inicio:dd/MM/yyyy HH:mm}. Valor: R$ {agendamento.ValorTotal:F2}.";
    }
}
