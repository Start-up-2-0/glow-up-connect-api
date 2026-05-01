using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class Agendamento
{
    public int Id { get; set; }
    public int UsuarioClienteId { get; set; }
    public int? EstabelecimentoId { get; set; }
    public int? ProfissionalAutonomoId { get; set; }
    public AgendamentoStatus Status { get; set; } = AgendamentoStatus.PendentePagamento;
    public decimal ValorTotal { get; set; }
    public string Observacao { get; set; } = string.Empty;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CanceladoEm { get; set; }

    public Usuario? UsuarioCliente { get; set; }
    public Estabelecimento? Estabelecimento { get; set; }
    public Profissional? ProfissionalAutonomo { get; set; }
    public ICollection<AgendamentoItem> Itens { get; set; } = new List<AgendamentoItem>();
}
