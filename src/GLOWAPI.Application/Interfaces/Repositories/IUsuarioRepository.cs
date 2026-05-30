using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorTelefoneAsync(string telefone, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorConfirmacaoTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterPorConfirmacaoCodigoHashAsync(string codigoHash, CancellationToken cancellationToken = default);
}
