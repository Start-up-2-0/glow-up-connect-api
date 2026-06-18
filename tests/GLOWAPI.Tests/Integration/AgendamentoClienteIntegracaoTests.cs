using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GLOWAPI.Tests.Integration;

public class AgendamentoClienteIntegracaoTests : IClassFixture<GlowApiWebApplicationFactory>
{
    private readonly GlowApiWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AgendamentoClienteIntegracaoTests(GlowApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ClienteLogado_DeveCriarListarDetalharECancelarAgendamento()
    {
        var seed = await SeedAgendamentoClienteAsync();
        await ConfigurarHorariosAsync(seed);
        var segunda = ObterProximaSegunda();

        var client = _factory.CreateClient();
        await AutenticarClienteAsync(client, seed.ClienteEmail, seed.Senha);

        var criar = await client.PostAsJsonAsync("/api/agendamentos", new
        {
            estabelecimentoPublicGuid = seed.PublicGuid,
            profissionalPublicGuid = seed.ProfissionalPublicGuid,
            servicoIds = new[] { seed.ServicoId },
            data = segunda.ToString("yyyy-MM-dd"),
            horarioInicio = "10:00:00",
            observacao = "Agendamento logado"
        });

        Assert.Equal(HttpStatusCode.Created, criar.StatusCode);

        var criarBody = await criar.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var agendamentoId = criarBody.GetProperty("data").GetProperty("id").GetInt32();
        Assert.Equal("PendenteConfirmacao", criarBody.GetProperty("data").GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(criarBody.GetProperty("data").GetProperty("estabelecimentoNome").GetString()));

        var listar = await client.GetAsync("/api/agendamentos/me?pagina=1&tamanhoPagina=10");
        Assert.Equal(HttpStatusCode.OK, listar.StatusCode);

        var listarBody = await listar.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(listarBody.GetProperty("data").GetProperty("total").GetInt32() >= 1);

        var detalhe = await client.GetAsync($"/api/agendamentos/me/{agendamentoId}");
        Assert.Equal(HttpStatusCode.OK, detalhe.StatusCode);

        var cancelar = await client.PostAsJsonAsync(
            $"/api/agendamentos/me/{agendamentoId}/cancelar",
            new { motivo = "Nao poderei comparecer" });
        Assert.Equal(HttpStatusCode.OK, cancelar.StatusCode);

        var cancelarBody = await cancelar.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("Cancelado", cancelarBody.GetProperty("data").GetProperty("status").GetString());
    }

    [Fact]
    public async Task CriarAgendamentoSemAutenticacao_DeveRetornarUnauthorized()
    {
        var seed = await SeedAgendamentoClienteAsync();
        await ConfigurarHorariosAsync(seed);
        var segunda = ObterProximaSegunda();

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/agendamentos", new
        {
            estabelecimentoPublicGuid = seed.PublicGuid,
            profissionalPublicGuid = seed.ProfissionalPublicGuid,
            servicoIds = new[] { seed.ServicoId },
            data = segunda.ToString("yyyy-MM-dd"),
            horarioInicio = "10:00:00"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ClienteLogado_DeveCriarAgendamentoSemPreferenciaDeProfissional()
    {
        var seed = await SeedAgendamentoClienteAsync();
        await ConfigurarHorariosAsync(seed);
        var segunda = ObterProximaSegunda();

        var client = _factory.CreateClient();
        await AutenticarClienteAsync(client, seed.ClienteEmail, seed.Senha);

        var disponibilidade = await client.GetAsync(
            $"/api/publico/agendar/loja/{seed.PublicGuid}/disponibilidade?" +
            $"dataInicio={segunda:yyyy-MM-dd}&dataFim={segunda:yyyy-MM-dd}&servicoIds={seed.ServicoId}");
        disponibilidade.EnsureSuccessStatusCode();

        var disponibilidadeBody = await disponibilidade.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var slots = disponibilidadeBody.GetProperty("data").GetProperty("slots");
        Assert.True(slots.GetArrayLength() > 0);

        var slot = slots[0];
        var inicioSelecionado = slot.GetProperty("inicio").GetDateTime();
        var horarioInicio = inicioSelecionado.ToString("HH:mm:ss");

        var criar = await client.PostAsJsonAsync("/api/agendamentos", new
        {
            estabelecimentoPublicGuid = seed.PublicGuid,
            servicoIds = new[] { seed.ServicoId },
            data = segunda.ToString("yyyy-MM-dd"),
            horarioInicio,
            inicioSelecionado = inicioSelecionado.ToString("O"),
            observacao = "Agendamento interno sem preferencia"
        });

        Assert.Equal(HttpStatusCode.Created, criar.StatusCode);

        var criarBody = await criar.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal("PendenteConfirmacao", criarBody.GetProperty("data").GetProperty("status").GetString());
        Assert.True(criarBody.GetProperty("data").GetProperty("id").GetInt32() > 0);
    }

    [Fact]
    public async Task ObterAgendamentoDeOutroCliente_DeveRetornarNotFound()
    {
        var seed = await SeedAgendamentoClienteAsync();
        await ConfigurarHorariosAsync(seed);
        var agendamentoId = await CriarAgendamentoPublicoAsync(seed);

        var outroCliente = _factory.CreateClient();
        await AutenticarClienteAsync(outroCliente, seed.ClienteEmail, seed.Senha);

        var response = await outroCliente.GetAsync($"/api/agendamentos/me/{agendamentoId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ObterEstabelecimentoPublico_DeveRetornarDetalhe()
    {
        var seed = await SeedAgendamentoClienteAsync();

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/publico/estabelecimentos/{seed.PublicGuid}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.Equal(seed.PublicGuid.ToString(), body.GetProperty("data").GetProperty("publicGuid").GetString());
    }

    private async Task<int> CriarAgendamentoPublicoAsync(SeedAgendamentoCliente seed)
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
                clienteNome = "Visitante",
                clienteEmail = "visitante@email.com",
                clienteTelefone = "11966665555"
            });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        return body.GetProperty("data").GetProperty("id").GetInt32();
    }

    private async Task ConfigurarHorariosAsync(SeedAgendamentoCliente seed)
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

    private async Task<SeedAgendamentoCliente> SeedAgendamentoClienteAsync()
    {
        const string senha = "Senha123!";
        var sufixo = Guid.NewGuid().ToString("N");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var owner = CriarUsuario($"owner-cliente-{sufixo}@email.com", senha, hasher);
        var profissionalUsuario = CriarUsuario($"prof-cliente-{sufixo}@email.com", senha, hasher);
        var cliente = CriarUsuario($"cliente-{sufixo}@email.com", senha, hasher);

        var plano = new Plano
        {
            Nome = $"Plano Cliente {sufixo}",
            Descricao = "Plano para testes de agendamento cliente",
            Preco = 99.90m,
            Periodo = PlanoPeriodo.Mensal,
            Ativo = true
        };

        var estabelecimento = new Estabelecimento
        {
            Nome = $"Negocio Cliente {sufixo}",
            Descricao = "Negocio para testes integrados de agendamento cliente",
            Telefone = "11999999999",
            Email = $"negocio-cliente-{sufixo}@email.com",
            Ativo = true
        };

        db.Usuarios.AddRange(owner, profissionalUsuario, cliente);
        db.Planos.Add(plano);
        db.Estabelecimentos.Add(estabelecimento);
        await db.SaveChangesAsync();

        var profissional = new Profissional
        {
            UsuarioId = profissionalUsuario.Id,
            NomePublico = "Profissional Cliente",
            Email = profissionalUsuario.Email,
            Telefone = "11988887777",
            TipoProfissional = ProfessionalType.VinculadoEstabelecimento,
            Ativo = true
        };

        var servico = new Servico
        {
            EstabelecimentoId = estabelecimento.Id,
            Nome = "Corte Cliente",
            Descricao = "Servico para agendamento cliente",
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

        return new SeedAgendamentoCliente(
            estabelecimento.Id,
            estabelecimento.PublicGuid,
            profissional.Id,
            profissional.PublicGuid,
            servico.Id,
            owner.Email,
            cliente.Email,
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
            Nome = "Usuario Teste",
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

    private Task AutenticarClienteAsync(HttpClient client, string email, string senha) =>
        AutenticarAsync(client, email, senha);

    private record SeedAgendamentoCliente(
        int EstabelecimentoId,
        Guid PublicGuid,
        int ProfissionalId,
        Guid ProfissionalPublicGuid,
        int ServicoId,
        string OwnerEmail,
        string ClienteEmail,
        string Senha);
}
