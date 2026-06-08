using GLOWAPI.Application.DTOs.Equipe;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IEquipeNegocioService
{
    Task<UsuarioEquipeResponseDto> CadastrarUsuarioAsync(
        int estabelecimentoId,
        CadastrarUsuarioEquipeRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ProfissionalEquipeResponseDto> ConvidarProfissionalAsync(
        int estabelecimentoId,
        ConvidarProfissionalEquipeRequestDto request,
        CancellationToken cancellationToken = default);

    Task<UsuarioEquipeResponseDto> AtualizarRoleUsuarioAsync(
        int estabelecimentoId,
        int usuarioId,
        AtualizarRoleUsuarioEquipeRequestDto request,
        CancellationToken cancellationToken = default);

    Task<UsuarioEquipeResponseDto> AtualizarStatusUsuarioAsync(
        int estabelecimentoId,
        int usuarioId,
        AtualizarStatusUsuarioEquipeRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ProfissionalEquipeResponseDto> AtualizarStatusProfissionalAsync(
        int estabelecimentoId,
        int profissionalId,
        AtualizarStatusProfissionalEquipeRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsuarioEquipeResponseDto>> ListarUsuariosAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfissionalEquipeResponseDto>> ListarProfissionaisAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);
}
