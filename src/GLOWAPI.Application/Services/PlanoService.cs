using GLOWAPI.Application.DTOs.Planos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;

namespace GLOWAPI.Application.Services;

public class PlanoService : IPlanoService
{
    private readonly IPlanoRepository _planoRepository;
    private readonly IPromocaoLancamentoService _promocaoLancamentoService;

    public PlanoService(
        IPlanoRepository planoRepository,
        IPromocaoLancamentoService promocaoLancamentoService)
    {
        _planoRepository = planoRepository;
        _promocaoLancamentoService = promocaoLancamentoService;
    }

    public async Task<PlanosAtivosResponseDto> ListarAtivosAsync(CancellationToken cancellationToken = default)
    {
        var planos = await _planoRepository.ListarAtivosAsync(cancellationToken);
        var promocao = await _promocaoLancamentoService.ObterStatusAsync(cancellationToken);
        return new PlanosAtivosResponseDto(
            planos.Select(PlanoResponseDto.From).ToList(),
            promocao);
    }
}
