using System.Text.Json;
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Mensageria;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class WebhookWhatsAppService : IWebhookWhatsAppService
{
    private const int PayloadLogMaxLength = 8000;

    private readonly IConfirmacaoWhatsAppService _confirmacaoWhatsAppService;
    private readonly IConfirmacaoWhatsAppEstabelecimentoService _confirmacaoWhatsAppEstabelecimentoService;
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;
    private readonly MensageriaWhatsAppOptions _options;
    private readonly ILogger<WebhookWhatsAppService> _logger;

    public WebhookWhatsAppService(
        IConfirmacaoWhatsAppService confirmacaoWhatsAppService,
        IConfirmacaoWhatsAppEstabelecimentoService confirmacaoWhatsAppEstabelecimentoService,
        IMensagemNotificacaoService mensagemNotificacaoService,
        IOptions<MensageriaWhatsAppOptions> options,
        ILogger<WebhookWhatsAppService> logger)
    {
        _confirmacaoWhatsAppService = confirmacaoWhatsAppService;
        _confirmacaoWhatsAppEstabelecimentoService = confirmacaoWhatsAppEstabelecimentoService;
        _mensagemNotificacaoService = mensagemNotificacaoService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProcessarMensagemRecebidaAsync(
        JsonElement payload,
        CancellationToken cancellationToken = default)
    {
        var evento = EvolutionWebhookParser.ExtrairEvento(payload) ?? "(nao informado)";
        var instancia = EvolutionWebhookParser.ExtrairInstancia(payload) ?? "(nao informado)";

        LogPayloadBrutoEvolution("messages-upsert", evento, instancia, payload);
        var fromMe = EvolutionWebhookParser.ExtrairFromMe(payload);
        var telefone = EvolutionWebhookParser.ExtrairTelefoneRemetente(payload);
        var textoMensagem = EvolutionWebhookParser.ExtrairTextoMensagem(payload);

        _logger.LogInformation(
            "Webhook WhatsApp messages-upsert recebido. Evento={Evento}, Instancia={Instancia}, FromMe={FromMe}, TelefonePresente={TelefonePresente}, TextoPresente={TextoPresente}",
            evento,
            instancia,
            fromMe,
            !string.IsNullOrWhiteSpace(telefone),
            !string.IsNullOrWhiteSpace(textoMensagem));

        if (!EvolutionWebhookParser.IsMensagemInboundDoUsuario(payload))
        {
            _logger.LogInformation(
                "Webhook WhatsApp messages-upsert ignorado: nao inbound. Evento={Evento}, Instancia={Instancia}, Motivo={Motivo}",
                evento,
                instancia,
                EvolutionWebhookParser.DescreverMotivoNaoInbound(payload));
            return;
        }

        if (!ValidarApiKey(payload))
        {
            _logger.LogWarning(
                "Webhook WhatsApp messages-upsert ignorado: apikey invalida. Evento={Evento}, Instancia={Instancia}",
                evento,
                instancia);
            return;
        }

        await ProcessarConfirmacaoInboundAsync(telefone, textoMensagem, cancellationToken);
    }

    public Task ProcessarMensagemEnviadaAsync(
        JsonElement payload,
        CancellationToken cancellationToken = default)
    {
        var evento = EvolutionWebhookParser.ExtrairEvento(payload) ?? "(nao informado)";
        var instancia = EvolutionWebhookParser.ExtrairInstancia(payload) ?? "(nao informado)";

        LogPayloadBrutoEvolution("send-message", evento, instancia, payload);

        if (!ValidarApiKey(payload))
        {
            _logger.LogWarning("Webhook WhatsApp send-message ignorado: apikey invalida.");
            return Task.CompletedTask;
        }

        var telefone = EvolutionWebhookParser.ExtrairTelefoneRemetente(payload);
        var textoMensagem = EvolutionWebhookParser.ExtrairTextoMensagem(payload);

        _logger.LogInformation(
            "Webhook WhatsApp send-message recebido. Telefone={Telefone}, TextoPresente={TextoPresente}",
            string.IsNullOrWhiteSpace(telefone) ? "(nao identificado)" : telefone,
            !string.IsNullOrWhiteSpace(textoMensagem));

        return Task.CompletedTask;
    }

    private async Task ProcessarConfirmacaoInboundAsync(
        string telefoneRemetente,
        string textoMensagem,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(telefoneRemetente) || string.IsNullOrWhiteSpace(textoMensagem))
        {
            _logger.LogInformation(
                "Webhook WhatsApp messages-upsert sem telefone ou texto. TelefonePresente={TelefonePresente}, TextoPresente={TextoPresente}",
                !string.IsNullOrWhiteSpace(telefoneRemetente),
                !string.IsNullOrWhiteSpace(textoMensagem));
            return;
        }

        var pareceTentativaConfirmacao = ConfirmacaoWhatsAppCodigoHelper.PareceTentativaConfirmacao(textoMensagem);
        if (pareceTentativaConfirmacao)
        {
            await EnviarMensagemProcessandoAsync(telefoneRemetente, cancellationToken);
        }

        var resultadoUsuario = await _confirmacaoWhatsAppService.TentarConfirmarPorMensagemInboundAsync(
            telefoneRemetente,
            textoMensagem,
            cancellationToken);

        if (resultadoUsuario.Confirmado)
        {
            await EnviarRespostaConfirmacaoSucessoAsync(resultadoUsuario, cancellationToken);
            return;
        }

        LogarMotivoIgnorado("usuario", telefoneRemetente, resultadoUsuario.MotivoIgnorado);

        if (resultadoUsuario.MotivoIgnorado == WhatsAppConfirmacaoInboundMotivoIgnorado.JaConfirmado)
        {
            await EnviarRespostaJaConfirmadoAsync(resultadoUsuario, cancellationToken);
            return;
        }

        var resultadoEstabelecimento = await _confirmacaoWhatsAppEstabelecimentoService.TentarConfirmarPorMensagemInboundAsync(
            telefoneRemetente,
            textoMensagem,
            cancellationToken);

        if (resultadoEstabelecimento.Confirmado)
        {
            await EnviarRespostaConfirmacaoSucessoAsync(resultadoEstabelecimento, cancellationToken);
            return;
        }

        LogarMotivoIgnorado("estabelecimento", telefoneRemetente, resultadoEstabelecimento.MotivoIgnorado);

        if (resultadoEstabelecimento.MotivoIgnorado == WhatsAppConfirmacaoInboundMotivoIgnorado.JaConfirmado)
        {
            await EnviarRespostaJaConfirmadoAsync(resultadoEstabelecimento, cancellationToken);
            return;
        }

        if (pareceTentativaConfirmacao)
        {
            var resultadoFalha = SelecionarResultadoFalha(resultadoUsuario, resultadoEstabelecimento);
            await EnviarRespostaConfirmacaoFalhaAsync(resultadoFalha, telefoneRemetente, cancellationToken);
        }
    }

    private static WhatsAppConfirmacaoInboundResultado SelecionarResultadoFalha(
        WhatsAppConfirmacaoInboundResultado resultadoUsuario,
        WhatsAppConfirmacaoInboundResultado resultadoEstabelecimento)
    {
        if (resultadoUsuario.PossuiContatoIdentificado
            && DeveNotificarFalha(resultadoUsuario.MotivoIgnorado))
        {
            return resultadoUsuario;
        }

        if (resultadoEstabelecimento.PossuiContatoIdentificado
            && DeveNotificarFalha(resultadoEstabelecimento.MotivoIgnorado))
        {
            return resultadoEstabelecimento;
        }

        return resultadoUsuario.MotivoIgnorado != WhatsAppConfirmacaoInboundMotivoIgnorado.Nenhum
            ? resultadoUsuario
            : resultadoEstabelecimento;
    }

    private static bool DeveNotificarFalha(WhatsAppConfirmacaoInboundMotivoIgnorado motivo) =>
        motivo is WhatsAppConfirmacaoInboundMotivoIgnorado.CodigoInvalido
            or WhatsAppConfirmacaoInboundMotivoIgnorado.SemPendencia
            or WhatsAppConfirmacaoInboundMotivoIgnorado.EntidadeNaoEncontrada;

    private void LogarMotivoIgnorado(
        string tipo,
        string telefone,
        WhatsAppConfirmacaoInboundMotivoIgnorado motivo)
    {
        if (motivo == WhatsAppConfirmacaoInboundMotivoIgnorado.Nenhum)
        {
            return;
        }

        _logger.LogInformation(
            "Webhook WhatsApp messages-upsert sem confirmacao. Tipo={Tipo}, Telefone={Telefone}, Motivo={Motivo}",
            tipo,
            telefone,
            motivo);
    }

    private void LogPayloadBrutoEvolution(
        string endpoint,
        string evento,
        string instancia,
        JsonElement payload)
    {
        var raw = payload.GetRawText();
        if (raw.Length > PayloadLogMaxLength)
        {
            raw = raw[..PayloadLogMaxLength] + "...(truncado)";
        }

        _logger.LogInformation(
            "Webhook WhatsApp {Endpoint} payload bruto Evolution. Evento={Evento}, Instancia={Instancia}, Payload={Payload}",
            endpoint,
            evento,
            instancia,
            raw);
    }

    private bool ValidarApiKey(JsonElement payload)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookApiKey))
        {
            return true;
        }

        if (payload.TryGetProperty("apikey", out var apikeyPayload)
            && string.Equals(apikeyPayload.GetString(), _options.WebhookApiKey, StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(_options.ApiKey, _options.WebhookApiKey, StringComparison.Ordinal)
            && payload.TryGetProperty("apikey", out var apikey)
            && string.Equals(apikey.GetString(), _options.ApiKey, StringComparison.Ordinal);
    }

    private async Task EnviarMensagemProcessandoAsync(
        string telefoneRemetente,
        CancellationToken cancellationToken)
    {
        var conteudo = ConfirmacaoWhatsAppTemplate.RespostaProcessandoConfirmacaoGenerica();

        await RegistrarWhatsAppAsync(
            telefoneRemetente,
            "Confirmacao WhatsApp em processamento",
            conteudo,
            estabelecimentoId: null,
            prioridade: 3,
            cancellationToken);
    }

    private async Task EnviarRespostaConfirmacaoSucessoAsync(
        WhatsAppConfirmacaoInboundResultado resultado,
        CancellationToken cancellationToken)
    {
        var conteudo = ConfirmacaoWhatsAppTemplate.RespostaConfirmacaoSucesso(resultado.NomeDestinatario);

        await RegistrarWhatsAppAsync(
            resultado.TelefoneResposta,
            "Confirmacao WhatsApp aprovada",
            conteudo,
            resultado.EstabelecimentoId,
            prioridade: 2,
            cancellationToken);

        _logger.LogInformation(
            "WhatsApp confirmado via webhook inbound. Tipo={Tipo}, Telefone={Telefone}",
            resultado.Tipo,
            resultado.TelefoneResposta);
    }

    private async Task EnviarRespostaJaConfirmadoAsync(
        WhatsAppConfirmacaoInboundResultado resultado,
        CancellationToken cancellationToken)
    {
        var conteudo = ConfirmacaoWhatsAppTemplate.RespostaConfirmacaoJaRealizada(resultado.NomeDestinatario);

        await RegistrarWhatsAppAsync(
            ObterTelefoneResposta(resultado),
            "WhatsApp ja confirmado",
            conteudo,
            resultado.EstabelecimentoId,
            prioridade: 2,
            cancellationToken);
    }

    private async Task EnviarRespostaConfirmacaoFalhaAsync(
        WhatsAppConfirmacaoInboundResultado resultado,
        string telefoneRemetente,
        CancellationToken cancellationToken)
    {
        var telefoneResposta = ObterTelefoneResposta(resultado, telefoneRemetente);
        var conteudoWhatsApp = resultado.PossuiContatoIdentificado
            ? ConfirmacaoWhatsAppTemplate.RespostaConfirmacaoFalha(resultado.NomeDestinatario)
            : ConfirmacaoWhatsAppTemplate.RespostaConfirmacaoFalhaGenerica();

        await RegistrarWhatsAppAsync(
            telefoneResposta,
            "Confirmacao WhatsApp nao concluida",
            conteudoWhatsApp,
            resultado.EstabelecimentoId,
            prioridade: 2,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(resultado.EmailDestinatario))
        {
            var conteudoEmail = resultado.PossuiContatoIdentificado
                ? ConfirmacaoWhatsAppEmailTemplate.CriarFalhaConfirmacao(resultado.NomeDestinatario)
                : ConfirmacaoWhatsAppEmailTemplate.CriarFalhaConfirmacaoGenerica();

            await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
            {
                Canal = CanalMensagemNotificacao.Email,
                Destinatario = resultado.EmailDestinatario,
                Assunto = "Nao conseguimos confirmar seu WhatsApp",
                Conteudo = conteudoEmail,
                EstabelecimentoId = resultado.EstabelecimentoId,
                Prioridade = 2
            }, cancellationToken);
        }

        _logger.LogInformation(
            "Confirmacao WhatsApp nao concluida. Motivo={Motivo}, Telefone={Telefone}, EmailEnviado={EmailEnviado}",
            resultado.MotivoIgnorado,
            telefoneResposta,
            !string.IsNullOrWhiteSpace(resultado.EmailDestinatario));
    }

    private async Task RegistrarWhatsAppAsync(
        string destinatario,
        string assunto,
        string conteudo,
        int? estabelecimentoId,
        int prioridade,
        CancellationToken cancellationToken)
    {
        await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.WhatsApp,
            Destinatario = destinatario,
            Assunto = assunto,
            Conteudo = conteudo,
            EstabelecimentoId = estabelecimentoId,
            Prioridade = prioridade
        }, cancellationToken);
    }

    private static string ObterTelefoneResposta(
        WhatsAppConfirmacaoInboundResultado resultado,
        string? fallback = null) =>
        !string.IsNullOrWhiteSpace(resultado.TelefoneResposta)
            ? resultado.TelefoneResposta
            : fallback ?? string.Empty;
}
