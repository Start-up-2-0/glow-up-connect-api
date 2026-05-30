namespace GLOWAPI.Application.DTOs.Usuario;

public record EstabelecimentoAcessoResponseDto(
    int EstabelecimentoId,
    Guid PublicGuid,
    string Nome,
    string Logo,
    string Role,
    bool PossuiVinculoProfissional,
    IReadOnlyList<string> Permissoes,
    bool AssinaturaAtiva,
    int? AssinaturaId,
    int? PlanoId,
    string? PlanoNome,
    IReadOnlyList<string> Modulos);
