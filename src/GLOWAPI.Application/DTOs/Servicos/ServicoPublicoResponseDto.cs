using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Servicos;

public class ServicoPublicoResponseDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal PrecoMinimo { get; set; }
    public decimal PrecoMaximo { get; set; }
    public int DuracaoMinutosBase { get; set; }
    public int DuracaoMinutosEstimada { get; set; }

    public static ServicoPublicoResponseDto From(Servico servico)
    {
        var vinculosAtivos = servico.Profissionais.Where(vinculo => vinculo.Ativo).ToList();
        var precos = vinculosAtivos.Select(vinculo => vinculo.Preco).ToList();
        var duracoes = vinculosAtivos.Select(vinculo => vinculo.DuracaoMinutos).ToList();

        if (precos.Count == 0)
        {
            precos.Add(servico.PrecoBase);
            duracoes.Add(servico.DuracaoMinutos);
        }

        return new ServicoPublicoResponseDto
        {
            Id = servico.Id,
            Nome = servico.Nome,
            Descricao = servico.Descricao,
            PrecoMinimo = precos.Min(),
            PrecoMaximo = precos.Max(),
            DuracaoMinutosBase = servico.DuracaoMinutos,
            DuracaoMinutosEstimada = duracoes.Min()
        };
    }
}
