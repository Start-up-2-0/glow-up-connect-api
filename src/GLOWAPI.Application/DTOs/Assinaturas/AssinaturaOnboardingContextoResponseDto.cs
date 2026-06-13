namespace GLOWAPI.Application.DTOs.Assinaturas;

public record EstabelecimentoOnboardingContextoDto(
    int EstabelecimentoId,
    string Nome,
    string? Logo,
    bool AssinaturaAtiva,
    bool AssinaturaPendente,
    bool PodeContratar);

public record AssinaturaOnboardingContextoResponseDto(
    bool TemEstabelecimentoProprio,
    IReadOnlyList<EstabelecimentoOnboardingContextoDto> Estabelecimentos,
    string ProximaEtapa,
    int? EstabelecimentoIdSugerido,
    bool PodeAdicionarLoja,
    int LojasVinculadas,
    int? LimiteLojas,
    int? AssinaturaPremiumId);
