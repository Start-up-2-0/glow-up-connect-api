using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Models.Assinaturas;

public record AssinaturaOnboardingPendentePayload(
    int UsuarioId,
    TipoAssinatura TipoAssinatura,
    CriarEstabelecimentoAssinaturaDto? Estabelecimento,
    CriarProfissionalAutonomoAssinaturaDto? ProfissionalAutonomo);
