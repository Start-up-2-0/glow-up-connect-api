using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GLOWAPI.Tests.Integration;

public class AuthControllerTests : IClassFixture<GlowApiWebApplicationFactory>
{
    private readonly GlowApiWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthControllerTests(GlowApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_DeveRetornar200_SemAutenticacao()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Usuario_GetSemToken_DeveRetornar401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/usuario/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CadastrarCliente_SemToken_DeveRetornar201ComAtivoFalse()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/usuario", new
        {
            nome = "Novo Usuario",
            email = "novo@email.com",
            telefone = "11999999999",
            senha = "Senha123!"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.False(body.GetProperty("ativo").GetBoolean());
        Assert.Equal("novo@email.com", body.GetProperty("email").GetString());
    }

    [Fact]
    public async Task CadastrarCliente_ComAvatarBase64_DeveRetornarAvatarBase64()
    {
        const string pngDataUri =
            "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/usuario", new
        {
            nome = "Usuario Avatar",
            email = "avatar@email.com",
            telefone = "11988887777",
            senha = "Senha123!",
            avatarBase64 = pngDataUri
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.StartsWith("data:image/png;base64,", body.GetProperty("avatarBase64").GetString());
    }

    [Fact]
    public async Task Login_AposCadastroSemConfirmar_DeveRetornarTokensComFlagRequerConfirmacao()
    {
        const string email = "pendente@email.com";
        const string senha = "Senha123!";

        var client = _factory.CreateClient();
        var cadastro = await client.PostAsJsonAsync("/api/usuario", new
        {
            nome = "Pendente",
            email,
            telefone = "11999999999",
            senha
        });
        Assert.Equal(HttpStatusCode.Created, cadastro.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, senha });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(body.GetProperty("data").GetProperty("requerConfirmacaoEmail").GetBoolean());
        Assert.True(string.IsNullOrWhiteSpace(body.GetProperty("data").GetProperty("token").GetString()));
        Assert.True(login.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.Contains("guc_access", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_DeveRetornarTokens()
    {
        const string email = "login@email.com";
        const string senha = "Senha123!";
        await _factory.SeedUsuarioAsync(email, senha);

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, senha });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(body.GetProperty("success").GetBoolean());
        Assert.True(string.IsNullOrWhiteSpace(body.GetProperty("data").GetProperty("token").GetString()));
        Assert.True(string.IsNullOrWhiteSpace(body.GetProperty("data").GetProperty("refreshToken").GetString()));
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.Contains("guc_access", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(cookies!, c => c.Contains("guc_refresh", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_ComSenhaInvalida_DeveRetornar401()
    {
        const string email = "falha@email.com";
        await _factory.SeedUsuarioAsync(email, "Senha123!");

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "errada" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("INVALID_CREDENTIALS", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Login_ComTentativasExcedidas_DeveRetornar403()
    {
        const string email = "bloqueado@email.com";
        await _factory.SeedUsuarioAsync(email, "Senha123!", tentativas: 5);

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "Senha123!" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("USER_BLOCKED", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task LoginDepois_AcessoAutorizado_DeveRetornar200()
    {
        const string email = "autorizado@email.com";
        const string senha = "Senha123!";
        await _factory.SeedUsuarioAsync(email, senha);

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, senha });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var meResponse = await client.GetAsync("/api/usuario/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    }

    [Fact]
    public async Task Request_ComTokenExpirado_DeveRetornar401()
    {
        var expiredToken = CriarTokenExpirado();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", $"guc_access={expiredToken}");
        var response = await client.GetAsync("/api/usuario/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("TOKEN_EXPIRED", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task LogoutDepois_AcessoNegado_DeveRetornar401()
    {
        const string email = "logout@email.com";
        const string senha = "Senha123!";
        await _factory.SeedUsuarioAsync(email, senha);

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, senha });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var logoutResponse = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        var retryResponse = await client.GetAsync("/api/usuario/me");
        Assert.Equal(HttpStatusCode.Unauthorized, retryResponse.StatusCode);

        var retryBody = await retryResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("UNAUTHORIZED", retryBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task DesativarUsuario_DeveRevogarSessao_DeveRetornar401NoProximoAcesso()
    {
        const string email = "desativar@email.com";
        const string senha = "Senha123!";
        await _factory.SeedUsuarioAsync(email, senha);

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, senha });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/usuario/me")
        {
            Content = JsonContent.Create(new { senha })
        };
        var deleteResponse = await client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var retryResponse = await client.GetAsync("/api/usuario/me");
        Assert.Equal(HttpStatusCode.Unauthorized, retryResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_ComBodyValido_DeveRetornarNovosTokens()
    {
        const string email = "refresh@email.com";
        const string senha = "Senha123!";
        await _factory.SeedUsuarioAsync(email, senha);

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, senha });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new { });
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(string.IsNullOrWhiteSpace(refreshBody.GetProperty("data").GetProperty("token").GetString()));
        Assert.True(string.IsNullOrWhiteSpace(refreshBody.GetProperty("data").GetProperty("refreshToken").GetString()));
        Assert.True(refreshResponse.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.Contains("guc_access", StringComparison.OrdinalIgnoreCase));

        var meResponse = await client.GetAsync("/api/usuario/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_EmailInexistente_DeveRetornar200Generico()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = "naoexiste@email.com"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(body.GetProperty("success").GetBoolean());
        Assert.Contains("cadastrado", body.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ForgotPassword_UsuarioAtivo_DeveEnfileirarMensagem()
    {
        const string email = "recupera-fila@email.com";
        await _factory.SeedUsuarioAsync(email, "Senha123!");

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var usuario = Assert.Single(db.Usuarios, u => u.Email == email);
        Assert.False(string.IsNullOrWhiteSpace(usuario.RecuperacaoTokenHash));
        Assert.False(string.IsNullOrWhiteSpace(usuario.RecuperacaoCodigoHash));
        Assert.NotNull(usuario.RecuperacaoExpiraEm);

        var mensagem = Assert.Single(db.MensagensNotificacao, m => m.Destinatario == email);
        Assert.Equal("Redefina sua senha", mensagem.Assunto);
        Assert.Contains("/resetar-senha?token=", mensagem.Conteudo);
        Assert.Contains("class=\"confirm-code\"", mensagem.Conteudo);
    }

    [Fact]
    public async Task ResetPassword_PorToken_DeveTrocarSenhaEPermitirLogin()
    {
        const string email = "recupera-token@email.com";
        const string senhaAntiga = "Senha123!";
        const string senhaNova = "NovaSenha123!";
        await _factory.SeedUsuarioAsync(email, senhaAntiga);

        var client = _factory.CreateClient();
        var forgot = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);

        string token;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var mensagem = Assert.Single(db.MensagensNotificacao, m => m.Destinatario == email);
            token = ExtrairTokenRecuperacao(mensagem.Conteudo);
        }

        var reset = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token,
            senha = senhaNova,
            confirmarSenha = senhaNova
        });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        var loginAntiga = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = senhaAntiga });
        Assert.Equal(HttpStatusCode.Unauthorized, loginAntiga.StatusCode);

        var loginNova = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = senhaNova });
        Assert.Equal(HttpStatusCode.OK, loginNova.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_PorCodigo_DeveTrocarSenha()
    {
        const string email = "recupera-codigo@email.com";
        const string senhaNova = "NovaSenha123!";
        await _factory.SeedUsuarioAsync(email, "Senha123!");

        var client = _factory.CreateClient();
        var forgot = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);

        string codigo;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var mensagem = Assert.Single(db.MensagensNotificacao, m => m.Destinatario == email);
            codigo = ExtrairCodigoRecuperacao(mensagem.Conteudo);
        }

        var reset = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            codigo,
            senha = senhaNova,
            confirmarSenha = senhaNova
        });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        var loginNova = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = senhaNova });
        Assert.Equal(HttpStatusCode.OK, loginNova.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_TokenInvalido_DeveRetornar400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = "token-inexistente",
            senha = "NovaSenha123!",
            confirmarSenha = "NovaSenha123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("RESET_SENHA_INVALIDO", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ResetPassword_SemTokenNemCodigo_DeveRetornar400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            senha = "NovaSenha123!",
            confirmarSenha = "NovaSenha123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("RESET_SENHA_INVALIDO", body.GetProperty("code").GetString());
    }

    private static string ExtrairTokenRecuperacao(string html)
    {
        var match = Regex.Match(html, @"resetar-senha\?token=([^""&\s<]+)", RegexOptions.IgnoreCase);
        Assert.True(match.Success, "Token de recuperacao nao encontrado no e-mail.");
        return Uri.UnescapeDataString(System.Net.WebUtility.HtmlDecode(match.Groups[1].Value));
    }

    private static string ExtrairCodigoRecuperacao(string html)
    {
        var match = Regex.Match(html, @"class=""confirm-code""[^>]*>\s*(\d{6})", RegexOptions.IgnoreCase);
        Assert.True(match.Success, "Codigo de recuperacao nao encontrado no e-mail.");
        return match.Groups[1].Value;
    }

    private string CriarTokenExpirado()
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IGlowTokenService>();
        var usuario = new Usuario { Id = 1, Email = "expired@email.com", Role = UserRole.Cliente };
        var expiredAt = DateTime.UtcNow.AddMinutes(-30);

        return tokenService.EmitirAccessToken(usuario, 1, expiredAt);
    }
}
