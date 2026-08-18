namespace GLOWAPI.Application.DTOs.Caixa;

public record LancamentoCaixaPaginadoResponseDto(
    int Total,
    int Pagina,
    int TamanhoPagina,
    IReadOnlyList<LancamentoCaixaResponseDto> Itens);
