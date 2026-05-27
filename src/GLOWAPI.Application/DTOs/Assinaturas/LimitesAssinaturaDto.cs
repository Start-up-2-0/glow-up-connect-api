namespace GLOWAPI.Application.DTOs.Assinaturas;

public record LimitesAssinaturaDto(
    int? Profissionais,
    int? Servicos,
    int? Agendamentos);
