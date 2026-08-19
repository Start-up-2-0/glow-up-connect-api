namespace GLOWAPI.Application.DTOs.Assinaturas;

public record OnboardingEtapaStatusDto(
    string Id,
    string Titulo,
    bool Concluida,
    IReadOnlyList<string> Pendencias);

public record OnboardingPublicacaoStatusDto(
    int EstabelecimentoId,
    string TipoAssinatura,
    bool ProntoParaPublicacao,
    bool VisivelPublicamente,
    bool OnboardingObrigatorioPendente,
    string? ProximaEtapa,
    IReadOnlyList<OnboardingEtapaStatusDto> Etapas);
