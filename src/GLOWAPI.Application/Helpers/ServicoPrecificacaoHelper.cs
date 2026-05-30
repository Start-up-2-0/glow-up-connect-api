using GLOWAPI.Application.DTOs.Equipe;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Helpers;

public static class ServicoPrecificacaoHelper
{
    public static int ObterDuracaoEfetiva(Servico servico, ProfissionalServico? vinculo)
    {
        if (vinculo?.Ativo == true)
        {
            return vinculo.DuracaoMinutos;
        }

        return servico.DuracaoMinutos;
    }

    public static decimal ObterPrecoEfetivo(Servico servico, ProfissionalServico? vinculo)
    {
        if (vinculo?.Ativo == true)
        {
            return vinculo.Preco;
        }

        return servico.PrecoBase;
    }

    public static int ObterDuracaoEfetiva(Servico servico, ProfissionalServicoResponseDto? vinculo)
    {
        if (vinculo?.Ativo == true)
        {
            return vinculo.DuracaoMinutos;
        }

        return servico.DuracaoMinutos;
    }

    public static decimal ObterPrecoEfetivo(Servico servico, ProfissionalServicoResponseDto? vinculo)
    {
        if (vinculo?.Ativo == true)
        {
            return vinculo.Preco;
        }

        return servico.PrecoBase;
    }
}
