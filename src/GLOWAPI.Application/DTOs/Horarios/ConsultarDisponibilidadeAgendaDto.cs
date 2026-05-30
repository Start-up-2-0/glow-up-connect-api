namespace GLOWAPI.Application.DTOs.Horarios;

public class ConsultarDisponibilidadeAgendaDto
{
    public DateOnly DataInicio { get; set; }
    public DateOnly DataFim { get; set; }
    public int ServicoId { get; set; }
    public int[] ServicoIds { get; set; } = [];
    public int? ProfissionalId { get; set; }

    public int[] ObterServicoIdsEfetivos()
    {
        if (ServicoIds.Length > 0)
        {
            return ServicoIds;
        }

        if (ServicoId > 0)
        {
            return [ServicoId];
        }

        return [];
    }
}
