using System.Diagnostics;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure.Mensageria;
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

    // IMPLEMENTAÇÃO DA INTERFACE IProvedorMensagem
    public async Task<ResultadoEnvioMensagem> EnviarAsync(
        MensagemNotificacao mensagem,
        CancellationToken cancellationToken = default)
    {
        var swInterface = Stopwatch.StartNew();

        var requestPayloadStub = JsonSerializer.Serialize(new
        {
            mensagem.Destinatario,
            mensagem.Assunto,
            TextoLength = mensagem.Conteudo.Length,
            ApiVersion = _options.UsarApiV2 ? "v2" : "v1"
        });

        if (!_options.Habilitado
            || string.IsNullOrWhiteSpace(_options.ApiUrl)
            || string.IsNullOrWhiteSpace(_options.InstanceName))
        {
            _logger.LogInformation(
                "Stub WhatsApp enviado. MensagemGuid={MensagemGuid}, Destinatario={Destinatario}",
                mensagem.Guid,
                mensagem.Destinatario);

            swInterface.Stop();
            return new ResultadoEnvioMensagem(
                Sucesso: true,
                RequestPayload: requestPayloadStub,
                ResponsePayload: """{"status":"stub_ok"}""",
                RespostaProvedor: "stub-whatsapp",
                MensagemErro: null,
                TempoExecucaoMs: (int)swInterface.ElapsedMilliseconds);
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            swInterface.Stop();
            return new ResultadoEnvioMensagem(
                Sucesso: false,
                RequestPayload: requestPayloadStub,
                ResponsePayload: null,
                RespostaProvedor: null,
                MensagemErro: "Mensageria:WhatsApp:ApiKey nao configurado.",
                TempoExecucaoMs: (int)swInterface.ElapsedMilliseconds);
        }

        try
        {
            var candidatos = EvolutionDestinoHelper.CriarCandidatosDestinoOutboundDeMensagem(
                mensagem.Destinatario,
                mensagem.PayloadJson);

            // LOG TEMPORARIO: Payload que sera enviado para Evolution API
            var logRequestBodyParaEvolution = EvolutionSendTextRequestBuilder.CriarBodyV1(
                candidatos.FirstOrDefault() ?? mensagem.Destinatario,
                mensagem.Conteudo);
            _logger.LogInformation("WhatsApp (Provedor): Payload para Evolution API: {Payload}", logRequestBodyParaEvolution);

            var (sucesso, responseBody, destinatarioUsado, formatoUsado, tempoExecucaoMs) = await EnviarTextoParaCandidatosInternoAsync( // <-- Chama o novo método interno
                candidatos,
                mensagem.Conteudo,
                cancellationToken);

            swInterface.Stop();

            if (!sucesso)
            {
                _logger.LogWarning(
                    "Evolution sendText tentativa falhou. MensagemGuid={MensagemGuid}, Destinatario={Destinatario}, Response={Response}",
                    mensagem.Guid,
                    destinatarioUsado,
                    responseBody);

                return new ResultadoEnvioMensagem(
                    Sucesso: false,
                    RequestPayload: logRequestBodyParaEvolution, // Re-utiliza o payload para log
                    ResponsePayload: responseBody,
                    RespostaProvedor: null,
                    MensagemErro: "Evolution API nao confirmou entrega do texto.",
                    TempoExecucaoMs: tempoExecucaoMs);
            }

            _logger.LogInformation(
                "WhatsApp enviado via Evolution API. MensagemGuid={MensagemGuid}, Formato={Formato}",
                mensagem.Guid,
                formatoUsado);

            return new ResultadoEnvioMensagem(
                Sucesso: true,
                RequestPayload: logRequestBodyParaEvolution, // Re-utiliza o payload para log
                ResponsePayload: responseBody,
                RespostaProvedor: $"evolution-whatsapp-{formatoUsado}",
                MensagemErro: null,
                TempoExecucaoMs: tempoExecucaoMs);
        }
        catch (Exception ex)
        {
            swInterface.Stop();
            _logger.LogWarning(ex, "Falha ao enviar WhatsApp via Evolution API. MensagemGuid={MensagemGuid}", mensagem.Guid);

            return new ResultadoEnvioMensagem(
                Sucesso: false,
                RequestPayload: null,
                ResponsePayload: ex.Message,
                RespostaProvedor: null,
                MensagemErro: ex.Message,
                TempoExecucaoMs: (int)swInterface.ElapsedMilliseconds);
        }
    }

    // Método principal que faz a chamada HTTP real, agora interno
    private async Task<(bool Sucesso, string? ResponseBody, string DestinatarioUsado, string FormatoUsado, int TempoExecucaoMs)> EnviarTextoParaCandidatosInternoAsync(
        IReadOnlyList<string> candidatosDestino,
        string conteudo,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var swInterno = Stopwatch.StartNew();

        if (candidatosDestino.Count == 0 || string.IsNullOrWhiteSpace(conteudo))
        {
            swInterno.Stop();
            return (false, null, string.Empty, "destino-invalido", (int)swInterno.ElapsedMilliseconds);
        }

        var ultimoBody = string.Empty;
        HttpResponseMessage? ultimaResponse = null;
        string ultimoDestino = candidatosDestino.FirstOrDefault() ?? string.Empty;
        const string formato = "v1-textMessage";

        // Sanitiza o conteúdo para prevenir injeção de HTML/Script
        var conteudoSanitizado = System.Text.Encodings.Web.HtmlEncoder.Default.Encode(conteudo);

        foreach (var candidato in candidatosDestino)
        {
            var url = $"{_options.ApiUrl.TrimEnd('/')}/message/sendText/{Uri.EscapeDataString(_options.InstanceName)}";
            var requestBody = EvolutionSendTextRequestBuilder.CriarBodyV1(candidato, conteudoSanitizado); // Usa conteúdo sanitizado

            _logger.LogInformation(
                "Evolution sendText request. Url={Url}, Destinatario={Destinatario}, Formato={Formato}, Body={Body}",
                url,
                candidato,
                "v1-text",
                requestBody);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.TryAddWithoutValidation("apiKey", _options.ApiKey);
            request.Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            ultimoBody = responseBody;
            ultimaResponse = response;
            ultimoDestino = candidato;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Evolution sendText tentativa falhou. StatusCode={StatusCode}, Destinatario={Destinatario}, Formato={Formato}, Response={Response}",
                    (int)response.StatusCode,
                    candidato,
                    formato,
                    responseBody);
                continue;
            }

            swInterno.Stop();
            _logger.LogInformation(
                "Evolution sendText ok. Destinatario={Destinatario}, Formato={Formato}, Response={Response}",
                candidato,
                formato,
                responseBody);

            return (true, responseBody, candidato, formato, (int)swInterno.ElapsedMilliseconds);
        }

        swInterno.Stop();
        var status = ultimaResponse is null ? 0 : (int)ultimaResponse.StatusCode;
        _logger.LogWarning(
            "Evolution sendText esgotou tentativas. UltimoStatus={StatusCode}, Destinatario={Destinatario}, Formato={Formato}, Response={Response}",
            status,
            ultimoDestino,
            formato,
            ultimoBody);

        return (false, ultimoBody, ultimoDestino, formato, (int)swInterno.ElapsedMilliseconds);
    }
}