namespace GLOWAPI.Application.DTOs.Assinaturas;

public record LimitesAssinaturaDto(
    int? Profissionais,
    int? Servicos,
    int? Agendamentos,
    int? Usuarios,
    int? AgendamentosPorDia,
    bool PrioridadeListagemPublica);
