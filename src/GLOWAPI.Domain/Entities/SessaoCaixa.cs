using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class SessaoCaixa
{
    public int Id { get; set; }
    public int CaixaId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime AbertoEm { get; set; }
    public DateTime? FechadoEm { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal? SaldoInformadoFechamento { get; set; }
    public decimal? Diferenca { get; set; }
    public SessaoCaixaStatus Status { get; set; } = SessaoCaixaStatus.Aberta;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Caixa? Caixa { get; set; }
    public Usuario? Usuario { get; set; }
    public ICollection<LancamentoCaixa> Lancamentos { get; set; } = new List<LancamentoCaixa>();
}
