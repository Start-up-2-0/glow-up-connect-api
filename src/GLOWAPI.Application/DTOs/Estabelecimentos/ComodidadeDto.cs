namespace GLOWAPI.Application.DTOs.Estabelecimentos;

public record ComodidadeDto(int Id, string Nome, string Slug, string Icone, int Ordem);

public class AtualizarComodidadesRequestDto
{
    public IReadOnlyList<int> ComodidadeIds { get; set; } = Array.Empty<int>();
}
