using GLOWAPI.Application.Helpers;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Agenda;

public class AgendaGeralFiltroDto
{
    public int? ProfissionalId { get; set; }
    public int? ClienteId { get; set; }
    public AgendamentoStatus? Status { get; set; }
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public bool IntervaloPersonalizado { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 12;
    public string Ordenacao { get; set; } = AgendaOrdenacaoConsulta.Padrao;
}
