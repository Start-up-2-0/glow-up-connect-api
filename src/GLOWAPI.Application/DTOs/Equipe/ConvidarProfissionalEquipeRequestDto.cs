namespace GLOWAPI.Application.DTOs.Equipe;

public class ConvidarProfissionalEquipeRequestDto
{
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? NomePublico { get; set; }
    public string? Biografia { get; set; }

    /// <summary>
    /// Foto de apresentação do profissional (data URI ou base64).
    /// Não altera o avatar da conta do usuário.
    /// </summary>
    public string? Foto { get; set; }

    public string? FotoContentType { get; set; }

    /// <summary>Alias legado — use <see cref="Foto"/>.</summary>
    public string? Logo { get; set; }

    public bool PodeReceberAgendamento { get; set; } = true;

    public string? ResolverFoto() =>
        !string.IsNullOrWhiteSpace(Foto) ? Foto : Logo;
}
