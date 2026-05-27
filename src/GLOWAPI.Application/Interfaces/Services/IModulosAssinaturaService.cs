using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IModulosAssinaturaService
{
    Task<ModulosAssinaturaResponseDto> ObterPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<ModulosAssinaturaResponseDto> ObterPorProfissionalAutonomoAsync(
        int profissionalId,
        CancellationToken cancellationToken = default);

    Task<bool> PossuiModuloPorEstabelecimentoAsync(
        int estabelecimentoId,
        ModuloAssinatura modulo,
        CancellationToken cancellationToken = default);

    Task<bool> PossuiModuloPorProfissionalAutonomoAsync(
        int profissionalId,
        ModuloAssinatura modulo,
        CancellationToken cancellationToken = default);
}
