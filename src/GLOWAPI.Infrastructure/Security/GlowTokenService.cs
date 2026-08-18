using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Auth;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Infrastructure.Security;

public class GlowTokenService : IGlowTokenService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly AuthOptions _authOptions;
    private readonly byte[] _saltKey;

    public GlowTokenService(IOptions<AuthOptions> authOptions)
    {
        _authOptions = authOptions.Value;
        if (string.IsNullOrWhiteSpace(_authOptions.TokenSalt) || _authOptions.TokenSalt.Length < 32)
        {
            throw new InvalidOperationException("Auth:TokenSalt deve possuir ao menos 32 caracteres.");
        }

        _saltKey = Encoding.UTF8.GetBytes(_authOptions.TokenSalt);
    }

    public string EmitirAccessToken(Usuario usuario, int sessionId, DateTime issuedAt)
    {
        var payload = new
        {
            uid = usuario.Id,
            sid = sessionId,
            iat = new DateTimeOffset(issuedAt).ToUnixTimeSeconds(),
            nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)),
            role = usuario.Role.ToString()
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadEncoded = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signature = Base64UrlEncode(ComputeHmac(payloadEncoded));

        return $"{payloadEncoded}.{signature}";
    }

    public GlowTokenMetadata? ValidarMetadata(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 2)
        {
            return null;
        }

        var payloadEncoded = parts[0];
        var signature = parts[1];
        var expectedSignature = Base64UrlEncode(ComputeHmac(payloadEncoded));

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(signature)))
        {
            return null;
        }

        try
        {
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payloadEncoded));
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;

            if (!root.TryGetProperty("uid", out var uidProp) ||
                !root.TryGetProperty("sid", out var sidProp) ||
                !root.TryGetProperty("iat", out var iatProp) ||
                !root.TryGetProperty("role", out var roleProp))
            {
                return null;
            }

            if (!Enum.TryParse<UserRole>(roleProp.GetString(), out var role))
            {
                return null;
            }

            return new GlowTokenMetadata(uidProp.GetInt32(), sidProp.GetInt32(), iatProp.GetInt64(), role);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string GerarRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Base64UrlEncode(bytes);
    }

    public string HashToken(string token)
    {
        var hash = ComputeHmac(token);
        return Convert.ToHexString(hash);
    }

    public string ProtegerToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var key = SHA256.HashData(_saltKey);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plaintext = Encoding.UTF8.GetBytes(token);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var payload = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSize + TagSize, ciphertext.Length);
        return Convert.ToBase64String(payload);
    }

    public string? DesprotegerToken(string tokenProtegido)
    {
        if (string.IsNullOrWhiteSpace(tokenProtegido))
        {
            return null;
        }

        try
        {
            var payload = Convert.FromBase64String(tokenProtegido);
            if (payload.Length <= NonceSize + TagSize)
            {
                return null;
            }

            var nonce = payload.AsSpan(0, NonceSize);
            var tag = payload.AsSpan(NonceSize, TagSize);
            var ciphertext = payload.AsSpan(NonceSize + TagSize);
            var plaintext = new byte[ciphertext.Length];

            var key = SHA256.HashData(_saltKey);
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }
        catch (FormatException)
        {
            return null;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public DateTime ObterExpiracaoAccessToken(DateTime issuedAt) =>
        issuedAt.AddMinutes(_authOptions.SessionMinutes);

    public DateTime ObterExpiracaoRefreshToken(DateTime issuedAt) =>
        issuedAt.AddDays(_authOptions.RefreshTokenDays);

    public bool EstaExpirado(GlowTokenMetadata metadata, DateTime utcNow)
    {
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(metadata.IssuedAtUnix).UtcDateTime;
        return ObterExpiracaoAccessToken(issuedAt) <= utcNow;
    }

    private byte[] ComputeHmac(string value)
    {
        using var hmac = new HMACSHA256(_saltKey);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }
}
