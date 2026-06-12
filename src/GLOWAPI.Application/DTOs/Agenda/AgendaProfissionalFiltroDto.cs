using GLOWAPI.Application.Helpers;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Agenda;

public class AgendaProfissionalFiltroDto
{
    public AgendamentoStatus? Status { get; set; }
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 12;
    public string Ordenacao { get; set; } = AgendaOrdenacaoConsulta.Padrao;
}
