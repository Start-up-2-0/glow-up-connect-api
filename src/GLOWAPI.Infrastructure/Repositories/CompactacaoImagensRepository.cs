using GLOWAPI.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class CompactacaoImagensRepository : ICompactacaoImagensRepository
{
    private readonly ApplicationDbContext _context;

    public CompactacaoImagensRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ImagemPersistidaRegistro>> ListarLoteUsuariosComAvatarAsync(
        int ultimoId,
        int tamanhoLote,
        CancellationToken cancellationToken = default)
    {
        return await _context.Usuarios
            .AsNoTracking()
            .Where(usuario => usuario.Id > ultimoId
                && usuario.AvatarBase64 != null
                && usuario.AvatarBase64 != string.Empty)
            .OrderBy(usuario => usuario.Id)
            .Take(tamanhoLote)
            .Select(usuario => new ImagemPersistidaRegistro(usuario.Id, usuario.AvatarBase64!))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ImagemPersistidaRegistro>> ListarLoteEstabelecimentosComLogoAsync(
        int ultimoId,
        int tamanhoLote,
        CancellationToken cancellationToken = default)
    {
        return await _context.Estabelecimentos
            .AsNoTracking()
            .Where(estabelecimento => estabelecimento.Id > ultimoId && estabelecimento.Logo != string.Empty)
            .OrderBy(estabelecimento => estabelecimento.Id)
            .Take(tamanhoLote)
            .Select(estabelecimento => new ImagemPersistidaRegistro(estabelecimento.Id, estabelecimento.Logo))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ImagemPersistidaRegistro>> ListarLoteProfissionaisComLogoAsync(
        int ultimoId,
        int tamanhoLote,
        CancellationToken cancellationToken = default)
    {
        return await _context.Profissionais
            .AsNoTracking()
            .Where(profissional => profissional.Id > ultimoId && profissional.Logo != string.Empty)
            .OrderBy(profissional => profissional.Id)
            .Take(tamanhoLote)
            .Select(profissional => new ImagemPersistidaRegistro(profissional.Id, profissional.Logo))
            .ToListAsync(cancellationToken);
    }

    public Task AtualizarAvatarUsuarioAsync(int id, string avatarBase64, CancellationToken cancellationToken = default) =>
        _context.Usuarios
            .Where(usuario => usuario.Id == id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(usuario => usuario.AvatarBase64, avatarBase64),
                cancellationToken);

    public Task AtualizarLogoEstabelecimentoAsync(int id, string logo, CancellationToken cancellationToken = default) =>
        _context.Estabelecimentos
            .Where(estabelecimento => estabelecimento.Id == id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(estabelecimento => estabelecimento.Logo, logo),
                cancellationToken);

    public Task AtualizarLogoProfissionalAsync(int id, string logo, CancellationToken cancellationToken = default) =>
        _context.Profissionais
            .Where(profissional => profissional.Id == id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(profissional => profissional.Logo, logo),
                cancellationToken);
}
