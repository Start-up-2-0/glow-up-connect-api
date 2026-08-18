namespace GLOWAPI.Application.DTOs.Avaliacao;

public record CriarAvaliacaoAtendimentoRequestDto(
    int NotaEstabelecimento,
    string? ComentarioEstabelecimento,
    int NotaProfissional,
    string? ComentarioProfissional);

public record AvaliacaoResumoClienteDto(
    int NotaEstabelecimento,
    int NotaProfissional,
    DateTime AvaliadoEm);

public record AvaliacaoContextoResponseDto(
    string Status,
    int AgendamentoId,
    Guid EstabelecimentoPublicGuid,
    string EstabelecimentoNome,
    string EstabelecimentoLogo,
    int ProfissionalId,
    string ProfissionalNome,
    string ProfissionalLogo,
    DateTime AtendimentoInicio,
    DateTime AtendimentoFim,
    AvaliacaoResumoClienteDto? Avaliacao);

public record AvaliacaoDistribuicaoItemDto(
    int Nota,
    int Quantidade);

public record AvaliacaoResumoPublicoDto(
    decimal NotaMedia,
    int TotalAvaliacoes,
    int JanelaDias,
    IReadOnlyList<AvaliacaoDistribuicaoItemDto> Distribuicao);

public record AvaliacaoComentarioItemDto(
    int Nota,
    string? Comentario,
    DateTime AvaliadoEm,
    string ClienteNome);

public record AvaliacoesPaginadasResponseDto(
    AvaliacaoResumoPublicoDto Resumo,
    int Total,
    int Pagina,
    int TamanhoPagina,
    IReadOnlyList<AvaliacaoComentarioItemDto> Itens);

public record AvaliacaoNegocioItemDto(
    int Id,
    int AgendamentoId,
    int NotaEstabelecimento,
    string? ComentarioEstabelecimento,
    int NotaProfissional,
    string? ComentarioProfissional,
    DateTime AvaliadoEm,
    string ClienteNome,
    string ProfissionalNome);

public record AvaliacoesNegocioPaginadasResponseDto(
    AvaliacaoResumoPublicoDto Resumo,
    int Total,
    int Pagina,
    int TamanhoPagina,
    IReadOnlyList<AvaliacaoNegocioItemDto> Itens);
