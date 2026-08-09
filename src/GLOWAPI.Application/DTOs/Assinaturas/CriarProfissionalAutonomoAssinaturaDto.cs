namespace GLOWAPI.Application.DTOs.Assinaturas;

using GLOWAPI.Application.DTOs.Operacoes;

public class CriarProfissionalAutonomoAssinaturaDto
{
    public string NomePublico { get; set; } = string.Empty;
    public string Biografia { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    /// <summary>Área de atuação (mesmo catálogo de categorias do marketplace).</summary>
    public int? CategoriaEstabelecimentoId { get; set; }
    public EnderecoOperacaoDto Endereco { get; set; } = new();
}
