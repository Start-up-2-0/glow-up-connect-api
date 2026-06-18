namespace GLOWAPI.Application.DTOs.Clientes;

public record ClienteNegocioResponseDto(
    string Nome,
    string? Email,
    string? Telefone,
    int TotalAgendamentos,
    DateTime? UltimoAgendamentoEm);
