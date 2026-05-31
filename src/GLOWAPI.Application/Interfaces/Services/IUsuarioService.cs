using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.DTOs.Usuario;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IUsuarioService
{
    Task<Usuario> CadastrarClienteAsync(CadastrarClienteDto dto, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterUsuarioPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Usuario?> ObterUsuarioPorEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Usuario> ObterPerfilAtualAsync(CancellationToken cancellationToken = default);
    Task AtualizarPerfilAtualAsync(AtualizarUsuarioDto dto, CancellationToken cancellationToken = default);
    Task DesativarContaAtualAsync(CancellationToken cancellationToken = default);
    Task<WhatsAppConfirmacaoInstrucoesDto> SolicitarConfirmacaoWhatsAppAtualAsync(CancellationToken cancellationToken = default);
    Task AtualizarWhatsAppOptInAtualAsync(bool optIn, CancellationToken cancellationToken = default);
    bool VerificarSenha(string senha, string Senha);
}
