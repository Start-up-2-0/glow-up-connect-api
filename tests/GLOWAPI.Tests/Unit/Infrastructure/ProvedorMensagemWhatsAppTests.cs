using System.Net;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure.Mensageria.Provedores;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Tests.Unit.Infrastructure;

public class ProvedorMensagemWhatsAppTests
{
    [Fact]
    public async Task EnviarAsync_DeveRetornarStub_QuandoDesabilitado()
    {
        var handler = new RecordingHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new HttpClient(handler);
        var provedor = CriarProvedor(client, habilitado: false);

        var resultado = await provedor.EnviarAsync(CriarMensagem());

        Assert.True(resultado.Sucesso);
        Assert.Equal("stub-whatsapp", resultado.RespostaProvedor);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task EnviarAsync_DeveChamarEvolution_QuandoHabilitado()
    {
        HttpRequestMessage? requestCapturado = null;
        string? bodyCapturado = null;
        var handler = new RecordingHandler(async message =>
        {
            requestCapturado = message;
            bodyCapturado = message.Content is null
                ? null
                : await message.Content.ReadAsStringAsync();
            return OkComTexto("Mensagem teste");
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://evolution.test/") };
        var provedor = CriarProvedor(client, habilitado: true);

        var resultado = await provedor.EnviarAsync(CriarMensagem());

        Assert.True(resultado.Sucesso);
        Assert.Equal(1, handler.CallCount);
        Assert.NotNull(requestCapturado);
        Assert.Equal(HttpMethod.Post, requestCapturado!.Method);
        Assert.Contains("/message/sendText/instancia-teste", requestCapturado.RequestUri!.ToString());

        Assert.NotNull(bodyCapturado);
        Assert.Contains("Mensagem teste", bodyCapturado);
        Assert.Contains("textMessage", bodyCapturado);
        Assert.DoesNotContain("\"text\":\"Mensagem teste\"", bodyCapturado!.Replace(" ", string.Empty));
    }

    [Fact]
    public async Task EnviarAsync_DeveUsarPayloadV2_QuandoConfigurado()
    {
        string? bodyCapturado = null;
        var handler = new RecordingHandler(async message =>
        {
            bodyCapturado = message.Content is null
                ? null
                : await message.Content.ReadAsStringAsync();
            return OkComTexto("Mensagem teste");
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://evolution.test/") };
        var provedor = CriarProvedor(client, habilitado: true, usarApiV2: true);

        var resultado = await provedor.EnviarAsync(CriarMensagem());

        Assert.True(resultado.Sucesso);
        Assert.NotNull(bodyCapturado);
        Assert.Contains("Mensagem teste", bodyCapturado);
        Assert.DoesNotContain("textMessage", bodyCapturado);
    }

    [Fact]
    public async Task EnviarAsync_DeveRetornarFalha_QuandoDestinatarioEhLidSemFallback()
    {
        var handler = new RecordingHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://evolution.test/") };
        var provedor = CriarProvedor(client, habilitado: true);

        var mensagem = CriarMensagem();
        mensagem.Destinatario = "60348602310753@lid";

        var resultado = await provedor.EnviarAsync(mensagem);

        Assert.False(resultado.Sucesso);
        Assert.Contains("invalido", resultado.MensagemErro!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task EnviarAsync_DeveUsarTelefoneFallback_QuandoDestinatarioLegadoEhLid()
    {
        string? bodyCapturado = null;
        var handler = new RecordingHandler(async message =>
        {
            bodyCapturado = message.Content is null
                ? null
                : await message.Content.ReadAsStringAsync();
            return OkComTexto("Mensagem teste");
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://evolution.test/") };
        var provedor = CriarProvedor(client, habilitado: true);

        var mensagem = CriarMensagem();
        mensagem.Destinatario = "60348602310753@lid";
        mensagem.PayloadJson = """{"telefoneFallback":"5579998755111","remoteJidConversa":"60348602310753@lid"}""";

        var resultado = await provedor.EnviarAsync(mensagem);

        Assert.True(resultado.Sucesso);
        Assert.Equal(1, handler.CallCount);
        Assert.NotNull(bodyCapturado);
        Assert.Contains("5579998755111", bodyCapturado);
        Assert.DoesNotContain("@lid", bodyCapturado);
    }

    [Fact]
    public async Task EnviarAsync_DeveRetornarFalha_QuandoEvolutionRetornaErro()
    {
        var handler = new RecordingHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("invalid number")
        }));

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://evolution.test/") };
        var provedor = CriarProvedor(client, habilitado: true);

        var resultado = await provedor.EnviarAsync(CriarMensagem());

        Assert.False(resultado.Sucesso);
        Assert.Contains("nao confirmou", resultado.MensagemErro!, StringComparison.OrdinalIgnoreCase);
    }

    private static HttpResponseMessage OkComTexto(string texto) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"message\":{\"extendedTextMessage\":{\"text\":\"" + texto + "\"}}}")
        };

    private static ProvedorMensagemWhatsApp CriarProvedor(HttpClient client, bool habilitado, bool usarApiV2 = false) =>
        new(
            client,
            Options.Create(new MensageriaWhatsAppOptions
            {
                ApiUrl = "https://evolution.test",
                ApiKey = "api-key-teste",
                InstanceName = "instancia-teste",
                Habilitado = habilitado,
                UsarApiV2 = usarApiV2
            }),
            NullLogger<ProvedorMensagemWhatsApp>.Instance);

    private static MensagemNotificacao CriarMensagem() =>
        new()
        {
            Guid = Guid.NewGuid(),
            Destinatario = "5511999999999",
            Conteudo = "Mensagem teste",
            Assunto = "Teste",
            Canal = CanalMensagemNotificacao.WhatsApp
        };

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _factory;

        public RecordingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> factory) =>
            _factory = factory;

        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return await _factory(request);
        }
    }
}
