namespace GLOWAPI.Domain.Entities;

public class AvaliacaoHistorico
{
    public int Id { get; set; }
    public int AvaliacaoAtendimentoId { get; set; }
    public byte NotaEstabelecimentoAnterior { get; set; }
    public string? ComentarioEstabelecimentoAnterior { get; set; }
    public byte NotaProfissionalAnterior { get; set; }
    public string? ComentarioProfissionalAnterior { get; set; }
    public int? AlteradoPorUsuarioId { get; set; }
    public string? Motivo { get; set; }
    public DateTime AlteradoEm { get; set; } = DateTime.UtcNow;

    public AvaliacaoAtendimento? AvaliacaoAtendimento { get; set; }
    public Usuario? AlteradoPorUsuario { get; set; }
}
