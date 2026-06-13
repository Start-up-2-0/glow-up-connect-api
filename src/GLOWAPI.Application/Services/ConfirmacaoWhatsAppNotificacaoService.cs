using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Mensageria;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GLOWAPI.Application.Services;

public class ConfirmacaoWhatsAppNotificacaoService : IConfirmacaoWhatsAppNotificacaoService
{
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;
    private readonly ILogger<ConfirmacaoWhatsAppNotificacaoService> _logger;

    public ConfirmacaoWhatsAppNotificacaoService(
        IMensagemNotificacaoService mensagemNotificacaoService,
        ILogger<ConfirmacaoWhatsAppNotificacaoService> logger)
    {
        _mensagemNotificacaoService = mensagemNotificacaoService;
        _logger = logger;
    }

    public Task EnfileirarRespostaProcessandoAsync(
        string telefoneDestino,
        string? nomeDestinatario,
        ConfirmacaoWhatsAppInboundContexto? contextoConversa,
        CancellationToken cancellationToken = default)
    {
        var conteudo = string.IsNullOrWhiteSpace(nomeDestinatario)
            ? ConfirmacaoWhatsAppTemplate.RespostaProcessandoConfirmacaoGenerica()
            : ConfirmacaoWhatsAppTemplate.RespostaProcessandoConfirmacao(nomeDestinatario);

        return EnfileirarWhatsAppAsync(
            telefoneDestino,
            "Confirmacao WhatsApp em processamento",
            conteudo,
            estabelecimentoId: null,
            prioridade: 3,
            cancellationToken);
    }

    public async Task EnfileirarRespostaConfirmacaoSucessoAsync(
        WhatsAppConfirmacaoInboundResultado resultado,
        ConfirmacaoWhatsAppInboundContexto? contextoConversa,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resultado.EmailDestinatario))
        {
            _logger.LogWarning(
                "Confirmacao WhatsApp aprovada sem e-mail cadastrado para notificacao. Tipo={Tipo}, UsuarioId={UsuarioId}, EstabelecimentoId={EstabelecimentoId}, Telefone={Telefone}",
                resultado.Tipo,
                resultado.UsuarioId,
                resultado.EstabelecimentoId,
                resultado.TelefoneResposta);
            return;
        }

        var conteudo = ConfirmacaoWhatsAppEmailTemplate.CriarConfirmacaoSucesso(
            resultado.NomeDestinatario,
            resultado.TelefoneResposta);

        _logger.LogInformation(
            "Enfileirando confirmacao WhatsApp aprovada por e-mail. Tipo={Tipo}, UsuarioId={UsuarioId}, EstabelecimentoId={EstabelecimentoId}, Email={Email}, Telefone={Telefone}",
            resultado.Tipo,
            resultado.UsuarioId,
            resultado.EstabelecimentoId,
            resultado.EmailDestinatario,
            resultado.TelefoneResposta);

        await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.Email,
            Destinatario = resultado.EmailDestinatario,
            Assunto = "WhatsApp confirmado no Glow Up Connect",
            Conteudo = conteudo,
            EstabelecimentoId = resultado.EstabelecimentoId,
            Prioridade = 2
        }, cancellationToken);
    }

    public async Task EnfileirarRespostaJaConfirmadoAsync(
        WhatsAppConfirmacaoInboundResultado resultado,
        ConfirmacaoWhatsAppInboundContexto? contextoConversa,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resultado.EmailDestinatario))
        {
            _logger.LogWarning(
                "WhatsApp ja confirmado sem e-mail cadastrado para notificacao. Tipo={Tipo}, UsuarioId={UsuarioId}, EstabelecimentoId={EstabelecimentoId}, Telefone={Telefone}",
                resultado.Tipo,
                resultado.UsuarioId,
                resultado.EstabelecimentoId,
                resultado.TelefoneResposta);
            return;
        }

        var conteudo = ConfirmacaoWhatsAppEmailTemplate.CriarConfirmacaoJaRealizada(resultado.NomeDestinatario);

        await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.Email,
            Destinatario = resultado.EmailDestinatario,
            Assunto = "WhatsApp ja confirmado no Glow Up Connect",
            Conteudo = conteudo,
            EstabelecimentoId = resultado.EstabelecimentoId,
            Prioridade = 2
        }, cancellationToken);
    }

    public async Task EnfileirarRespostaFalhaAsync(
        WhatsAppConfirmacaoInboundResultado resultado,
        string? telefoneFallback,
        ConfirmacaoWhatsAppInboundContexto? contextoConversa,
        CancellationToken cancellationToken = default)
    {
        var telefoneResposta = ObterTelefoneResposta(resultado, telefoneFallback);
        var conteudoWhatsApp = resultado.PossuiContatoIdentificado
            ? ConfirmacaoWhatsAppTemplate.RespostaConfirmacaoFalha(resultado.NomeDestinatario)
            : ConfirmacaoWhatsAppTemplate.RespostaConfirmacaoFalhaGenerica();

        if (!string.IsNullOrWhiteSpace(telefoneResposta))
        {
            await EnfileirarWhatsAppAsync(
                telefoneResposta,
                "Confirmacao WhatsApp nao concluida",
                conteudoWhatsApp,
                resultado.EstabelecimentoId,
                prioridade: 2,
                cancellationToken);
        }

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
            "Confirmacao WhatsApp nao concluida; notificacoes enfileiradas. Motivo={Motivo}, Telefone={Telefone}, EmailEnviado={EmailEnviado}",
            resultado.MotivoIgnorado,
            telefoneResposta,
            !string.IsNullOrWhiteSpace(resultado.EmailDestinatario));
    }

    private async Task EnfileirarWhatsAppAsync(
        string telefoneDestino,
        string assunto,
        string conteudo,
        int? estabelecimentoId,
        int prioridade,
        CancellationToken cancellationToken)
    {
        var telefoneCadastrado = TelefoneHelper.NormalizarParaWhatsApp(telefoneDestino) ?? telefoneDestino;

        if (string.IsNullOrWhiteSpace(telefoneCadastrado) || string.IsNullOrWhiteSpace(conteudo))
        {
            _logger.LogWarning(
                "WhatsApp outbound ignorado: destinatario ou conteudo ausente. Assunto={Assunto}, TelefoneCadastradoPresente={TelefoneCadastradoPresente}, ConteudoPresente={ConteudoPresente}",
                assunto,
                !string.IsNullOrWhiteSpace(telefoneCadastrado),
                !string.IsNullOrWhiteSpace(conteudo));
            return;
        }

        await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.WhatsApp,
            Destinatario = telefoneCadastrado,
            Assunto = assunto,
            Conteudo = conteudo,
            EstabelecimentoId = estabelecimentoId,
            Prioridade = prioridade
        }, cancellationToken);

        _logger.LogInformation(
            "WhatsApp enfileirado para envio assincrono. Assunto={Assunto}, TelefoneCadastrado={TelefoneCadastrado}",
            assunto,
            telefoneCadastrado);
    }

    private static string ObterTelefoneResposta(
        WhatsAppConfirmacaoInboundResultado resultado,
        string? fallback = null) =>
        !string.IsNullOrWhiteSpace(resultado.TelefoneResposta)
            ? resultado.TelefoneResposta
            : fallback ?? string.Empty;
}
