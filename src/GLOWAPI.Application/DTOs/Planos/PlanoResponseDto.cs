using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

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
    public static PlanoResponseDto From(
        Plano plano,
        TipoAssinatura tipoAssinatura = TipoAssinatura.Estabelecimento)
    {
        var perfil = PlanoComercialCatalogo.Obter(plano, tipoAssinatura);

        return new(
            plano.Id,
            plano.Nome,
            PlanoComercialCatalogo.ResolverDescricao(plano, tipoAssinatura),
            PlanoComercialCatalogo.ResolverPreco(plano, tipoAssinatura),
            plano.Periodo.ToString(),
            perfil.LimiteProfissionaisEfetivo ?? plano.LimiteProfissionais,
            plano.LimiteServicos,
            plano.LimiteAgendamentos,
            perfil.LimiteUsuarios,
            perfil.LimiteAgendamentosPorDia,
            perfil.LimiteEstabelecimentosEfetivo ?? plano.LimiteEstabelecimentos,
            perfil.PrioridadeListagemPublica,
            perfil.Modulos.Select(modulo => modulo.ToString()).ToList(),
            perfil.Funcionalidades);
    }
}
