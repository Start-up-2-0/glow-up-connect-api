namespace GLOWAPI.Domain.Entities;

public class Estabelecimento
{
    public int Id { get; set; }
    public Guid PublicGuid { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public bool VisivelPublicamente { get; set; } = true;
    public DateTime? WhatsAppConfirmadoEm { get; set; }
    public string? WhatsAppConfirmacaoTokenHash { get; set; }
    public string? WhatsAppConfirmacaoCodigoHash { get; set; }
    public DateTime? WhatsAppConfirmacaoExpiraEm { get; set; }
    public bool WhatsAppOptIn { get; set; }
    public decimal? NotaMedia { get; set; }
    public int TotalAvaliacoes { get; set; }
    public DateTime CreateAd { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<EstabelecimentoUsuario> Usuarios { get; set; } = new List<EstabelecimentoUsuario>();
    public ICollection<ProfissionalEstabelecimento> Profissionais { get; set; } = new List<ProfissionalEstabelecimento>();
    public ICollection<Servico> Servicos { get; set; } = new List<Servico>();
    public ICollection<HorarioFuncionamentoEstabelecimento> HorariosFuncionamento { get; set; } = new List<HorarioFuncionamentoEstabelecimento>();
    public ICollection<HorarioAtendimentoProfissional> HorariosProfissionais { get; set; } = new List<HorarioAtendimentoProfissional>();
    public ICollection<Agendamento> Agendamentos { get; set; } = new List<Agendamento>();
    public ICollection<Assinatura> Assinaturas { get; set; } = new List<Assinatura>();
    public Endereco? Endereco { get; set; }
    public Caixa? Caixa { get; set; }

    public bool PendenteConfirmacaoWhatsApp() =>
        !string.IsNullOrEmpty(WhatsAppConfirmacaoTokenHash) || !string.IsNullOrEmpty(WhatsAppConfirmacaoCodigoHash);

    public void LimparConfirmacaoWhatsApp()
    {
        WhatsAppConfirmacaoTokenHash = null;
        WhatsAppConfirmacaoCodigoHash = null;
        WhatsAppConfirmacaoExpiraEm = null;
    }

    public bool PodeReceberAlertasWhatsApp() =>
        WhatsAppConfirmadoEm.HasValue
        && WhatsAppOptIn
        && !string.IsNullOrWhiteSpace(Telefone);
}
