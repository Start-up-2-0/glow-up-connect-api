using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Mensageria;
using Microsoft.Extensions.Logging;

namespace GLOWAPI.Application.Services;

public class ConfirmacaoWhatsAppInboundService : IConfirmacaoWhatsAppInboundService
{
    private readonly IConfirmacaoWhatsAppService _confirmacaoWhatsAppService;
    private readonly IConfirmacaoWhatsAppEstabelecimentoService _confirmacaoWhatsAppEstabelecimentoService;
    private readonly IConfirmacaoWhatsAppNotificacaoService _notificacaoService;
    private readonly ILogger<ConfirmacaoWhatsAppInboundService> _logger;

    public ConfirmacaoWhatsAppInboundService(
        IConfirmacaoWhatsAppService confirmacaoWhatsAppService,
        IConfirmacaoWhatsAppEstabelecimentoService confirmacaoWhatsAppEstabelecimentoService,
        IConfirmacaoWhatsAppNotificacaoService notificacaoService,
        ILogger<ConfirmacaoWhatsAppInboundService> logger)
    {
        _confirmacaoWhatsAppService = confirmacaoWhatsAppService;
        _confirmacaoWhatsAppEstabelecimentoService = confirmacaoWhatsAppEstabelecimentoService;
        _notificacaoService = notificacaoService;
        _logger = logger;
    }

    public async Task ProcessarAsync(
        string telefoneRemetente,
        string textoMensagem,
        ConfirmacaoWhatsAppInboundContexto? contextoConversa,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(textoMensagem))
        {
            _logger.LogInformation(
                "Confirmacao WhatsApp inbound ignorada: texto ausente. TelefonePresente={TelefonePresente}",
                !string.IsNullOrWhiteSpace(telefoneRemetente));
            return;
        }

        if (!ConfirmacaoWhatsAppCodigoHelper.PareceTentativaConfirmacao(textoMensagem))
        {
            _logger.LogInformation(
                "Confirmacao WhatsApp inbound ignorada: mensagem sem token de confirmacao. TelefonePresente={TelefonePresente}",
                !string.IsNullOrWhiteSpace(telefoneRemetente));
            return;
        }

        var destinoResposta = await ResolverDestinoRespostaInboundAsync(
            telefoneRemetente,
            textoMensagem,
            cancellationToken);

        if (destinoResposta is not null && !string.IsNullOrWhiteSpace(destinoResposta.Telefone))
        {
            LogarDestinoRespostaAutomatica(telefoneRemetente, destinoResposta);

            await _notificacaoService.EnfileirarRespostaProcessandoAsync(
                destinoResposta.Telefone,
                destinoResposta.Nome,
                contextoConversa,
                cancellationToken);
        }

        var resultadoUsuario = await _confirmacaoWhatsAppService.TentarConfirmarPorMensagemInboundAsync(
            telefoneRemetente,
            textoMensagem,
            cancellationToken);

        if (resultadoUsuario.Confirmado)
        {
            await _notificacaoService.EnfileirarRespostaConfirmacaoSucessoAsync(
                resultadoUsuario,
                contextoConversa,
                cancellationToken);

            _logger.LogInformation(
                "WhatsApp confirmado via inbound. Tipo={Tipo}, UsuarioId={UsuarioId}, EstabelecimentoId={EstabelecimentoId}, Destinatario={Destinatario}",
                resultadoUsuario.Tipo,
                resultadoUsuario.UsuarioId,
                resultadoUsuario.EstabelecimentoId,
                resultadoUsuario.TelefoneResposta);
            return;
        }

        LogarMotivoIgnorado("usuario", telefoneRemetente, resultadoUsuario.MotivoIgnorado);

        if (resultadoUsuario.MotivoIgnorado == WhatsAppConfirmacaoInboundMotivoIgnorado.JaConfirmado)
        {
            await _notificacaoService.EnfileirarRespostaJaConfirmadoAsync(
                resultadoUsuario,
                contextoConversa,
                cancellationToken);
            return;
        }

