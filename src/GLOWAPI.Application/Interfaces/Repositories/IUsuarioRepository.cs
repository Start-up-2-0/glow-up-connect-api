using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorTelefoneAsync(string telefone, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorTelefoneNormalizadoAsync(string telefoneNormalizado, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorConfirmacaoTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorConfirmacaoCodigoHashAsync(string codigoHash, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorRecuperacaoTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorRecuperacaoCodigoHashAsync(string codigoHash, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorWhatsAppConfirmacaoTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorWhatsAppConfirmacaoCodigoHashAsync(string codigoHash, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorCodigoAgendamentoAsync(string codigoAgendamento, CancellationToken cancellationToken = default);
    Task<bool> ExisteCodigoAgendamentoAsync(string codigoAgendamento, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Usuario>> ListarExclusoesPendentesVencidasAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
