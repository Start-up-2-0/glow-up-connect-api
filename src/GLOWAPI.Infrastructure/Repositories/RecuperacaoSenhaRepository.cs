using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace GLOWAPI.Infrastructure.Repositories;

public class RecuperacaoSenhaRepository : Repository<RecuperacaoSenha>, IRecuperacaoSenhaRepository
{
    private readonly ApplicationDbContext _context;

    public RecuperacaoSenhaRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    // 1️⃣ Último registro de recuperação do usuário
    public async Task<RecuperacaoSenha?> ObterUltimoPorUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(r => r.UsuarioId == usuarioId)
            .OrderByDescending(r => r.CriadoEm)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // 2️⃣ Busca por token de reset (hash)
    public async Task<RecuperacaoSenha?> ObterPorResetTokenHashAsync(
        string resetTokenHash,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(r => r.ResetTokenHash == resetTokenHash, cancellationToken);
    }

    // 3️⃣ Inserir novo registro
    public async Task AdicionarAsync(
        RecuperacaoSenha recuperacao,
        CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(recuperacao, cancellationToken);
    }

    // 4️⃣ Atualizar registro existente
    public void Atualizar(RecuperacaoSenha recuperacao)
    {
        DbSet.Update(recuperacao);
    }

    // 5️⃣ Persistir alterações no banco
    public async Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
