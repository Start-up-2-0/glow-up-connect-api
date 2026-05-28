using GLOWAPI.Application.DTOs.Operacoes;

namespace GLOWAPI.Application.DTOs.Profissionais;

public class AtualizarProfissionalAutonomoPerfilDto
{
    public string NomePublico { get; set; } = string.Empty;
    public string Logo { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public EnderecoOperacaoDto Endereco { get; set; } = new();
}
