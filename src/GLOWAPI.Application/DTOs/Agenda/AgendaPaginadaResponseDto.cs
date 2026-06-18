namespace GLOWAPI.Application.DTOs.Agenda;

public record AgendaPaginadaResponseDto<T>(
    int Total,
    int Pagina,
    int TamanhoPagina,
    IReadOnlyList<T> Itens);
