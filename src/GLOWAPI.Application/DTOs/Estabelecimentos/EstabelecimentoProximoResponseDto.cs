namespace GLOWAPI.Application.DTOs.Estabelecimentos;

public record EnderecoResumoDto(
    string Logradouro,
    string Bairro,
    string Cidade,
    string Estado);

public record EstabelecimentoProximoResponseDto(
    Guid PublicGuid,
    string Nome,
    string Logo,
    string Descricao,
    double DistanciaKm,
    bool DestaqueMarketplace,
    EnderecoResumoDto Endereco,
    decimal? NotaMedia = null,
    int TotalAvaliacoes = 0);

public record EstabelecimentosProximosPaginadoResponseDto(
    string Cidade,
    string Estado,
    double RaioKm,
    int Total,
    IReadOnlyList<EstabelecimentoProximoResponseDto> Itens);
