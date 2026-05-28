using GLOWAPI.Application.DTOs.Estabelecimentos;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IEstabelecimentoPerfilService
{
    Task<EstabelecimentoPerfilResponseDto> AtualizarAsync(
        int estabelecimentoId,
        AtualizarEstabelecimentoPerfilDto request,
        CancellationToken cancellationToken = default);
}
