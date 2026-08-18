using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
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

    public Task<Usuario?> ObterPorRecuperacaoTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(usuario => usuario.RecuperacaoTokenHash == tokenHash, cancellationToken);
    }

    public Task<Usuario?> ObterPorRecuperacaoCodigoHashAsync(string codigoHash, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(usuario => usuario.RecuperacaoCodigoHash == codigoHash, cancellationToken);
    }

    public Task<Usuario?> ObterPorWhatsAppConfirmacaoTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(usuario => usuario.WhatsAppConfirmacaoTokenHash == tokenHash, cancellationToken);
    }

    public Task<Usuario?> ObterPorWhatsAppConfirmacaoCodigoHashAsync(string codigoHash, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(usuario => usuario.WhatsAppConfirmacaoCodigoHash == codigoHash, cancellationToken);
    }

    public Task<Usuario?> ObterPorCodigoAgendamentoAsync(string codigoAgendamento, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(usuario => usuario.CodigoAgendamento == codigoAgendamento, cancellationToken);
    }

    public Task<bool> ExisteCodigoAgendamentoAsync(string codigoAgendamento, CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(usuario => usuario.CodigoAgendamento == codigoAgendamento, cancellationToken);
    }

    public async Task<IReadOnlyList<Usuario>> ListarExclusoesPendentesVencidasAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(usuario =>
                usuario.ExclusaoStatus == ExclusaoStatus.Pendente
                && (usuario.ExclusaoEfetivarEm == null || usuario.ExclusaoEfetivarEm <= utcNow))
            .ToListAsync(cancellationToken);
    }
}
