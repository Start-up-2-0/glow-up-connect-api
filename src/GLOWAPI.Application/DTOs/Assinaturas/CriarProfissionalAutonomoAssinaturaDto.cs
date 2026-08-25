using System.Text.Json.Serialization;
using GLOWAPI.Application.DTOs.Operacoes;

namespace GLOWAPI.Application.DTOs.Assinaturas;

public class CriarProfissionalAutonomoAssinaturaDto
{
    public string NomePublico { get; set; } = string.Empty;
    public string Biografia { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Categoria do ofício (id do catálogo filtrado por profissional autônomo).</summary>
    [JsonPropertyName("categoriaId")]
    public int? CategoriaEstabelecimentoId { get; set; }

    public EnderecoOperacaoDto Endereco { get; set; } = new();
    public IReadOnlyList<int> ComodidadeIds { get; set; } = Array.Empty<int>();
}
