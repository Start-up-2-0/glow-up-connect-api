using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Equipe;

public class ProfissionalServicoResponseDto
{
    public int Id { get; set; }
    public int ProfissionalId { get; set; }
    public int ServicoId { get; set; }
    public decimal Preco { get; set; }
    public int DuracaoMinutos { get; set; }
    public bool Ativo { get; set; }

    public static ProfissionalServicoResponseDto From(ProfissionalServico vinculo)
    {
        return new ProfissionalServicoResponseDto
        {
            Id = vinculo.Id,
            ProfissionalId = vinculo.ProfissionalId,
            ServicoId = vinculo.ServicoId,
            Preco = vinculo.Preco,
            DuracaoMinutos = vinculo.DuracaoMinutos,
            Ativo = vinculo.Ativo
        };
    }
}
