using GLOWAPI.Application.DTOs.Profissionais;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IProfissionalAutonomoPerfilService
{
    Task<ProfissionalAutonomoPerfilResponseDto> AtualizarAsync(
        int profissionalId,
        AtualizarProfissionalAutonomoPerfilDto request,
        CancellationToken cancellationToken = default);
}
