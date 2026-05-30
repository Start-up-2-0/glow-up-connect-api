using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class ConviteNegocioRepository : Repository<ConviteNegocio>, IConviteNegocioRepository
{
    public ConviteNegocioRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<ConviteNegocio?> ObterPendentePorDestinatarioAsync(
        int estabelecimentoId,
        string email,
        TipoConviteNegocio tipoConvite,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            convite => convite.EstabelecimentoId == estabelecimentoId
                && convite.Email == email
                && convite.TipoConvite == tipoConvite
                && convite.Status == StatusConviteNegocio.Pendente,
            cancellationToken);
    }

    public Task<ConviteNegocio?> ObterPorTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(convite => convite.Estabelecimento)
            .FirstOrDefaultAsync(convite => convite.TokenHash == tokenHash, cancellationToken);
    }
}
