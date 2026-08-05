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

    Task<ProfissionalEquipeResponseDto> AtualizarProfissionalAsync(
        int estabelecimentoId,
        int profissionalId,
        AtualizarProfissionalEquipeRequestDto request,
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

    Task<IReadOnlyList<AgendamentoFuturoEquipeResponseDto>> ListarAgendamentosFuturosProfissionalAsync(
        int estabelecimentoId,
        int profissionalId,
        CancellationToken cancellationToken = default);

    Task<CancelarAgendamentosFuturosProfissionalEquipeResponseDto> CancelarAgendamentosFuturosProfissionalAsync(
        int estabelecimentoId,
        int profissionalId,
        CancelarAgendamentosFuturosProfissionalEquipeRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsuarioEquipeResponseDto>> ListarUsuariosAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfissionalEquipeResponseDto>> ListarProfissionaisAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<EquipeMembrosPaginadoResponseDto> ListarMembrosPaginadoAsync(
        int estabelecimentoId,
        EquipeMembrosFiltroDto filtro,
        CancellationToken cancellationToken = default);

    Task<ProfissionalVitrineResponseDto> CadastrarProfissionalVitrineAsync(
        int estabelecimentoId,
        CadastrarProfissionalVitrineRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ProfissionalVitrineResponseDto> AtualizarProfissionalVitrineAsync(
        int estabelecimentoId,
        int profissionalId,
        AtualizarProfissionalVitrineRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ProfissionalVitrineResponseDto> AtualizarStatusProfissionalVitrineAsync(
        int estabelecimentoId,
        int profissionalId,
        AtualizarStatusProfissionalVitrineRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfissionalVitrineResponseDto>> ListarProfissionaisVitrineAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);
}
