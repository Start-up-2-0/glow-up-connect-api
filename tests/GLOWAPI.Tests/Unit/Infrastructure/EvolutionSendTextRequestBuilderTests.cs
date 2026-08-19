using System.Text.Json;
using GLOWAPI.Infrastructure.Mensageria;

namespace GLOWAPI.Tests.Unit.Infrastructure;

public class EvolutionSendTextRequestBuilderTests
{
    [Fact]
    public void CriarBodyV1_DeveSeguirEstruturaEvolution()
    {
        const string body = """
            {
              "number": "557991992830",
              "text": "Hello, World!"
            }
            """;

        var gerado = EvolutionSendTextRequestBuilder.CriarBodyV1("557991992830", "Hello, World!");

        using var esperado = JsonDocument.Parse(body);
        using var atual = JsonDocument.Parse(gerado);

        Assert.Equal(esperado.RootElement.GetProperty("number").GetString(), atual.RootElement.GetProperty("number").GetString());
        Assert.Equal(
            esperado.RootElement.GetProperty("text").GetString(),
            atual.RootElement.GetProperty("text").GetString());
    }
}
