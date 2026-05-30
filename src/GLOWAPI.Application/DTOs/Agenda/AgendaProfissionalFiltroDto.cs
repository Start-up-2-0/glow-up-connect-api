using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Agenda;

public class AgendaProfissionalFiltroDto
{
    public AgendamentoItemStatus? Status { get; set; }
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
}
