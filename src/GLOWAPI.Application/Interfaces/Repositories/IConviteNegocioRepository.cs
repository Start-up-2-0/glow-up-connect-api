using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IConviteNegocioRepository : IRepository<ConviteNegocio>
{
    Task<ConviteNegocio?> ObterPendentePorDestinatarioAsync(
        int estabelecimentoId,
        string email,
        TipoConviteNegocio tipoConvite,
        CancellationToken cancellationToken = default);

    Task<ConviteNegocio?> ObterPorTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);
}
