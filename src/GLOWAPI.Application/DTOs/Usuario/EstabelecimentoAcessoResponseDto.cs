using System.Text.Json.Serialization;
using GLOWAPI.Application.DTOs.Assinaturas;

namespace GLOWAPI.Application.DTOs.Usuario;

public record EstabelecimentoAcessoResponseDto(
    int EstabelecimentoId,
    Guid PublicGuid,
    string Nome,
    string Logo,
    string Role,
    bool PossuiVinculoProfissional,
    int? ProfissionalId,
    Guid? ProfissionalPublicGuid,
    IReadOnlyList<string> Permissoes,
    bool AssinaturaAtiva,
    int? AssinaturaId,
    int? PlanoId,
    string? PlanoNome,
    string? AssinaturaStatus,
    bool EmTrial,
    int? DiasTrial,
    DateTime? ProximaDataVencimento,
    IReadOnlyList<string> Modulos,
    LimitesAssinaturaDto Limites,
    string? TipoAssinatura = null,
    [property: JsonPropertyName("categoriaId")] int? CategoriaEstabelecimentoId = null,
    [property: JsonPropertyName("categoria")] string? CategoriaEstabelecimento = null,
    bool OnboardingObrigatorioPendente = false,
    string? ProximaEtapaOnboarding = null);
