using GLOWAPI.Application.DTOs.Mensageria;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<AgendamentoNotificacaoService> _logger;
    private readonly IModulosAssinaturaService _modulosAssinaturaService;

    public AgendamentoNotificacaoService(
        IMensagemNotificacaoService mensagemNotificacaoService,
        IModulosAssinaturaService modulosAssinaturaService,
        ILogger<AgendamentoNotificacaoService> logger)
    {
        _mensagemNotificacaoService = mensagemNotificacaoService;
        _modulosAssinaturaService = modulosAssinaturaService;
        _logger = logger;
    }

    public async Task AgendamentoCriadoAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional,
        CancellationToken cancellationToken = default)
    {
        var conteudoLoja = MontarMensagemCriacao(agendamento, estabelecimento, profissional);
        _logger.LogInformation("WhatsApp Loja (Agendamento Criado): Conteudo gerado: {Conteudo}", conteudoLoja);
        await EnfileirarNegocioAsync(
            estabelecimento,
            profissional,
            "Novo agendamento recebido",
            conteudoLoja,
            "agendamento-criado",
            agendamento,
            cancellationToken);

        var inicio = AgendamentoHorarioHelper.ObterInicio(agendamento);
        await EnfileirarClienteContatoAsync(
            agendamento,
            estabelecimento,
            "Agendamento recebido",
            $"Recebemos seu pedido de agendamento em {estabelecimento.Nome} com {profissional.NomePublico} para {inicio:dd/MM/yyyy HH:mm}. Aguarde a confirmacao da loja.",
            AgendamentoClienteEmailTemplate.Criado(agendamento, estabelecimento, profissional),
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

        var conteudoClienteWhatsApp = AgendamentoClienteWhatsAppTemplate.Confirmado(agendamento, estabelecimento);
        _logger.LogInformation("WhatsApp Cliente (Agendamento Confirmado): Conteudo gerado: {Conteudo}", conteudoClienteWhatsApp);
        await EnfileirarClienteAsync(
            agendamento,
            estabelecimento,
            "Agendamento confirmado",
            conteudoClienteWhatsApp,
            AgendamentoClienteEmailTemplate.Confirmado(agendamento, estabelecimento),
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

        var conteudoClienteWhatsApp = AgendamentoClienteWhatsAppTemplate.Cancelado(agendamento, estabelecimento, motivo);
        _logger.LogInformation("WhatsApp Cliente (Agendamento Cancelado): Conteudo gerado: {Conteudo}", conteudoClienteWhatsApp);
        await EnfileirarClienteAsync(
            agendamento,
            estabelecimento,
            "Agendamento cancelado",
            conteudoClienteWhatsApp,
            AgendamentoClienteEmailTemplate.Cancelado(agendamento, estabelecimento, motivo),
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
            AgendamentoClienteEmailTemplate.PropostaRemarcacao(
                agendamento,
                estabelecimento,
                proposta.DataSugerida,
                proposta.HorarioInicioSugerido,
                proposta.Motivo,
                linkResposta),
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

        var conteudoClienteWhatsApp = AgendamentoClienteWhatsAppTemplate.Remarcado(agendamento, estabelecimento);
        _logger.LogInformation("WhatsApp Cliente (Agendamento Remarcado): Conteudo gerado: {Conteudo}", conteudoClienteWhatsApp);
        await EnfileirarClienteAsync(
            agendamento,
            estabelecimento,
            "Agendamento remarcado",
            conteudoClienteWhatsApp,
            AgendamentoClienteEmailTemplate.Remarcado(agendamento, estabelecimento),
            "agendamento-cliente-remarcado",
            cancellationToken);
    }

    public async Task AgendamentoConcluidoAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        Profissional profissional,
        string linkAvaliacao,
        CancellationToken cancellationToken = default)
    {
        var inicio = AgendamentoHorarioHelper.ObterInicio(agendamento);
        var mensagemWhatsApp =
            $"Seu atendimento em {estabelecimento.Nome} com {profissional.NomePublico} foi concluido. " +
            $"Avalie sua experiencia em: {linkAvaliacao}";

        await EnfileirarClienteContatoAsync(
            agendamento,
            estabelecimento,
            "Avalie seu atendimento",
            mensagemWhatsApp,
            AgendamentoClienteEmailTemplate.Concluido(
                agendamento,
                estabelecimento,
                profissional,
                linkAvaliacao),
            "agendamento-cliente-concluido",
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
        var payloadJson = JsonSerializer.Serialize(new
        {
            evento,
            agendamentoId = agendamento.Id,
            cliente = agendamento.ClienteNome ?? agendamento.UsuarioCliente?.Nome,
            inicio = AgendamentoHorarioHelper.ObterInicio(agendamento),
            valorTotal = agendamento.ValorTotal
        });

        if (!string.IsNullOrWhiteSpace(estabelecimento.Email))
        {
            await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
            {
                Canal = CanalMensagemNotificacao.Email,
                Destinatario = estabelecimento.Email.Trim(),
                Assunto = assunto,
                Conteudo = conteudo,
                EstabelecimentoId = estabelecimento.Id,
                Prioridade = 1,
                PayloadJson = payloadJson
            }, cancellationToken);
        }

        var emailProfissional = !string.IsNullOrWhiteSpace(profissional.Email)
            ? profissional.Email
            : profissional.Usuario?.Email;
        if (!string.IsNullOrWhiteSpace(emailProfissional))
        {
            await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
            {
                Canal = CanalMensagemNotificacao.Email,
                Destinatario = emailProfissional.Trim(),
                Assunto = assunto,
                Conteudo = conteudo,
                EstabelecimentoId = estabelecimento.Id,
                Prioridade = 1,
                PayloadJson = payloadJson
            }, cancellationToken);
        }

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
                PayloadJson = payloadJson
            }, cancellationToken);
        }
    }

    private Task EnfileirarClienteAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        string assunto,
        string conteudoWhatsApp,
        string conteudoEmail,
        string evento,
        CancellationToken cancellationToken) =>
        EnfileirarClienteContatoAsync(
            agendamento,
            estabelecimento,
            assunto,
            conteudoWhatsApp,
            conteudoEmail,
            evento,
            cancellationToken);

    private async Task EnfileirarClienteContatoAsync(
        Agendamento agendamento,
        Estabelecimento estabelecimento,
        string assunto,
        string conteudoWhatsApp,
        string conteudoEmail,
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
                Conteudo = conteudoEmail,
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
            Conteudo = conteudoWhatsApp,
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
