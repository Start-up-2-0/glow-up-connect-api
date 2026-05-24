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
    public int Tentativas { get; set; }
    public DateTime? BloqueadoAte { get; set; }
    public bool Ativo { get; set; } = true;
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
}
