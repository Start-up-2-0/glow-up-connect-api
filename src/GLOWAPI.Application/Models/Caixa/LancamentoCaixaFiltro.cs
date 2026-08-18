namespace GLOWAPI.Application.Models.Caixa;

public record LancamentoCaixaFiltro(
    int CaixaId,
    DateTime? Inicio,
    DateTime? Fim,
    string? Q = null,
    string? Tipo = null,
    string? Status = null,
    int Pagina = 1,
    int TamanhoPagina = 20);
