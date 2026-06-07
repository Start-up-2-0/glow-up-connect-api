using GLOWAPI.Application.DTOs.Auth;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Auth;
using GLOWAPI.Application.Options;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Exceptions.Auth;
using Moq;
using GLOWAPI.Application.DTOs.Mensageria;

namespace GLOWAPI.Tests.Unit;

public class AuthServiceRecuperacaoSenhaTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock = new();
    private readonly Mock<IAuthSessionService> _authSessionMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<ISecurityAuditLogger> _auditLoggerMock = new();
    private readonly Mock<IRecuperacaoSenhaRepository> _recuperacaoRepoMock = new();
    private readonly Mock<IMensagemNotificacaoService> _mensagemMock = new();
    private readonly AuthOptions _authOptions;
    private readonly AuthService _authService;

    public AuthServiceRecuperacaoSenhaTests()
    {
        _authOptions = new AuthOptions
        {
            ConfirmacaoCodigoDigitos = 6,
            CodigoExpiracaoMinutos = 10,
            ResetTokenExpiracaoMinutos = 30,
            MaxTentativasCodigo = 3
        };

        _authService = new AuthService(
            _usuarioRepoMock.Object,
            _authSessionMock.Object,
            _passwordHasherMock.Object,
            _auditLoggerMock.Object,
            _recuperacaoRepoMock.Object,
            _mensagemMock.Object,
            Microsoft.Extensions.Options.Options.Create(_authOptions));
    }

    // ✅ ForgotPassword: deve enviar código para usuário ativo
    [Fact]
    public async Task ForgotPassword_DeveEnviarCodigo_QuandoUsuarioExisteEAtivo()
    {
        var usuario = new Usuario { Id = 1, Email = "teste@email.com", Ativo = true };
        _usuarioRepoMock.Setup(r => r.ObterPorEmailAsync("teste@email.com", default))
            .ReturnsAsync(usuario);
        _passwordHasherMock.Setup(p => p.Hash(It.IsAny<string>())).Returns("hash123");

        await _authService.ForgotPasswordAsync(
            new ForgotPasswordRequestDto { Email = "teste@email.com" },
            null, default);

        _recuperacaoRepoMock.Verify(r => r.AdicionarAsync(It.IsAny<RecuperacaoSenha>(), default), Times.Once);
        _mensagemMock.Verify(m => m.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), default), Times.Once);
    }

    // ✅ ForgotPassword: não deve fazer nada se usuário não existe
    [Fact]
    public async Task ForgotPassword_NaoDeveFazerNada_QuandoUsuarioNaoExiste()
    {
        _usuarioRepoMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>(), default))
            .ReturnsAsync((Usuario?)null);

        await _authService.ForgotPasswordAsync(
            new ForgotPasswordRequestDto { Email = "inexistente@email.com" },
            null, default);

        _recuperacaoRepoMock.Verify(r => r.AdicionarAsync(It.IsAny<RecuperacaoSenha>(), default), Times.Never);
        _mensagemMock.Verify(m => m.RegistrarAsync(It.IsAny<RegistrarMensagemNotificacaoDto>(), default), Times.Never);
    }

    // ✅ VerifyRecoveryCode: deve retornar token quando código é válido
    [Fact]
    public async Task VerifyRecoveryCode_DeveRetornarToken_QuandoCodigoValido()
    {
        var usuario = new Usuario { Id = 1, Email = "teste@email.com" };
        var recuperacao = new RecuperacaoSenha
        {
            UsuarioId = 1,
            CodigoHash = "hash123",
            CodigoExpiraEm = DateTime.UtcNow.AddMinutes(5),
            CodigoTentativas = 0
        };

        _usuarioRepoMock.Setup(r => r.ObterPorEmailAsync("teste@email.com", default))
            .ReturnsAsync(usuario);
        _recuperacaoRepoMock.Setup(r => r.ObterUltimoPorUsuarioAsync(1, default))
            .ReturnsAsync(recuperacao);
        // Configure the password hasher to return the same hash for any input, ensuring the code hash matches
        _passwordHasherMock.Setup(p => p.Hash(It.IsAny<string>())).Returns("hash123");

        var result = await _authService.VerifyRecoveryCodeAsync(
            new VerifyRecoveryCodeRequestDto { Email = "teste@email.com", Codigo = "123456" },
            default);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.ResetToken));
    }

    // ✅ ResetPassword: deve redefinir senha e revogar sessões
    [Fact]
    public async Task ResetPassword_DeveRedefinirSenha_QuandoTokenValido()
    {
        var usuario = new Usuario { Id = 1, Senha = "senha_antiga", Email = "teste@email.com" };
        var recuperacao = new RecuperacaoSenha
        {
            UsuarioId = 1,
            ResetTokenHash = "tokenHash123",
            ResetTokenExpiraEm = DateTime.UtcNow.AddMinutes(10),
            ResetTokenConsumido = false
        };

        _recuperacaoRepoMock.Setup(r => r.ObterPorResetTokenHashAsync("tokenHash123", default))
            .ReturnsAsync(recuperacao);
        _usuarioRepoMock.Setup(r => r.ObterPorIdAsync(1, default))
            .ReturnsAsync(usuario);
        // Ensure token hash matches the stored value
        _passwordHasherMock.Setup(p => p.Hash("token123")).Returns("tokenHash123");
        // Ensure new password hash returns a valid hash
        _passwordHasherMock.Setup(p => p.Hash("novaSenha123")).Returns("novoHash");

        await _authService.ResetPasswordAsync(
            new ResetPasswordRequestDto { ResetToken = "token123", NovaSenha = "novaSenha123" },
            default);

        _usuarioRepoMock.Verify(r => r.Atualizar(It.IsAny<Usuario>()), Times.Once);
        _authSessionMock.Verify(s => s.RevogarTodasSessoesAsync(1, default), Times.Once);
    }
}
