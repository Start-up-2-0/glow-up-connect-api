using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Infrastructure.Mensageria.Provedores;

public class ProvedorMensagemWhatsApp : IProvedorMensagem
{
    private readonly HttpClient _httpClient;
    private readonly MensageriaWhatsAppOptions _options;
    private readonly ILogger<ProvedorMensagemWhatsApp> _logger;

    public ProvedorMensagemWhatsApp(
        HttpClient httpClient,
        IOptions<MensageriaWhatsAppOptions> options,
        ILogger<ProvedorMensagemWhatsApp> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public CanalMensagemNotificacao CanalSuportado => CanalMensagemNotificacao.WhatsApp;

    public async Task<ResultadoEnvioMensagem> EnviarAsync(
        MensagemNotificacao mensagem,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sw = Stopwatch.StartNew();

        var requestPayload = JsonSerializer.Serialize(new
        {
            mensagem.Destinatario,
            mensagem.Assunto
        });

        if (!_options.Habilitado
            || string.IsNullOrWhiteSpace(_options.ApiUrl)
            || string.IsNullOrWhiteSpace(_options.InstanceName))
        {
            _logger.LogInformation(
                "Stub WhatsApp enviado. MensagemGuid={MensagemGuid}, Destinatario={Destinatario}",
                mensagem.Guid,
                mensagem.Destinatario);

            sw.Stop();
            return new ResultadoEnvioMensagem(
                Sucesso: true,
                RequestPayload: requestPayload,
                ResponsePayload: """{"status":"stub_ok"}""",
                RespostaProvedor: "stub-whatsapp",
                MensagemErro: null,
                TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            sw.Stop();
            return new ResultadoEnvioMensagem(
                Sucesso: false,
                RequestPayload: requestPayload,
                ResponsePayload: null,
                RespostaProvedor: null,
                MensagemErro: "Mensageria:WhatsApp:ApiKey nao configurado.",
                TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
        }

        try
        {
            var url = $"{_options.ApiUrl.TrimEnd('/')}/message/sendText/{Uri.EscapeDataString(_options.InstanceName)}";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apikey", _options.ApiKey);
            request.Content = JsonContent.Create(new
            {
                number = mensagem.Destinatario,
                textMessage = new { text = mensagem.Conteudo }
            });

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                return new ResultadoEnvioMensagem(
                    Sucesso: false,
                    RequestPayload: requestPayload,
                    ResponsePayload: responseBody,
                    RespostaProvedor: null,
                    MensagemErro: $"Evolution API retornou {(int)response.StatusCode}.",
                    TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
            }

            _logger.LogInformation(
                "WhatsApp enviado via Evolution API. MensagemGuid={MensagemGuid}",
                mensagem.Guid);

            return new ResultadoEnvioMensagem(
                Sucesso: true,
                RequestPayload: requestPayload,
                ResponsePayload: responseBody,
                RespostaProvedor: "evolution-whatsapp",
                MensagemErro: null,
                TempoExecucaoMs: (int)sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Falha ao enviar WhatsApp via Evolution API. MensagemGuid={MensagemGuid}", mensagem.Guid);

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
