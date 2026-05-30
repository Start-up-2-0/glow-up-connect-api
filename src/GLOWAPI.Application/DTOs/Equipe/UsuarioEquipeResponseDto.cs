using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Equipe;

public record UsuarioEquipeResponseDto(
    int Id,
    int EstabelecimentoId,
    int UsuarioId,
    string Nome,
    string Email,
    string Telefone,
    EstablishmentUserRole Role,
    bool Ativo)
{
    public static UsuarioEquipeResponseDto From(
        GLOWAPI.Domain.Entities.EstabelecimentoUsuario vinculo,
        GLOWAPI.Domain.Entities.Usuario usuario) =>
        new(
            vinculo.Id,
            vinculo.EstabelecimentoId,
            usuario.Id,
            usuario.Nome,
            usuario.Email,
            usuario.Telefone,
            vinculo.RoleNoEstabelecimento,
            vinculo.Ativo);
}
