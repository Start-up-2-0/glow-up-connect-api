using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class AvaliacaoAtendimento
{
    public int Id { get; set; }
    public int AgendamentoId { get; set; }
    public int? UsuarioClienteId { get; set; }
    public int EstabelecimentoId { get; set; }
    public int ProfissionalId { get; set; }
    public byte NotaEstabelecimento { get; set; }
    public string? ComentarioEstabelecimento { get; set; }
    public byte NotaProfissional { get; set; }
    public string? ComentarioProfissional { get; set; }
    public DateTime AvaliadoEm { get; set; } = DateTime.UtcNow;
    public AvaliacaoOrigem Origem { get; set; } = AvaliacaoOrigem.Historico;

    public Agendamento? Agendamento { get; set; }
    public Usuario? UsuarioCliente { get; set; }
    public Estabelecimento? Estabelecimento { get; set; }
    public Profissional? Profissional { get; set; }
}
