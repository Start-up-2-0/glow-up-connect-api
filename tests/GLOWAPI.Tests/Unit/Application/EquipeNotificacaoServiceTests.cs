using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Moq;

namespace GLOWAPI.Tests.Unit.Application;

public class EquipeNotificacaoServiceTests
{
    private readonly Mock<IMensagemNotificacaoService> _mensagemNotificacaoService = new();

    [Fact]
    public async Task UsuarioEquipeConvidadoAsync_DeveEnfileirarEmailComContextoDoNegocio()
    {
        RegistrarMensagemNotificacaoDto? mensagem = null;
        _mensagemNotificacaoService
            .Setup(s => s.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrarMensagemNotificacaoDto, CancellationToken>((dto, _) => mensagem = dto);

        var service = CreateService();

        await service.UsuarioEquipeConvidadoAsync(
            new EstabelecimentoUsuario
            {
                Id = 10,
                EstabelecimentoId = 20,
                UsuarioId = 30,
                RoleNoEstabelecimento = EstablishmentUserRole.Receptionist,
                Ativo = true
            },
            CriarUsuario());

        Assert.NotNull(mensagem);
        Assert.Equal(CanalMensagemNotificacao.Email, mensagem!.Canal);
        Assert.Equal("maria@email.com", mensagem.Destinatario);
        Assert.Equal(20, mensagem.EstabelecimentoId);
        Assert.Equal(2, mensagem.Prioridade);
        Assert.Contains("usuario-equipe-convidado", mensagem.PayloadJson);
        Assert.Contains("Receptionist", mensagem.Conteudo);
    }

    [Fact]
    public async Task ProfissionalConvidadoAsync_DeveEnfileirarEmailParaProfissional()
    {
        RegistrarMensagemNotificacaoDto? mensagem = null;
        _mensagemNotificacaoService
            .Setup(s => s.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrarMensagemNotificacaoDto, CancellationToken>((dto, _) => mensagem = dto);

        var service = CreateService();

        await service.ProfissionalConvidadoAsync(
            new ProfissionalEstabelecimento
            {
                Id = 40,
                EstabelecimentoId = 20,
                ProfissionalId = 50,
                PodeReceberAgendamento = true,
                Ativo = true
            },
            CriarProfissional());

        Assert.NotNull(mensagem);
        Assert.Equal("profissional@email.com", mensagem!.Destinatario);
        Assert.Contains("profissional-convidado", mensagem.PayloadJson);
        Assert.Contains("vinculado como profissional", mensagem.Conteudo);
    }

    [Fact]
    public async Task RoleUsuarioAlteradaAsync_DeveEnfileirarEmailComRoles()
    {
        RegistrarMensagemNotificacaoDto? mensagem = null;
        _mensagemNotificacaoService
            .Setup(s => s.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrarMensagemNotificacaoDto, CancellationToken>((dto, _) => mensagem = dto);

        var service = CreateService();

        await service.RoleUsuarioAlteradaAsync(
            new EstabelecimentoUsuario
            {
                Id = 10,
                EstabelecimentoId = 20,
                UsuarioId = 30,
                RoleNoEstabelecimento = EstablishmentUserRole.Admin
            },
            CriarUsuario(),
            EstablishmentUserRole.Receptionist,
            EstablishmentUserRole.Admin);

        Assert.NotNull(mensagem);
        Assert.Contains("usuario-equipe-role-alterada", mensagem!.PayloadJson);
        Assert.Contains("Receptionist", mensagem.Conteudo);
        Assert.Contains("Admin", mensagem.Conteudo);
    }

    [Fact]
    public async Task StatusUsuarioAlteradoAsync_DeveEnfileirarEmailDeRemocao()
    {
        RegistrarMensagemNotificacaoDto? mensagem = null;
        _mensagemNotificacaoService
            .Setup(s => s.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrarMensagemNotificacaoDto, CancellationToken>((dto, _) => mensagem = dto);

        var service = CreateService();

        await service.StatusUsuarioAlteradoAsync(
            new EstabelecimentoUsuario
            {
                Id = 10,
                EstabelecimentoId = 20,
                UsuarioId = 30,
                RoleNoEstabelecimento = EstablishmentUserRole.Manager
            },
            CriarUsuario(),
            ativoAnterior: true,
            ativoNovo: false);

        Assert.NotNull(mensagem);
        Assert.Contains("usuario-equipe-status-alterado", mensagem!.PayloadJson);
        Assert.Contains("removido", mensagem.Assunto);
    }

    [Fact]
    public async Task StatusProfissionalAlteradoAsync_DeveEnfileirarEmailDeReativacao()
    {
        RegistrarMensagemNotificacaoDto? mensagem = null;
        _mensagemNotificacaoService
            .Setup(s => s.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrarMensagemNotificacaoDto, CancellationToken>((dto, _) => mensagem = dto);

        var service = CreateService();

        await service.StatusProfissionalAlteradoAsync(
            new ProfissionalEstabelecimento
            {
                Id = 40,
                EstabelecimentoId = 20,
                ProfissionalId = 50,
                PodeReceberAgendamento = true
            },
            CriarProfissional(),
            ativoAnterior: false,
            ativoNovo: true);

        Assert.NotNull(mensagem);
        Assert.Contains("profissional-status-alterado", mensagem!.PayloadJson);
        Assert.Contains("reativado", mensagem.Assunto);
    }

    [Fact]
    public async Task Notificacao_DeveIgnorarDestinatarioVazio()
    {
        var service = CreateService();

        await service.ProfissionalConvidadoAsync(
            new ProfissionalEstabelecimento
            {
                EstabelecimentoId = 20,
                ProfissionalId = 50
            },
            new Profissional
            {
                Id = 50,
                NomePublico = "Maria"
            });

        _mensagemNotificacaoService.Verify(
            s => s.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private EquipeNotificacaoService CreateService() =>
        new(_mensagemNotificacaoService.Object);

    private static Usuario CriarUsuario() =>
        new()
        {
            Id = 30,
            Nome = "Maria",
            Email = "maria@email.com",
            Telefone = "11999999999"
        };

    private static Profissional CriarProfissional() =>
        new()
        {
            Id = 50,
            UsuarioId = 30,
            NomePublico = "Maria Beauty",
            Email = "profissional@email.com",
            Telefone = "11999999999"
        };
}
