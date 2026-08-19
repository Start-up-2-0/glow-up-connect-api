using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Options;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class ConfirmacaoWhatsAppEstabelecimentoServiceTests
{
    private readonly Mock<IEstabelecimentoRepository> _estabelecimentoRepository = new();
    private readonly Mock<IAutorizacaoNegocioService> _autorizacaoNegocioService = new();
    private readonly Mock<IGlowTokenService> _tokenService = new();
    private readonly Mock<IMensagemNotificacaoService> _mensagemService = new();

    public ConfirmacaoWhatsAppEstabelecimentoServiceTests()
    {
        _tokenService.Setup(t => t.HashToken(It.IsAny<string>()))
            .Returns<string>(value => $"hash-{value}");
    }

    [Fact]
    public async Task IniciarConfirmacaoAsync_DeveEnviarDoisEmails_QuandoDestinosDiferem()
    {
        var estabelecimento = new Estabelecimento
        {
            Id = 40,
            Nome = "Studio Glow",
            Telefone = "11977776666",
            Email = "comercial@studio.com"
        };

        var mensagens = new List<RegistrarMensagemNotificacaoDto>();
        _mensagemService
            .Setup(m => m.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrarMensagemNotificacaoDto, CancellationToken>((dto, _) => mensagens.Add(dto));

        var service = CreateService();
        var instrucoes = await service.IniciarConfirmacaoAsync(
            estabelecimento,
            ["owner@email.com", "comercial@studio.com"]);

        Assert.True(instrucoes.EmailEnviado);
        Assert.True(instrucoes.WhatsAppEnviado);
        Assert.Equal(3, mensagens.Count);
        Assert.Equal(1, mensagens.Count(dto => dto.Canal == CanalMensagemNotificacao.WhatsApp));
        Assert.Equal(2, mensagens.Count(dto => dto.Canal == CanalMensagemNotificacao.Email));
    }

    [Fact]
    public async Task IniciarConfirmacaoAsync_DeveEnviarUmEmail_QuandoDestinosIguais()
    {
        var estabelecimento = new Estabelecimento
        {
            Id = 40,
            Nome = "Studio Glow",
            Telefone = "11977776666",
            Email = "owner@email.com"
        };

        var mensagens = new List<RegistrarMensagemNotificacaoDto>();
        _mensagemService
            .Setup(m => m.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrarMensagemNotificacaoDto, CancellationToken>((dto, _) => mensagens.Add(dto));

        var service = CreateService();
        await service.IniciarConfirmacaoAsync(
            estabelecimento,
            ["owner@email.com", "OWNER@email.com"]);

        Assert.Equal(2, mensagens.Count);
        Assert.Single(mensagens, dto => dto.Canal == CanalMensagemNotificacao.WhatsApp);
        Assert.Single(mensagens, dto => dto.Canal == CanalMensagemNotificacao.Email);
    }

    private ConfirmacaoWhatsAppEstabelecimentoService CreateService() =>
        new(
            _estabelecimentoRepository.Object,
            _autorizacaoNegocioService.Object,
            _tokenService.Object,
            _mensagemService.Object,
            Options.Create(new MensageriaWhatsAppOptions { NumeroPlataforma = "5511999999999" }),
            Options.Create(new AuthOptions
            {
                FrontendBaseUrl = "http://localhost:3000",
                ConfirmacaoWhatsAppHoras = 24,
                ConfirmacaoCodigoDigitos = 6
            }));
}
