using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.DTOs.Planos;

public record PlanoResponseDto(
    int Id,
    string Nome,
    string Descricao,
    decimal Preco,
    string Periodo,
    int? LimiteProfissionais,
    int? LimiteServicos,
    int? LimiteAgendamentos,
    int? LimiteUsuarios,
    int? LimiteAgendamentosPorDia,
    int? LimiteEstabelecimentos,
    bool PrioridadeListagemPublica,
    IReadOnlyList<string> Modulos,
    IReadOnlyList<string> Funcionalidades)
{
    public static PlanoResponseDto From(Plano plano)
    {
        var perfil = PlanoComercialCatalogo.Obter(plano);

        return new(
            plano.Id,
            plano.Nome,
            plano.Descricao,
            plano.Preco,
            plano.Periodo.ToString(),
            plano.LimiteProfissionais,
            plano.LimiteServicos,
            plano.LimiteAgendamentos,
            perfil.LimiteUsuarios,
            perfil.LimiteAgendamentosPorDia,
            plano.LimiteEstabelecimentos,
            perfil.PrioridadeListagemPublica,
            perfil.Modulos.Select(modulo => modulo.ToString()).ToList(),
            perfil.Funcionalidades);
    }
}
