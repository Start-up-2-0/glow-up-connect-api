using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Helpers;

public static class ServicoExecucaoHelper
{
    public static bool ServicoPossuiVinculosAtivos(Servico servico) =>
        servico.Profissionais.Any(vinculo => vinculo.Ativo);

    public static bool ProfissionalExecutaServico(Servico servico, int profissionalId)
    {
        if (!ServicoPossuiVinculosAtivos(servico))
        {
            return true;
        }

        return servico.Profissionais.Any(vinculo =>
            vinculo.Ativo && vinculo.ProfissionalId == profissionalId);
    }
}