        var resultadoEstabelecimento = await _confirmacaoWhatsAppEstabelecimentoService.TentarConfirmarPorMensagemInboundAsync(
            telefoneRemetente,
            textoMensagem,
            cancellationToken);

        if (resultadoEstabelecimento.Confirmado)
        {
            await _notificacaoService.EnfileirarRespostaConfirmacaoSucessoAsync(
                resultadoEstabelecimento,
                contextoConversa,
                cancellationToken);

            _logger.LogInformation(
                "WhatsApp confirmado via inbound. Tipo={Tipo}, UsuarioId={UsuarioId}, EstabelecimentoId={EstabelecimentoId}, Destinatario={Destinatario}",
                resultadoEstabelecimento.Tipo,
                resultadoEstabelecimento.UsuarioId,
                resultadoEstabelecimento.EstabelecimentoId,
                resultadoEstabelecimento.TelefoneResposta);
            return;
        }

        LogarMotivoIgnorado("estabelecimento", telefoneRemetente, resultadoEstabelecimento.MotivoIgnorado);

        if (resultadoEstabelecimento.MotivoIgnorado == WhatsAppConfirmacaoInboundMotivoIgnorado.JaConfirmado)
        {
            await _notificacaoService.EnfileirarRespostaJaConfirmadoAsync(
                resultadoEstabelecimento,
                contextoConversa,
                cancellationToken);
            return;
        }

        var resultadoFalha = SelecionarResultadoFalha(resultadoUsuario, resultadoEstabelecimento);
        await _notificacaoService.EnfileirarRespostaFalhaAsync(
            resultadoFalha,
            destinoResposta?.Telefone,
            contextoConversa,
            cancellationToken);
    }

    private async Task<WhatsAppConfirmacaoInboundRespostaDestino?> ResolverDestinoRespostaInboundAsync(
        string telefoneRemetente,
        string textoMensagem,
        CancellationToken cancellationToken)
    {
        var destinoUsuario = await _confirmacaoWhatsAppService.ResolverDestinoRespostaInboundAsync(
            telefoneRemetente,
            textoMensagem,
            cancellationToken);

        if (destinoUsuario is not null)
        {
            return destinoUsuario;
        }

        return await _confirmacaoWhatsAppEstabelecimentoService.ResolverDestinoRespostaInboundAsync(
            telefoneRemetente,
            textoMensagem,
            cancellationToken);
    }

    private void LogarDestinoRespostaAutomatica(
        string telefoneWebhook,
        WhatsAppConfirmacaoInboundRespostaDestino destino)
    {
        var telefoneWebhookNormalizado = TelefoneHelper.NormalizarParaWhatsApp(telefoneWebhook);
        if (string.IsNullOrWhiteSpace(telefoneWebhookNormalizado))
        {
            _logger.LogInformation(
                "Confirmacao WhatsApp inbound: resposta via telefone cadastrado do codigo. UsuarioId={UsuarioId}, EstabelecimentoId={EstabelecimentoId}, Nome={Nome}, Telefone={Telefone}",
                destino.UsuarioId,
                destino.EstabelecimentoId,
                destino.Nome ?? "(nao informado)",
                destino.Telefone);
            return;
        }

        if (!TelefoneHelper.SaoEquivalentes(telefoneWebhookNormalizado, destino.Telefone))
        {
            _logger.LogInformation(
                "Confirmacao WhatsApp inbound: resposta via telefone cadastrado do codigo (telefone do payload ignorado). UsuarioId={UsuarioId}, EstabelecimentoId={EstabelecimentoId}, TelefonePayload={TelefonePayload}, Nome={Nome}, TelefoneDestino={TelefoneDestino}",
                destino.UsuarioId,
                destino.EstabelecimentoId,
                telefoneWebhookNormalizado,
                destino.Nome ?? "(nao informado)",
                destino.Telefone);
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
            "Confirmacao WhatsApp inbound sem confirmacao. Tipo={Tipo}, Telefone={Telefone}, Motivo={Motivo}",
            tipo,
            telefone,
            motivo);
    }
}
