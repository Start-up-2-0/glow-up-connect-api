using GLOWAPI.Application.DTOs.Assinaturas;

namespace GLOWAPI.Application.DTOs.Planos;

public record PlanosAtivosResponseDto(
    IReadOnlyList<PlanoResponseDto> Planos,
    PromocaoLancamentoStatusDto PromocaoLancamento);
