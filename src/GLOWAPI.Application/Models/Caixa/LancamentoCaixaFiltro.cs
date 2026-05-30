namespace GLOWAPI.Application.Models.Caixa;

public record LancamentoCaixaFiltro(
    int CaixaId,
    DateTime? Inicio,
    DateTime? Fim);
