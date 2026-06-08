namespace GLOWAPI.Application.DTOs.Agendamento;

public class PropostaRemarcacaoResponseDto
{
    public int Id { get; set; }
    public int AgendamentoId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DataSugerida { get; set; } = string.Empty;
    public string HorarioInicioSugerido { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string? InicioAtual { get; set; }
    public string? EstabelecimentoNome { get; set; }
    public string? ProfissionalNome { get; set; }
    public Guid TokenPublico { get; set; }
    public DateTime ExpiraEm { get; set; }

    public static PropostaRemarcacaoResponseDto From(
        Domain.Entities.AgendamentoPropostaRemarcacao proposta,
        Domain.Entities.Agendamento agendamento,
        Domain.Entities.Profissional? profissional)
    {
        var inicioAtual = agendamento.Itens.OrderBy(item => item.Inicio).FirstOrDefault()?.Inicio;

        return new PropostaRemarcacaoResponseDto
        {
            Id = proposta.Id,
            AgendamentoId = proposta.AgendamentoId,
            Status = proposta.Status.ToString(),
            DataSugerida = proposta.DataSugerida.ToString("yyyy-MM-dd"),
            HorarioInicioSugerido = proposta.HorarioInicioSugerido.ToString("HH:mm"),
            Motivo = proposta.Motivo,
            InicioAtual = inicioAtual?.ToString("O"),
            EstabelecimentoNome = agendamento.Estabelecimento?.Nome,
            ProfissionalNome = profissional?.NomePublico,
            TokenPublico = proposta.TokenPublico,
            ExpiraEm = proposta.ExpiraEm
        };
    }
}
