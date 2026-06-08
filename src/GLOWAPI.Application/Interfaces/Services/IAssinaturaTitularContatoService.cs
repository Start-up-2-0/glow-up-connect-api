using GLOWAPI.Application.Models.Assinaturas;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAssinaturaTitularContatoService
{
    Task<AssinaturaTitularContato> ResolverAsync(
        Assinatura assinatura,
        CancellationToken cancellationToken = default);
}
