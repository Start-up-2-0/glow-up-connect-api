namespace GLOWAPI.Application.DTOs.Auditoria;

public record AuditoriaNegocioResponseDto(
    int Id,
    int EstabelecimentoId,
    int? UsuarioId,
    string TipoAcao,
    string Entidade,
    int? EntidadeId,
    string PayloadJson,
    DateTime CriadoEm);
