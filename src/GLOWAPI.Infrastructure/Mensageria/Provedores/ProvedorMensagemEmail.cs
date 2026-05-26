using System.Diagnostics;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace GLOWAPI.Infrastructure.Mensageria.Provedores;

public class ProvedorMensagemEmail : IProvedorMensagem
{
    private readonly IResend _resend;
    private readonly MensageriaEmailOptions _options;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProvedorMensagemEmail> _logger;

    public ProvedorMensagemEmail(
        IResend resend,
        IOptions<MensageriaEmailOptions> options,
        IConfiguration configuration,
        ILogger<ProvedorMensagemEmail> logger)
    {
        _resend = resend;
        _options = options.Value;
        _configuration = configuration;
        _logger = logger;
    }

    public CanalMensagemNotificacao CanalSuportado => CanalMensagemNotificacao.Email;

    public async Task<ResultadoEnvioMensagem> EnviarAsync(
        MensagemNotificacao mensagem,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sw = Stopwatch.StartNew();

        var requestPayload = JsonSerializer.Serialize(new
        {
            mensagem.Destinatario,
            mensagem.Assunto,
            Provedor = _options.Provedor
        });

        if (!_options.Habilitado)
        {
            sw.Stop();
            return new ResultadoEnvioMensagem(
                Sucesso: false,
                RequestPayload: requestPayload,
                ResponsePayload: null,
                RespostaProvedor: null,
                MensagemErro: "Envio de e-mail desabilitado (Mensageria:Email:Habilitado=false).",
                TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
        }

        if (string.IsNullOrWhiteSpace(_configuration["RESEND_APITOKEN"]))
        {
            sw.Stop();
            return new ResultadoEnvioMensagem(
                Sucesso: false,
                RequestPayload: requestPayload,
                ResponsePayload: null,
                RespostaProvedor: null,
                MensagemErro: "RESEND_APITOKEN nao configurado.",
                TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
        }

        if (string.IsNullOrWhiteSpace(_options.From))
        {
            sw.Stop();
            return new ResultadoEnvioMensagem(
                Sucesso: false,
                RequestPayload: requestPayload,
                ResponsePayload: null,
                RespostaProvedor: null,
                MensagemErro: "Mensageria:Email:From nao configurado.",
                TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
        }

        try
        {
            var emailMessage = new EmailMessage
            {
                From = _options.From,
                Subject = mensagem.Assunto,
                HtmlBody = EmailConteudoHtml.TextoParaHtml(mensagem.Conteudo)
            };
            emailMessage.To.Add(mensagem.Destinatario);

            var idempotencyKey = mensagem.Guid.ToString("N");
            var resposta = await _resend.EmailSendAsync(idempotencyKey, emailMessage, cancellationToken);

            sw.Stop();

            if (!resposta.Success)
            {
                var erro = resposta.Exception?.Message ?? "Falha ao enviar e-mail via Resend.";
                return new ResultadoEnvioMensagem(
                    Sucesso: false,
                    RequestPayload: requestPayload,
                    ResponsePayload: erro,
                    RespostaProvedor: null,
                    MensagemErro: erro,
                    TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
            }

            var emailId = resposta.Content;
            var responsePayload = JsonSerializer.Serialize(new { emailId = emailId.ToString() });

            _logger.LogInformation(
                "E-mail enviado via Resend. MensagemGuid={MensagemGuid}, EmailId={EmailId}",
                mensagem.Guid,
                emailId);

            return new ResultadoEnvioMensagem(
                Sucesso: true,
                RequestPayload: requestPayload,
                ResponsePayload: responsePayload,
                RespostaProvedor: emailId.ToString(),
                MensagemErro: null,
                TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(
                ex,
                "Falha ao enviar e-mail via Resend. MensagemGuid={MensagemGuid}",
                mensagem.Guid);

            return new ResultadoEnvioMensagem(
                Sucesso: false,
                RequestPayload: requestPayload,
                ResponsePayload: ex.Message,
                RespostaProvedor: null,
                MensagemErro: ex.Message,
                TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
        }
    }
}
