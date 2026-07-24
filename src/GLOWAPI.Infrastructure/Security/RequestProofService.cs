using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Security;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Infrastructure.Security;

public class RequestProofService : IRequestProofService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly RequestProofOptions _options;
    private readonly IRequestProofNonceStore _nonceStore;
    private byte[]? _secretKey;

    public RequestProofService(IOptions<RequestProofOptions> options, IRequestProofNonceStore nonceStore)
    {
        _options = options.Value;
        _nonceStore = nonceStore;
    }

    private byte[] SecretKey => _secretKey ??= CriarSecretKey();

    public IReadOnlyList<string> EmitirProofs(string contextId, string? method, string? path, int count)
    {
        if (string.IsNullOrWhiteSpace(contextId))
        {
            throw new ArgumentException("Contexto de request proof obrigatorio.", nameof(contextId));
        }

        var quantidade = Math.Clamp(count, 1, 20);
        var metodo = NormalizarMetodo(method) ?? RequestProofOptions.Wildcard;
        var caminho = string.Equals(path, RequestProofOptions.Wildcard, StringComparison.Ordinal)
            ? RequestProofOptions.Wildcard
            : RequestProofPathNormalizer.NormalizePath(path); // Alterado
        var proofs = new List<string>(quantidade);

        for (var i = 0; i < quantidade; i++)
        {
            proofs.Add(GerarProof(contextId, metodo, caminho));
        }

        return proofs;
    }

    public RequestProofValidationResult ValidarEConsumir(
        string? proofHeader,
        string method,
        string path,
        string? contextId)
    {
        if (string.IsNullOrWhiteSpace(proofHeader))
        {
            return RequestProofValidationResult.Falha(RequestProofFailureCode.Ausente);
        }

        var partes = proofHeader.Split('.', 2);
        if (partes.Length != 2)
        {
            return RequestProofValidationResult.Falha(RequestProofFailureCode.Invalido);
        }

        var payloadEncoded = partes[0];
        var assinatura = partes[1];
        var assinaturaEsperada = RequestProofCrypto.Base64UrlEncode(ComputeHmac(payloadEncoded));

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(assinaturaEsperada),
                Encoding.UTF8.GetBytes(assinatura)))
        {
            return RequestProofValidationResult.Falha(RequestProofFailureCode.Invalido);
        }

        RequestProofPayload payload;
        try
        {
            var payloadJson = Encoding.UTF8.GetString(RequestProofCrypto.Base64UrlDecode(payloadEncoded));
            payload = JsonSerializer.Deserialize<RequestProofPayload>(payloadJson, JsonOptions)
                ?? throw new JsonException("Payload nulo.");
        }
        catch (JsonException)
        {
            return RequestProofValidationResult.Falha(RequestProofFailureCode.Invalido);
        }

        if (string.IsNullOrWhiteSpace(payload.Nonce)
            || string.IsNullOrWhiteSpace(payload.Ctx)
            || string.IsNullOrWhiteSpace(payload.M)
            || string.IsNullOrWhiteSpace(payload.P))
        {
            return RequestProofValidationResult.Falha(RequestProofFailureCode.Invalido);
        }

        if (!string.Equals(payload.Ctx, contextId, StringComparison.Ordinal))
        {
            return RequestProofValidationResult.Falha(RequestProofFailureCode.ContextoInvalido);
        }

        var agora = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var tolerancia = Math.Max(1, _options.ClockSkewSeconds);
        var validade = Math.Max(1, _options.TtlSeconds);
        var idade = agora - payload.Ts;
        if (idade > validade + tolerancia || idade < -tolerancia)
        {
            return RequestProofValidationResult.Falha(RequestProofFailureCode.Expirado);
        }

        var metodoRequest = NormalizarMetodo(method) ?? string.Empty;
        var pathRequest = string.Equals(path, RequestProofOptions.Wildcard, StringComparison.Ordinal)
            ? RequestProofOptions.Wildcard
            : RequestProofPathNormalizer.NormalizePath(path) ?? string.Empty; // Alterado

        if (!MetodoCompativel(payload.M, metodoRequest) || !PathCompativel(payload.P, pathRequest))
        {
            return RequestProofValidationResult.Falha(RequestProofFailureCode.MetodoPathInvalido);
        }

        var ttl = TimeSpan.FromSeconds(validade + tolerancia);
        if (!_nonceStore.TryConsume(payload.Nonce, ttl))
        {
            return RequestProofValidationResult.Falha(RequestProofFailureCode.Replay);
        }

        return RequestProofValidationResult.Ok();
    }

    private string GerarProof(string contextId, string method, string path)
    {
        var payload = new RequestProofPayload
        {
            Ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
            M = method,
            P = path,
            Ctx = contextId
        };

        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var payloadEncoded = RequestProofCrypto.Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signature = RequestProofCrypto.Base64UrlEncode(ComputeHmac(payloadEncoded));
        return $"{payloadEncoded}.{signature}";
    }

    private byte[] ComputeHmac(string value)
    {
        using var hmac = new HMACSHA256(SecretKey);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
    }

    private byte[] CriarSecretKey()
    {
        if (string.IsNullOrWhiteSpace(_options.Secret) || _options.Secret.Length < 32)
        {
            throw new InvalidOperationException("RequestProof:Secret deve possuir ao menos 32 caracteres.");
        }

        return Encoding.UTF8.GetBytes(_options.Secret);
    }

    private static bool MetodoCompativel(string proofMethod, string requestMethod) =>
        string.Equals(proofMethod, RequestProofOptions.Wildcard, StringComparison.OrdinalIgnoreCase)
        || string.Equals(proofMethod, requestMethod, StringComparison.OrdinalIgnoreCase);

    private static bool PathCompativel(string proofPath, string requestPath) =>
        string.Equals(proofPath, RequestProofOptions.Wildcard, StringComparison.Ordinal)
        || string.Equals(proofPath, requestPath, StringComparison.OrdinalIgnoreCase);

    private static string? NormalizarMetodo(string? method)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            return null;
        }

        return method.Trim().ToUpperInvariant();
    }
}
