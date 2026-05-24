using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Tests.Helpers;

public static class UsuarioBuilder
{
    public static Usuario Criar(
        int id = 1,
        string email = "usuario@email.com",
        string senhaHash = "$2a$11$abcdefghijklmnopqrstuv", // placeholder; use BCrypt nos testes de integração
        bool ativo = true,
        int tentativas = 0,
        UserRole role = UserRole.Cliente)
    {
        return new Usuario
        {
            Id = id,
            Nome = "Usuario Teste",
            Email = email,
            Telefone = "11999999999",
            Senha = senhaHash,
            Role = role,
            Tentativas = tentativas,
            Ativo = ativo
        };
    }
}
