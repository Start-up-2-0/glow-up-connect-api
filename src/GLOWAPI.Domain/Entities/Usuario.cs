using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int Tentativas { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<EstabelecimentoUsuario> Estabelecimentos { get; set; } = new List<EstabelecimentoUsuario>();
    public ICollection<Agendamento> Agendamentos { get; set; } = new List<Agendamento>();
}