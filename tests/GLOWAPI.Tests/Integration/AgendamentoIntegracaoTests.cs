using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GLOWAPI.Tests.Integration;

public class AgendamentoIntegracaoTests : IClassFixture<GlowApiWebApplicationFactory>
{
    private readonly GlowApiWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AgendamentoIntegracaoTests(GlowApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Visitante_DeveCriarAgendamentoPublicoComSucesso()
    {
        var seed = await SeedAgendamentoAsync();
        await ConfigurarHorariosAsync(seed);
        var segunda = ObterProximaSegunda();

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/api/publico/agendar/loja/{seed.PublicGuid}",
            new
            {
                profissionalPublicGuid = seed.ProfissionalPublicGuid,
                servicoIds = new[] { seed.ServicoId },
                data = segunda.ToString("yyyy-MM-dd"),
                horarioInicio = "10:00:00",
                clienteNome = "Cliente Visitante",
                clienteEmail = "visitante@email.com",
                clienteTelefone = "11988887777",
                observacao = "Primeiro agendamento"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("PendenteConfirmacao", body.GetProperty("data").GetProperty("status").GetString());
        Assert.True(body.GetProperty("data").GetProperty("valorTotal").GetDecimal() > 0);
    }

    [Fact]
    public async Task Visitante_DeveRetornarBadRequest_SemTelefone()
    {
        var seed = await SeedAgendamentoAsync();
        await ConfigurarHorariosAsync(seed);
        var segunda = ObterProximaSegunda();

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/api/publico/agendar/loja/{seed.PublicGuid}",
            new
            {
                profissionalPublicGuid = seed.ProfissionalPublicGuid,
                servicoIds = new[] { seed.ServicoId },
                data = segunda.ToString("yyyy-MM-dd"),
                horarioInicio = "10:00:00",
                clienteNome = "Cliente Visitante",
                clienteEmail = "visitante@email.com",
                clienteTelefone = ""
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Owner_DeveConfirmarECancelarAgendamento()
    {
        var seed = await SeedAgendamentoAsync();
        await ConfigurarHorariosAsync(seed);
        var agendamentoId = await CriarAgendamentoPublicoAsync(seed);

        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.OwnerEmail, seed.Senha);

        var confirmar = await client.PostAsync(
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/agendamentos/{agendamentoId}/confirmar",
            null);
        Assert.Equal(HttpStatusCode.OK, confirmar.StatusCode);

        var cancelar = await client.PostAsJsonAsync(
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/agendamentos/{agendamentoId}/cancelar",
            new { motivo = "Cliente desistiu" });
        Assert.Equal(HttpStatusCode.OK, cancelar.StatusCode);

        var historico = await client.GetAsync(
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/agendamentos/{agendamentoId}/historico");
        Assert.Equal(HttpStatusCode.OK, historico.StatusCode);

        var historicoBody = await historico.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(historicoBody.GetProperty("data").GetArrayLength() >= 2);
    }

    [Fact]
    public async Task SegundoAgendamentoNoMesmoHorario_DeveRetornarConflict()
    {
        var seed = await SeedAgendamentoAsync();
        await ConfigurarHorariosAsync(seed);
        _ = await CriarAgendamentoPublicoAsync(seed);

        var client = _factory.CreateClient();
        var segunda = ObterProximaSegunda();
        var response = await client.PostAsJsonAsync(
            $"/api/publico/agendar/loja/{seed.PublicGuid}",
            new
            {
                profissionalPublicGuid = seed.ProfissionalPublicGuid,
                servicoIds = new[] { seed.ServicoId },
                data = segunda.ToString("yyyy-MM-dd"),
                horarioInicio = "10:00:00",
                clienteNome = "Outro Cliente",
                clienteEmail = "outro@email.com",
                clienteTelefone = "11977776666"
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<int> CriarAgendamentoPublicoAsync(SeedAgendamento seed)
    {
        var client = _factory.CreateClient();
        var segunda = ObterProximaSegunda();
        var response = await client.PostAsJsonAsync(
            $"/api/publico/agendar/loja/{seed.PublicGuid}",
            new
            {
                profissionalPublicGuid = seed.ProfissionalPublicGuid,
                servicoIds = new[] { seed.ServicoId },
                data = segunda.ToString("yyyy-MM-dd"),
                horarioInicio = "10:00:00",
                clienteNome = "Cliente Visitante",
                clienteEmail = "visitante@email.com",
                clienteTelefone = "11988887777"
            });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        return body.GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task ConfigurarHorariosAsync(SeedAgendamento seed)
    {
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.OwnerEmail, seed.Senha);

        var criarFuncionamento = await client.PostAsJsonAsync(
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/horarios-funcionamento",
            new
            {
                diaSemana = DayOfWeek.Monday,
                horaInicio = "08:00:00",
                horaFim = "18:00:00"
            });
        criarFuncionamento.EnsureSuccessStatusCode();

        var criarProfissional = await client.PostAsJsonAsync(
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/profissionais/{seed.ProfissionalId}/horarios",
            new
            {
                diaSemana = DayOfWeek.Monday,
                horaInicio = "09:00:00",
                horaFim = "17:00:00"
            });
        criarProfissional.EnsureSuccessStatusCode();
    }

    private async Task<SeedAgendamento> SeedAgendamentoAsync()
    {
        const string senha = "Senha123!";
        var sufixo = Guid.NewGuid().ToString("N");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var owner = CriarUsuario($"owner-agenda-{sufixo}@email.com", senha, hasher);
        var profissionalUsuario = CriarUsuario($"prof-agenda-{sufixo}@email.com", senha, hasher);

        var plano = new Plano
        {
            Nome = $"Plano Basic Agenda {sufixo}",
            Descricao = "Plano para testes de agendamento",
            Preco = 99.90m,
            Periodo = PlanoPeriodo.Mensal,
            Ativo = true
        };

        var estabelecimento = new Estabelecimento
        {
            Nome = $"Negocio Agenda {sufixo}",
            Descricao = "Negocio para testes integrados de agendamento",
            Telefone = "11999999999",
            Email = $"negocio-agenda-{sufixo}@email.com",
            Ativo = true
        };

        db.Usuarios.AddRange(owner, profissionalUsuario);
        db.Planos.Add(plano);
        db.Estabelecimentos.Add(estabelecimento);
        await db.SaveChangesAsync();

        var profissional = new Profissional
        {
            UsuarioId = profissionalUsuario.Id,
            NomePublico = "Profissional Agenda",
            Email = profissionalUsuario.Email,
            Telefone = "11988887777",
            TipoProfissional = ProfessionalType.VinculadoEstabelecimento,
            Ativo = true
        };

        var servico = new Servico
        {
            EstabelecimentoId = estabelecimento.Id,
            Nome = "Corte Agenda",
            Descricao = "Servico para agendamento",
            PrecoBase = 50,
            DuracaoMinutos = 60,
            Ativo = true
        };

        db.Profissionais.Add(profissional);
        db.Servicos.Add(servico);
        await db.SaveChangesAsync();

        db.EstabelecimentoUsuarios.Add(
            CriarVinculoUsuario(estabelecimento.Id, owner.Id, EstablishmentUserRole.Owner));
        db.EstabelecimentoUsuarios.Add(
            CriarVinculoUsuario(estabelecimento.Id, profissionalUsuario.Id, EstablishmentUserRole.Profissional));

        db.ProfissionalEstabelecimentos.Add(new ProfissionalEstabelecimento
        {
            EstabelecimentoId = estabelecimento.Id,
            ProfissionalId = profissional.Id,
            Ativo = true,
            PodeReceberAgendamento = true
        });

        db.ProfissionalServicos.Add(new ProfissionalServico
        {
            ProfissionalId = profissional.Id,
            ServicoId = servico.Id,
            Preco = 55,
            DuracaoMinutos = 60,
            Ativo = true
        });

        db.Assinaturas.Add(new Assinatura
        {
            EstabelecimentoId = estabelecimento.Id,
            PlanoId = plano.Id,
            Status = AssinaturaStatus.Ativa,
            Inicio = DateTime.UtcNow.AddDays(-1),
            Fim = DateTime.UtcNow.AddMonths(1)
        });

        await db.SaveChangesAsync();

        return new SeedAgendamento(
            estabelecimento.Id,
            estabelecimento.PublicGuid,
            profissional.Id,
            profissional.PublicGuid,
            servico.Id,
            owner.Email,
            senha);
    }

    private static DateOnly ObterProximaSegunda()
    {
        var data = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        while (data.DayOfWeek != DayOfWeek.Monday)
        {
            data = data.AddDays(1);
        }

        return data;
    }

    private static Usuario CriarUsuario(string email, string senha, IPasswordHasher hasher) =>
        new()
        {
            Nome = "Usuario Agenda",
            Email = email,
            Telefone = "11999999999",
            Senha = hasher.Hash(senha),
            Role = UserRole.Cliente,
            Ativo = true
        };

    private static EstabelecimentoUsuario CriarVinculoUsuario(
        int estabelecimentoId,
        int usuarioId,
        EstablishmentUserRole role) =>
        new()
        {
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = usuarioId,
            RoleNoEstabelecimento = role,
            Ativo = true
        };

    private async Task AutenticarAsync(HttpClient client, string email, string senha)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, senha });
        loginResponse.EnsureSuccessStatusCode();

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var token = loginBody.GetProperty("data").GetProperty("token").GetString();
        client.DefaultRequestHeaders.Add("x-glow-token", token);
    }

    private record SeedAgendamento(
        int EstabelecimentoId,
        Guid PublicGuid,
        int ProfissionalId,
        Guid ProfissionalPublicGuid,
        int ServicoId,
        string OwnerEmail,
        string Senha);
}
