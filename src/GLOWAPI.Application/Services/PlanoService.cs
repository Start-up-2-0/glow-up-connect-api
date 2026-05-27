using GLOWAPI.Application.DTOs.Planos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;

namespace GLOWAPI.Application.Services;

public class PlanoService : IPlanoService
{
    private readonly IPlanoRepository _planoRepository;

    public PlanoService(IPlanoRepository planoRepository)
    {
        _planoRepository = planoRepository;
    }

    public async Task<IReadOnlyList<PlanoResponseDto>> ListarAtivosAsync(CancellationToken cancellationToken = default)
    {
        var planos = await _planoRepository.ListarAtivosAsync(cancellationToken);
        return planos.Select(PlanoResponseDto.From).ToList();
    }
}
