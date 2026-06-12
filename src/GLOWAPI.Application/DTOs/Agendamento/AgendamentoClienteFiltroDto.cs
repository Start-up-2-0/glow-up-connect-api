using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Agendamento;

public class AgendamentoClienteFiltroDto
{
    public AgendamentoStatus? Status { get; set; }
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public Guid? EstabelecimentoPublicGuid { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
    public string Ordenacao { get; set; } = "atendimento_desc";
}
