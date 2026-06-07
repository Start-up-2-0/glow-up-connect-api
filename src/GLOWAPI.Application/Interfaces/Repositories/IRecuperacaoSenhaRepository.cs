using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IRecuperacaoSenhaRepository
{
    /// <summary>
    /// Busca a solicitação de recuperação mais recente do usuário.
    /// </summary>
    Task<RecuperacaoSenha?> ObterUltimoPorUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca pelo hash do reset token (usado no passo 3 - reset-password).
    /// </summary>
    Task<RecuperacaoSenha?> ObterPorResetTokenHashAsync(string resetTokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cria uma nova solicitação de recuperação.
    /// </summary>
    Task AdicionarAsync(RecuperacaoSenha recuperacaoSenha, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza uma solicitação existente (ex: marcar código como verificado).
    /// </summary>
    void Atualizar(RecuperacaoSenha recuperacaoSenha);

    /// <summary>
    /// Persiste as alterações no banco.
    /// </summary>
    Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
}