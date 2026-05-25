using UsuarioEntity = GLOWAPI.Domain.Entities.Usuario;

namespace GLOWAPI.Application.DTOs.Usuario;

public class CadastroClienteResponseDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public string? AvatarBase64 { get; set; }
    public string Mensagem { get; set; } = "Cadastro realizado. Confirme seu e-mail (link ou codigo) para ativar a conta.";

    public static CadastroClienteResponseDto From(UsuarioEntity usuario) => new()
    {
        Id = usuario.Id,
        Nome = usuario.Nome,
        Email = usuario.Email,
        Telefone = usuario.Telefone,
        Ativo = usuario.Ativo,
        AvatarBase64 = usuario.AvatarBase64
    };
}
