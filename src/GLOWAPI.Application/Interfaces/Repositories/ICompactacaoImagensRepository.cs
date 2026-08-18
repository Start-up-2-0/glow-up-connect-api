namespace GLOWAPI.Application.Interfaces.Repositories;

public interface ICompactacaoImagensRepository
{
    Task<IReadOnlyList<ImagemPersistidaRegistro>> ListarLoteUsuariosComAvatarAsync(
        int ultimoId,
        int tamanhoLote,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImagemPersistidaRegistro>> ListarLoteEstabelecimentosComLogoAsync(
        int ultimoId,
        int tamanhoLote,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImagemPersistidaRegistro>> ListarLoteProfissionaisComLogoAsync(
        int ultimoId,
        int tamanhoLote,
        CancellationToken cancellationToken = default);

    Task AtualizarAvatarUsuarioAsync(int id, string avatarBase64, CancellationToken cancellationToken = default);

    Task AtualizarLogoEstabelecimentoAsync(int id, string logo, CancellationToken cancellationToken = default);

    Task AtualizarLogoProfissionalAsync(int id, string logo, CancellationToken cancellationToken = default);
}

public sealed record ImagemPersistidaRegistro(int Id, string ValorAtual);
