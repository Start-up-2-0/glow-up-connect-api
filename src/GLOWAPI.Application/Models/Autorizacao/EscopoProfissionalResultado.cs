namespace GLOWAPI.Application.Models.Autorizacao;

public record EscopoProfissionalResultado(
    int EstabelecimentoId,
    int UsuarioId,
    int ProfissionalId,
    int ProfissionalEstabelecimentoId,
    bool PodeReceberAgendamento);
