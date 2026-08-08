using GLOWAPI.Application.DTOs.Planos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;

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

    public async Task<PlanosAtivosResponseDto> ListarAtivosAsync(
        TipoAssinatura tipoAssinatura = TipoAssinatura.Estabelecimento,
        CancellationToken cancellationToken = default)
    {
        var planos = await _planoRepository.ListarAtivosAsync(cancellationToken);
        var promocao = await _promocaoLancamentoService.ObterStatusAsync(cancellationToken);
        return new PlanosAtivosResponseDto(
            planos.Select(plano => PlanoResponseDto.From(plano, tipoAssinatura)).ToList(),
            promocao);
    }
}
