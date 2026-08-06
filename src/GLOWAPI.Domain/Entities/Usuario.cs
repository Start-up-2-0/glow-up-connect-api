using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Sexo? Sexo { get; set; }
    public int Tentativas { get; set; }
    public DateTime? BloqueadoAte { get; set; }
    public bool Ativo { get; set; } = true;
    public string? AvatarBase64 { get; set; }
    public string? ConfirmacaoTokenHash { get; set; }
    public string? ConfirmacaoCodigoHash { get; set; }
    public DateTime? ConfirmacaoExpiraEm { get; set; }
    public DateTime? WhatsAppConfirmadoEm { get; set; }
    public string? WhatsAppConfirmacaoTokenHash { get; set; }
    public string? WhatsAppConfirmacaoCodigoHash { get; set; }
    public DateTime? WhatsAppConfirmacaoExpiraEm { get; set; }
    public bool WhatsAppOptIn { get; set; }
    /// <summary>Código pessoal para autenticação no link público de agendamento (único).</summary>
    public string? CodigoAgendamento { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<EstabelecimentoUsuario> Estabelecimentos { get; set; } = new List<EstabelecimentoUsuario>();
    public ICollection<Agendamento> Agendamentos { get; set; } = new List<Agendamento>();
    public ICollection<SessaoAutenticacao> Sessoes { get; set; } = new List<SessaoAutenticacao>();

    public void RegistrarTentativaFalha()
    {
        Tentativas++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetarTentativas()
    {
        Tentativas = 0;
        BloqueadoAte = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool EstaBloqueado(int max) =>
        (BloqueadoAte.HasValue && BloqueadoAte > DateTime.UtcNow)
        || Tentativas >= max;

    public void AplicarBloqueioTemporario(int lockoutMinutes)
    {
        BloqueadoAte = DateTime.UtcNow.AddMinutes(lockoutMinutes);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool PodeAutenticar(int max) => Ativo && !EstaBloqueado(max);

    public bool PodeAutenticarOnboarding(int max) =>
        !EstaBloqueado(max) && (Ativo || PendenteConfirmacaoEmail());

    public bool PendenteConfirmacaoEmail() =>
        !string.IsNullOrEmpty(ConfirmacaoTokenHash) || !string.IsNullOrEmpty(ConfirmacaoCodigoHash);

    public void LimparConfirmacaoEmail()
    {
        ConfirmacaoTokenHash = null;
        ConfirmacaoCodigoHash = null;
        ConfirmacaoExpiraEm = null;
    }

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
