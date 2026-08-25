using System.Text.Json.Serialization;
using GLOWAPI.Application.DTOs.Operacoes;

namespace GLOWAPI.Application.DTOs.Assinaturas;

public class CriarEstabelecimentoAssinaturaDto
{
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Aceita <c>categoriaId</c> no JSON do frontend.</summary>
    [JsonPropertyName("categoriaId")]
    public int? CategoriaEstabelecimentoId { get; set; }

    public EnderecoOperacaoDto Endereco { get; set; } = new();
    public IReadOnlyList<int> ComodidadeIds { get; set; } = Array.Empty<int>();
}
