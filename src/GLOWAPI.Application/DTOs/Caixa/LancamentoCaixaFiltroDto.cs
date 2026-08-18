namespace GLOWAPI.Application.DTOs.Caixa;

public class LancamentoCaixaFiltroDto
{
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public string? Q { get; set; }
    public string? Tipo { get; set; }
    public string? Status { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
}
