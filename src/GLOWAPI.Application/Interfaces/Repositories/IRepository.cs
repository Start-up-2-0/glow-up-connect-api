namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> ListarAsync(CancellationToken cancellationToken = default);
    Task AdicionarAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Atualizar(TEntity entity);
    void Remover(TEntity entity);
    Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
}
