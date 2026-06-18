namespace GLOWAPI.Application.DTOs.Assinaturas;

public class AdicionarEstabelecimentoAssinaturaRequestDto
{
    public CriarEstabelecimentoAssinaturaDto Estabelecimento { get; set; } = null!;
}

public record AdicionarEstabelecimentoAssinaturaResponseDto(
    int EstabelecimentoId,
    string Nome,
    int AssinaturaId);
