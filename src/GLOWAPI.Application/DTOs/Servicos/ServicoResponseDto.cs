using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Servicos;

public class ServicoResponseDto
{
    public int Id { get; set; }
    public int EstabelecimentoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal PrecoBase { get; set; }
    public int DuracaoMinutos { get; set; }
    public bool Ativo { get; set; }
    public IReadOnlyList<ServicoProfissionalResumoDto> Profissionais { get; set; } = [];

    public static ServicoResponseDto From(Servico servico)
    {
        return new ServicoResponseDto
        {
            Id = servico.Id,
            EstabelecimentoId = servico.EstabelecimentoId!.Value,
            Nome = servico.Nome,
            Descricao = servico.Descricao,
            PrecoBase = servico.PrecoBase,
            DuracaoMinutos = servico.DuracaoMinutos,
            Ativo = servico.Ativo,
            Profissionais = servico.Profissionais
                .Select(vinculo => new ServicoProfissionalResumoDto
                {
                    ProfissionalId = vinculo.ProfissionalId,
                    Preco = vinculo.Preco,
                    DuracaoMinutos = vinculo.DuracaoMinutos,
                    Ativo = vinculo.Ativo
                })
                .OrderBy(vinculo => vinculo.ProfissionalId)
                .ToList()
        };
    }
}
