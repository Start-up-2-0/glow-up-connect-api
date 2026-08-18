using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.DTOs.Equipe;

public class EquipeMembrosFiltroDto
{
    public string? Busca { get; set; }
    public string? Cargo { get; set; }
    public string? Status { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 6;
}

public record EquipeMembrosResumoDto(
    int TotalMembros,
    int Administradores,
    int Profissionais,
    int Recepcionistas,
    int Convidados);

public record MembroEquipeResponseDto(
    string Id,
    string Tipo,
    string Nome,
    string Cargo,
    string Role,
    string? Email,
    string? Telefone,
    bool Ativo,
    int? UsuarioId,
    int? ProfissionalId,
    bool? PodeReceberAgendamento,
    string? Foto,
    string? ConviteEm);

public record EquipeMembrosPaginadoResponseDto(
    int Total,
    int Pagina,
    int TamanhoPagina,
    IReadOnlyList<MembroEquipeResponseDto> Itens,
    EquipeMembrosResumoDto Resumo);
