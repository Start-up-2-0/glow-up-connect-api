using System.Net;
using FluentAssertions;
using GLOWAPI.Application.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure.Mensageria.Provedores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Resend;

namespace GLOWAPI.Tests.Unit.Infrastructure;

public class ProvedorMensagemEmailTests
{
    private static MensagemNotificacao CriarMensagem() => new()
    {
        Guid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
        Canal = CanalMensagemNotificacao.Email,
        Destinatario = "cliente@example.com",
        Assunto = "Confirmacao",
        Conteudo = "Codigo: 123456\nLink: https://app.test/confirmar"
    };

    private static IConfiguration CriarConfiguration(string? apiToken = "re_test_token")
    {
        var dados = new Dictionary<string, string?>();
        if (apiToken is not null)
        {
            dados["RESEND_APITOKEN"] = apiToken;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(dados)
            .Build();
    }

    private static ProvedorMensagemEmail CriarProvedor(
        Mock<IResend> resend,
        MensageriaEmailOptions options,
        IConfiguration? configuration = null)
    {
        return new ProvedorMensagemEmail(
            resend.Object,
            Options.Create(options),
            configuration ?? CriarConfiguration(),
            NullLogger<ProvedorMensagemEmail>.Instance);
    }

    [Fact]
    public async Task EnviarAsync_DeveRetornarSucessoComId_QuandoResendOk()
    {
        var emailId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var resend = new Mock<IResend>();
        resend
            .Setup(r => r.EmailSendAsync(
                It.IsAny<string>(),
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResendResponse<Guid>(emailId, null!));

        var provedor = CriarProvedor(resend, new MensageriaEmailOptions
        {
            Habilitado = true,
            From = "Glow Up Connect <noreply@example.com>",
            Provedor = "resend"
        });

        var resultado = await provedor.EnviarAsync(CriarMensagem());

        resultado.Sucesso.Should().BeTrue();
        resultado.RespostaProvedor.Should().Be(emailId.ToString());
        resultado.MensagemErro.Should().BeNull();

        resend.Verify(
            r => r.EmailSendAsync(
                "aaaaaaaabbbbccccddddeeeeeeeeeeee",
                It.Is<EmailMessage>(m =>
                    m.From.Email == "noreply@example.com"
                    && m.Subject == "Confirmacao"
                    && m.To.Any(t => t.Email == "cliente@example.com")
                    && m.HtmlBody!.Contains("123456")
                    && m.HtmlBody.Contains("<br/>")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnviarAsync_DeveAnexarImagensInline_QuandoHtmlReferenciaCid()
    {
        var html = ConfirmacaoEmailTemplate.Criar(
            "Maria",
            "https://app.test/confirmar-email?token=abc",
            "123456",
            24);

        var mensagem = new MensagemNotificacao
        {
            Guid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Canal = CanalMensagemNotificacao.Email,
            Destinatario = "cliente@example.com",
            Assunto = "Confirmacao",
            Conteudo = html
        };

        var emailId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var resend = new Mock<IResend>();
        EmailMessage? mensagemEnviada = null;
        resend
            .Setup(r => r.EmailSendAsync(
                It.IsAny<string>(),
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, EmailMessage, CancellationToken>((_, message, _) => mensagemEnviada = message)
            .ReturnsAsync(new ResendResponse<Guid>(emailId, null!));

        var provedor = CriarProvedor(resend, new MensageriaEmailOptions
        {
            Habilitado = true,
            From = "noreply@example.com",
            Provedor = "resend"
        });

        var resultado = await provedor.EnviarAsync(mensagem);

        resultado.Sucesso.Should().BeTrue();
        mensagemEnviada.Should().NotBeNull();
        mensagemEnviada!.Attachments.Should().NotBeNull();
        mensagemEnviada.Attachments!.Should().HaveCount(3);
        mensagemEnviada.Attachments.Should().Contain(a =>
            a.ContentId == EmailTemplateInlineAssets.LogoContentId
            && a.Filename == "email-logo-sm.png");
    }

    [Fact]
    public async Task EnviarAsync_DeveRetornarFalha_QuandoResendRetornaErro()
    {
        var resend = new Mock<IResend>();
        resend
            .Setup(r => r.EmailSendAsync(
                It.IsAny<string>(),
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResendResponse<Guid>(
                new ResendException(HttpStatusCode.BadRequest, ErrorType.InvalidFromAddress, "invalid from", null!),
                null!));

        var provedor = CriarProvedor(resend, new MensageriaEmailOptions
        {
            Habilitado = true,
            From = "noreply@example.com"
        });

        var resultado = await provedor.EnviarAsync(CriarMensagem());

        resultado.Sucesso.Should().BeFalse();
        resultado.MensagemErro.Should().Contain("invalid from");
    }

    [Fact]
    public async Task EnviarAsync_DeveRetornarFalha_QuandoResendLancaExcecao()
    {
        var resend = new Mock<IResend>();
        resend
            .Setup(r => r.EmailSendAsync(
                It.IsAny<string>(),
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Resend API error"));

        var provedor = CriarProvedor(resend, new MensageriaEmailOptions
        {
            Habilitado = true,
            From = "noreply@example.com"
        });

        var resultado = await provedor.EnviarAsync(CriarMensagem());

        resultado.Sucesso.Should().BeFalse();
        resultado.MensagemErro.Should().Contain("Resend API error");
    }

    [Fact]
    public async Task EnviarAsync_DeveRetornarFalha_QuandoHabilitadoFalse()
    {
        var resend = new Mock<IResend>();
        var provedor = CriarProvedor(resend, new MensageriaEmailOptions { Habilitado = false });

        var resultado = await provedor.EnviarAsync(CriarMensagem());

        resultado.Sucesso.Should().BeFalse();
        resultado.MensagemErro.Should().Contain("Habilitado=false");
        resend.Verify(
            r => r.EmailSendAsync(
                It.IsAny<string>(),
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EnviarAsync_DeveRetornarFalha_QuandoTokenAusente()
    {
        var resend = new Mock<IResend>();
        var provedor = CriarProvedor(
            resend,
            new MensageriaEmailOptions { Habilitado = true, From = "noreply@example.com" },
            CriarConfiguration(apiToken: null));

        var resultado = await provedor.EnviarAsync(CriarMensagem());

        resultado.Sucesso.Should().BeFalse();
        resultado.MensagemErro.Should().Contain("RESEND_APITOKEN");
        resend.Verify(
            r => r.EmailSendAsync(
                It.IsAny<string>(),
                It.IsAny<EmailMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EnviarAsync_DeveRetornarFalha_QuandoFromAusente()
    {
        var resend = new Mock<IResend>();
        var provedor = CriarProvedor(resend, new MensageriaEmailOptions
        {
            Habilitado = true,
            From = string.Empty
        });

        var resultado = await provedor.EnviarAsync(CriarMensagem());

        resultado.Sucesso.Should().BeFalse();
        resultado.MensagemErro.Should().Contain("From");
    }
}
