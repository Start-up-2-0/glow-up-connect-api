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
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"status":"sent"}""")
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://evolution.test/") };
        var provedor = CriarProvedor(client, habilitado: true);

        var resultado = await provedor.EnviarAsync(CriarMensagem());

        Assert.True(resultado.Sucesso);
        Assert.Equal(1, handler.CallCount);
        Assert.NotNull(requestCapturado);
        Assert.Equal(HttpMethod.Post, requestCapturado!.Method);
        Assert.Contains("/message/sendText/instancia-teste", requestCapturado.RequestUri!.ToString());

        Assert.Contains("textMessage", bodyCapturado);
        Assert.Contains("\"text\":", bodyCapturado);
        Assert.Contains("Mensagem teste", bodyCapturado);
    }

    [Fact]
    public async Task EnviarAsync_DeveRetornarFalha_QuandoDestinatarioEhLid()
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
        Assert.Contains("400", resultado.MensagemErro);
    }

    private static ProvedorMensagemWhatsApp CriarProvedor(HttpClient client, bool habilitado) =>
        new(
            client,
            Options.Create(new MensageriaWhatsAppOptions
            {
                ApiUrl = "https://evolution.test",
                ApiKey = "api-key-teste",
                InstanceName = "instancia-teste",
                Habilitado = habilitado
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
