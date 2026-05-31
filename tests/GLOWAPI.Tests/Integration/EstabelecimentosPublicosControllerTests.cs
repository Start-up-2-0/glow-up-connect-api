using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Geolocalizacao;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GLOWAPI.Tests.Integration;

public class EstabelecimentosPublicosControllerTests : IClassFixture<EstabelecimentosPublicosWebApplicationFactory>
{
    private readonly EstabelecimentosPublicosWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public EstabelecimentosPublicosControllerTests(EstabelecimentosPublicosWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListarProximos_DeveRetornarEstabelecimentosDentroDoRaio_SemAutenticacao()
    {
        await SeedEstabelecimentosAsync();

        var client = _factory.CreateClient();
        var response = await client.GetAsync(
            "/api/publico/estabelecimentos/proximos?latitude=-22.9056&longitude=-47.0608&raioKm=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        Assert.True(body.GetProperty("success").GetBoolean());

        var data = body.GetProperty("data");
        Assert.Equal("Campinas", data.GetProperty("cidade").GetString());
        Assert.Equal("SP", data.GetProperty("estado").GetString());
        Assert.Equal(10, data.GetProperty("raioKm").GetDouble());

        var itens = data.GetProperty("itens").EnumerateArray().ToList();
        Assert.Single(itens);
        Assert.Equal("Barbearia Centro", itens[0].GetProperty("nome").GetString());
        Assert.True(itens[0].GetProperty("distanciaKm").GetDouble() <= 10);
    }

    [Fact]
    public async Task ListarProximos_DeveExcluirEstabelecimentoSemCoordenadas()
    {
        await SeedEstabelecimentosAsync(incluirSemCoordenadas: true);

        var client = _factory.CreateClient();
        var response = await client.GetAsync(
            "/api/publico/estabelecimentos/proximos?latitude=-22.9056&longitude=-47.0608&raioKm=10");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        var itens = body.GetProperty("data").GetProperty("itens").EnumerateArray().ToList();

        Assert.Single(itens);
        Assert.DoesNotContain(itens, item => item.GetProperty("nome").GetString() == "Salao Incompleto");
    }

    private async Task SeedEstabelecimentosAsync(bool incluirSemCoordenadas = false)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.Estabelecimentos.RemoveRange(db.Estabelecimentos);
        db.Enderecos.RemoveRange(db.Enderecos);

        db.Estabelecimentos.Add(new Estabelecimento
        {
            PublicGuid = Guid.NewGuid(),
            Nome = "Barbearia Centro",
            Descricao = "Cortes masculinos",
            Logo = "https://cdn.test/logo.png",
            Telefone = "11999999999",
            Email = "barbearia@email.com",
            Ativo = true,
            Endereco = new Endereco
            {
                Cep = "13010000",
                Logradouro = "Rua Barao de Jaguara",
                Numero = "100",
                Bairro = "Centro",
                Cidade = "Campinas",
                Estado = "SP",
                Latitude = -22.9060m,
                Longitude = -47.0610m,
                GeocodificadoEm = DateTime.UtcNow
            }
        });

        if (incluirSemCoordenadas)
        {
            db.Estabelecimentos.Add(new Estabelecimento
            {
                PublicGuid = Guid.NewGuid(),
                Nome = "Salao Incompleto",
                Descricao = "Sem geocode",
                Logo = "logo.png",
                Telefone = "11988888888",
                Email = "salao@email.com",
                Ativo = true,
                Endereco = new Endereco
                {
                    Cep = "13010000",
                    Logradouro = "Rua X",
                    Numero = "1",
                    Bairro = "Centro",
                    Cidade = "Campinas",
                    Estado = "SP"
                }
            });
        }

        db.Estabelecimentos.Add(new Estabelecimento
        {
            PublicGuid = Guid.NewGuid(),
            Nome = "Barbearia Distante",
            Descricao = "Fora do raio",
            Logo = "logo.png",
            Telefone = "11977777777",
            Email = "distante@email.com",
            Ativo = true,
            Endereco = new Endereco
            {
                Cep = "13010000",
                Logradouro = "Rua Longe",
                Numero = "500",
                Bairro = "Centro",
                Cidade = "Campinas",
                Estado = "SP",
                Latitude = -23.0500m,
                Longitude = -47.2000m,
                GeocodificadoEm = DateTime.UtcNow
            }
        });

        await db.SaveChangesAsync();
    }
}

public class EstabelecimentosPublicosWebApplicationFactory : GlowApiWebApplicationFactory
{
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(service => service.ServiceType == typeof(IGeocodificadorService));
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        var geocodificador = new Mock<IGeocodificadorService>();
        geocodificador
            .Setup(g => g.ReverseGeocodificarAsync(-22.9056m, -47.0608m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocalizacaoReversa("Campinas", "SP"));

        services.AddSingleton(geocodificador.Object);
    }
}
