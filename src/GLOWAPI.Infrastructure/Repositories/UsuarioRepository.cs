using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(usuario => usuario.Email == email, cancellationToken);
    }

    public Task<Usuario?> ObterPorTelefoneAsync(string telefone, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(usuario => usuario.Telefone == telefone, cancellationToken);
    }

    public async Task<Usuario?> ObterPorTelefoneNormalizadoAsync(
        string telefoneNormalizado,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(telefoneNormalizado))
        {
            return null;
        }

        var candidatos = await DbSet
            .Where(usuario => usuario.Telefone != null && usuario.Telefone != string.Empty)
            .ToListAsync(cancellationToken);

        return candidatos.FirstOrDefault(usuario =>
            TelefoneHelper.SaoEquivalentes(usuario.Telefone, telefoneNormalizado));
    }

    public Task<Usuario?> ObterPorConfirmacaoTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(usuario => usuario.ConfirmacaoTokenHash == tokenHash, cancellationToken);
    }

    public Task<Usuario?> ObterPorConfirmacaoCodigoHashAsync(string codigoHash, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(usuario => usuario.ConfirmacaoCodigoHash == codigoHash, cancellationToken);
    }

    public Task<Usuario?> ObterPorWhatsAppConfirmacaoTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(usuario => usuario.WhatsAppConfirmacaoTokenHash == tokenHash, cancellationToken);
    }

    public Task<Usuario?> ObterPorWhatsAppConfirmacaoCodigoHashAsync(string codigoHash, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(usuario => usuario.WhatsAppConfirmacaoCodigoHash == codigoHash, cancellationToken);
    }
}
