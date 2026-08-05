namespace GLOWAPI.Application.DTOs.Assinaturas;

using GLOWAPI.Application.DTOs.Operacoes;

public class CriarEstabelecimentoAssinaturaDto
{
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int? CategoriaEstabelecimentoId { get; set; }
    public EnderecoOperacaoDto Endereco { get; set; } = new();
}
