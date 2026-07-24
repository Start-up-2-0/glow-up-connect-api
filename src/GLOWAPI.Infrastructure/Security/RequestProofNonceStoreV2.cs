using System.Collections.Concurrent;
using GLOWAPI.Application.Interfaces.Services;

namespace GLOWAPI.Infrastructure.Security;

/// <summary>
/// Versão 2 do Nonce Store em memória com lógica aprimorada para mitigar race conditions.
/// </summary>
public class RequestProofNonceStoreV2 : IRequestProofNonceStore
{
    // Usamos um ConcurrentDictionary para segurança em nível de thread para operações individuais.
    private readonly ConcurrentDictionary<string, DateTime> _consumedNonces = new();

    // Objeto de bloqueio para operações que precisam ser atômicas em múltiplos passos.
    private readonly object _lock = new();

    private DateTime _lastCleanup = DateTime.UtcNow;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Tenta consumir um nonce. Garante que a verificação de existência e a adição
    /// sejam uma operação atômica para prevenir race conditions.
    /// </summary>
    /// <param name="nonce">O nonce a ser consumido.</param>
    /// <param name="ttl">O tempo de vida (time-to-live) do nonce.</param>
    /// <returns>True se o nonce foi consumido com sucesso; False caso contrário.</returns>
    public bool TryConsume(string nonce, TimeSpan ttl)
    {
        var now = DateTime.UtcNow;

        // Bloqueia esta seção para garantir que a verificação e a adição sejam atômicas.
        lock (_lock)
        {
            // Primeiro, removemos nonces expirados para manter o dicionário limpo.
            // Fazer isso dentro do lock garante consistência.
            Cleanup(now);

            // Verifica se o nonce já foi consumido.
            if (_consumedNonces.ContainsKey(nonce))
            {
                return false; // Nonce já existe, portanto não pode ser consumido.
            }

            // Se não existe, adiciona com a data de expiração.
            var expiresAt = now.Add(ttl);
            return _consumedNonces.TryAdd(nonce, expiresAt);
        }
    }

    /// <summary>
    /// Executa a limpeza de nonces expirados.
    /// </summary>
    private void Cleanup(DateTime now)
    {
        // A verificação do intervalo de limpeza é feita fora do lock para otimização,
        // mas a limpeza real acontece dentro de um lock no método TryConsume.
        if (now - _lastCleanup < CleanupInterval)
        {
            return;
        }

        // Itera sobre uma cópia das chaves para evitar modificar a coleção enquanto itera.
        var keysToRemove = _consumedNonces.Where(kvp => kvp.Value <= now).Select(kvp => kvp.Key).ToList();

        foreach (var key in keysToRemove)
        {
            _consumedNonces.TryRemove(key, out _);
        }

        _lastCleanup = now;
    }
}
