using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GLOWAPI.Tests.Integration;

public class HorariosAtendimentoIntegracaoTests : IClassFixture<GlowApiWebApplicationFactory>
{
    private readonly GlowApiWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public HorariosAtendimentoIntegracaoTests(GlowApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Owner_DeveCriarHorarioFuncionamentoEConsultarDisponibilidadePublica()
    {
        var seed = await SeedHorariosAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.OwnerEmail, seed.Senha);

        var criarResponse = await client.PostAsJsonAsync(
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/horarios-funcionamento",
            new
            {
                diaSemana = DayOfWeek.Monday,
                horaInicio = "08:00:00",
                horaFim = "18:00:00"
            });

        Assert.Equal(HttpStatusCode.Created, criarResponse.StatusCode);

        var criarProfissionalResponse = await client.PostAsJsonAsync(
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/profissionais/{seed.ProfissionalId}/horarios",
            new
            {
                diaSemana = DayOfWeek.Monday,
                horaInicio = "09:00:00",
                horaFim = "17:00:00"
            });

        Assert.Equal(HttpStatusCode.Created, criarProfissionalResponse.StatusCode);

        var publicClient = _factory.CreateClient();
        var segunda = ObterProximaSegunda();
        var disponibilidadeResponse = await publicClient.GetAsync(
            $"/api/publico/agendar/loja/{seed.PublicGuid}/disponibilidade?dataInicio={segunda:yyyy-MM-dd}&dataFim={segunda:yyyy-MM-dd}&servicoId={seed.ServicoId}&profissionalId={seed.ProfissionalId}");

        Assert.Equal(HttpStatusCode.OK, disponibilidadeResponse.StatusCode);
        var body = await disponibilidadeResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(body.GetProperty("data").GetProperty("slots").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Recepcionista_DeveVisualizarHorariosESerBloqueadaAoCriarFuncionamento()
    {
        var seed = await SeedHorariosAsync();
        var client = _factory.CreateClient();
        await AutenticarAsync(client, seed.RecepcionistaEmail, seed.Senha);

        var listar = await client.GetAsync(
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/horarios-funcionamento");
        var criar = await client.PostAsJsonAsync(
            $"/api/estabelecimentos/{seed.EstabelecimentoId}/horarios-funcionamento",
            new
            {
                diaSemana = DayOfWeek.Tuesday,
                horaInicio = "08:00:00",
                horaFim = "12:00:00"
            });

        Assert.Equal(HttpStatusCode.OK, listar.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, criar.StatusCode);
    }

    private async Task<SeedHorarios> SeedHorariosAsync()
    {
        const string senha = "Senha123!";
        var sufixo = Guid.NewGuid().ToString("N");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var owner = CriarUsuario($"owner-horario-{sufixo}@email.com", senha, hasher);
        var recepcionista = CriarUsuario($"recepcao-horario-{sufixo}@email.com", senha, hasher);
        var profissionalUsuario = CriarUsuario($"prof-horario-{sufixo}@email.com", senha, hasher);

        var plano = new Plano
        {
            Nome = $"Plano Horarios {sufixo}",
            Descricao = "Plano para testes de horarios",
            Preco = 99.90m,
            Periodo = PlanoPeriodo.Mensal,
            Ativo = true
        };

        var estabelecimento = new Estabelecimento
        {
            Nome = $"Negocio Horarios {sufixo}",
            Descricao = "Negocio para testes integrados de horarios",
            Telefone = "11999999999",
            Email = $"negocio-horario-{sufixo}@email.com",
            Ativo = true
        };

        db.Usuarios.AddRange(owner, recepcionista, profissionalUsuario);
        db.Planos.Add(plano);
        db.Estabelecimentos.Add(estabelecimento);
        await db.SaveChangesAsync();

        var profissional = new Profissional
        {
            UsuarioId = profissionalUsuario.Id,
            NomePublico = "Profissional Horarios",
            Email = profissionalUsuario.Email,
            Telefone = profissionalUsuario.Telefone,
            TipoProfissional = ProfessionalType.VinculadoEstabelecimento,
            Ativo = true
        };

        var servico = new Servico
        {
            EstabelecimentoId = estabelecimento.Id,
            Nome = "Corte Teste",
            Descricao = "Servico para disponibilidade",
            PrecoBase = 50,
            DuracaoMinutos = 60,
            Ativo = true
        };

        db.Profissionais.Add(profissional);
        db.Servicos.Add(servico);
        await db.SaveChangesAsync();

        db.EstabelecimentoUsuarios.AddRange(
            CriarVinculoUsuario(estabelecimento.Id, owner.Id, EstablishmentUserRole.Owner),
            CriarVinculoUsuario(estabelecimento.Id, recepcionista.Id, EstablishmentUserRole.Receptionist),
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

        return new SeedHorarios(
            estabelecimento.Id,
            estabelecimento.PublicGuid,
            profissional.Id,
            servico.Id,
            owner.Email,
            recepcionista.Email,
            senha);
    }

    private static DateOnly ObterProximaSegunda()
    {
        var data = DateOnly.FromDateTime(DateTime.UtcNow);
        while (data.DayOfWeek != DayOfWeek.Monday)
        {
            data = data.AddDays(1);
        }

        return data;
    }

    private static Usuario CriarUsuario(string email, string senha, IPasswordHasher hasher) =>
        new()
        {
            Nome = "Usuario Horarios",
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
        // Sessão autenticada via cookie HttpOnly guc_access (HandleCookies no client).
    }

    private record SeedHorarios(
        int EstabelecimentoId,
        Guid PublicGuid,
        int ProfissionalId,
        int ServicoId,
        string OwnerEmail,
        string RecepcionistaEmail,
        string Senha);
}
