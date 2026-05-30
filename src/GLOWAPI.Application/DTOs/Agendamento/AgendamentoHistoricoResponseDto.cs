using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Agendamento;

public class AgendamentoHistoricoResponseDto
{
    public int Id { get; set; }
    public int? UsuarioExecutorId { get; set; }
    public string? UsuarioExecutorNome { get; set; }
    public string StatusAnterior { get; set; } = string.Empty;
    public string StatusNovo { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public DateTime CriadoEm { get; set; }

    public static AgendamentoHistoricoResponseDto From(AgendamentoHistorico historico) =>
        new()
        {
            Id = historico.Id,
            UsuarioExecutorId = historico.UsuarioExecutorId,
            UsuarioExecutorNome = historico.UsuarioExecutor?.Nome,
            StatusAnterior = historico.StatusAnterior.ToString(),
            StatusNovo = historico.StatusNovo.ToString(),
            Motivo = historico.Motivo,
            CriadoEm = historico.CriadoEm
        };
}
