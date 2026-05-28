using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class Profissional
{
    public int Id { get; set; }
    public Guid PublicGuid { get; set; } = Guid.NewGuid();
    public int UsuarioId { get; set; }
    public string NomePublico { get; set; } = string.Empty;
    public string Biografia { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ProfessionalType TipoProfissional { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Usuario? Usuario { get; set; }
    public ICollection<ProfissionalEstabelecimento> Estabelecimentos { get; set; } = new List<ProfissionalEstabelecimento>();
    public ICollection<Servico> ServicosAutonomo { get; set; } = new List<Servico>();
    public ICollection<ProfissionalServico> Servicos { get; set; } = new List<ProfissionalServico>();
    public ICollection<HorarioAtendimentoProfissional> HorariosAtendimento { get; set; } = new List<HorarioAtendimentoProfissional>();
    public ICollection<Agendamento> AgendamentosAutonomo { get; set; } = new List<Agendamento>();
    public ICollection<AgendamentoItem> AgendamentoItens { get; set; } = new List<AgendamentoItem>();
    public ICollection<AgendamentoItem> AgendamentoItensRepassados { get; set; } = new List<AgendamentoItem>();
    public ICollection<Assinatura> AssinaturasAutonomo { get; set; } = new List<Assinatura>();
    public ICollection<LancamentoCaixa> LancamentosCaixa { get; set; } = new List<LancamentoCaixa>();
    public Endereco? Endereco { get; set; }
    public Caixa? Caixa { get; set; }
}
