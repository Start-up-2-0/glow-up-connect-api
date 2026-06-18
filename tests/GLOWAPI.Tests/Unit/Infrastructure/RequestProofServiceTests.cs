using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Security;
using GLOWAPI.Application.Options;
using GLOWAPI.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Tests.Unit.Infrastructure;

public class RequestProofServiceTests
{
    private const string Secret = "glow-test-request-proof-secret-min-32!!";

    [Fact]
    public void EmitirEValidar_DeveAceitarProofValido()
    {
        var service = CreateService();
        var proofs = service.EmitirProofs("ctx-1", "*", "*", 1);
        var proof = proofs[0];

        var resultado = service.ValidarEConsumir(proof, "GET", "/api/planos", "ctx-1");

        Assert.True(resultado.Sucesso);
    }

    [Fact]
    public void ValidarEConsumir_ReusoDoMesmoProof_DeveFalharComReplay()
    {
        var service = CreateService();
        var proof = service.EmitirProofs("ctx-1", "*", "*", 1)[0];

        Assert.True(service.ValidarEConsumir(proof, "GET", "/api/planos", "ctx-1").Sucesso);
        var replay = service.ValidarEConsumir(proof, "GET", "/api/planos", "ctx-1");

        Assert.Equal(RequestProofFailureCode.Replay, replay.Codigo);
    }

    [Fact]
    public void ValidarEConsumir_AssinaturaInvalida_DeveFalhar()
    {
        var service = CreateService();
        var proof = service.EmitirProofs("ctx-1", "*", "*", 1)[0] + "x";

        var resultado = service.ValidarEConsumir(proof, "GET", "/api/planos", "ctx-1");

        Assert.Equal(RequestProofFailureCode.Invalido, resultado.Codigo);
    }

    [Fact]
    public void ValidarEConsumir_ContextoDiferente_DeveFalhar()
    {
        var service = CreateService();
        var proof = service.EmitirProofs("ctx-1", "*", "*", 1)[0];

        var resultado = service.ValidarEConsumir(proof, "GET", "/api/planos", "ctx-2");

        Assert.Equal(RequestProofFailureCode.ContextoInvalido, resultado.Codigo);
    }

    [Fact]
    public void ValidarEConsumir_PathEspecifico_DeveRespeitarWildcard()
    {
        var service = CreateService();
        var proofWildcard = service.EmitirProofs("ctx-1", "*", "*", 1)[0];
        var proofEspecifico = service.EmitirProofs("ctx-1", "GET", "/api/auth/login", 1)[0];

        Assert.True(service.ValidarEConsumir(proofWildcard, "POST", "/api/auth/login", "ctx-1").Sucesso);
        Assert.Equal(
            RequestProofFailureCode.MetodoPathInvalido,
            service.ValidarEConsumir(proofEspecifico, "POST", "/api/auth/login", "ctx-1").Codigo);
    }

    [Fact]
    public void NonceStore_TryConsume_DevePermitirApenasUmaVez()
    {
        var store = new RequestProofNonceStore();

        Assert.True(store.TryConsume("nonce-1", TimeSpan.FromMinutes(1)));
        Assert.False(store.TryConsume("nonce-1", TimeSpan.FromMinutes(1)));
    }

    private static RequestProofService CreateService()
    {
        var options = Options.Create(new RequestProofOptions
        {
            Enabled = true,
            Secret = Secret,
            TtlSeconds = 60,
            ClockSkewSeconds = 30
        });

        return new RequestProofService(options, new RequestProofNonceStore());
    }
}
