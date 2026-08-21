using System.Globalization;
using System.Text;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Services;

public static class PlanoComercialCatalogo
{
    public const decimal PrecoAutonomoEssencial = 49.99m;
    public const decimal PrecoAutonomoPremium = 79.99m;

    private static readonly IReadOnlyList<string> FuncionalidadesBasic =
    [
        "Cadastro de serviços",
        "Agenda simples",
        "Configuração de horários",
        "Página pública básica",
        "Gestão simples de clientes",
        "Confirmação por e-mail",
        "Cancelamento por e-mail",
        "Lembrete por e-mail"
    ];

    private static readonly IReadOnlyList<string> FuncionalidadesPlus =
    [
        .. FuncionalidadesBasic,
        "Multiusuário",
        "Agenda compartilhada",
        "Gestão de profissionais",
        "Histórico de clientes",
        "Confirmação automática via WhatsApp",
        "Lembrete automático de agendamento",
        "Aviso de cancelamento",
        "Dashboard básico",
        "Relatórios básicos"
    ];

    private static readonly IReadOnlyList<string> FuncionalidadesEssencial =
    [
        .. FuncionalidadesPlus,
        "Operação completa para uma unidade"
    ];

    private static readonly IReadOnlyList<string> FuncionalidadesPremium =
    [
        .. FuncionalidadesEssencial,
        "Até 5 unidades na mesma assinatura",
        "Painel consolidado da rede",
        "Controle de caixa",
        "Fluxo financeiro",
        "Comissão automática",
        "Relatórios financeiros",
        "CRM de clientes",
        "Auditoria de operações",
        "Dashboard avançado",
        "Métricas do estabelecimento",
        "Histórico financeiro",
        "Gestão completa da equipe",
        "Prioridade na busca e listagem do marketplace"
    ];

    private static readonly IReadOnlyList<string> FuncionalidadesAutonomoEssencial =
    [
        "Agenda pessoal",
        "Cadastro de serviços",
        "Configuração de horários",
        "Perfil profissional público",
        "Presença no Explorar Lojas",
        "Gestão de clientes e histórico",
        "Notificações por e-mail",
        "Confirmação e lembrete por e-mail",
        "Histórico de atendimentos"
    ];

    private static readonly IReadOnlyList<string> FuncionalidadesAutonomoPremium =
    [
        .. FuncionalidadesAutonomoEssencial,
        "Confirmação automática via WhatsApp",
        "Lembrete automático de agendamento",
        "Aviso de cancelamento via WhatsApp",
        "Controle de caixa pessoal",
        "Fluxo financeiro",
        "Relatórios financeiros",
        "Dashboard avançado",
        "Prioridade na busca e listagem do marketplace"
    ];

    private static readonly IReadOnlyList<ModuloAssinatura> ModulosBasic =
    [
        ModuloAssinatura.Agenda,
        ModuloAssinatura.Servicos,
        ModuloAssinatura.HorariosAtendimento,
        ModuloAssinatura.Notificacoes,
        ModuloAssinatura.Email
    ];

    private static readonly IReadOnlyList<ModuloAssinatura> ModulosPlus =
    [
        .. ModulosBasic,
        ModuloAssinatura.Profissionais,
        ModuloAssinatura.WhatsApp
    ];

    private static readonly IReadOnlyList<ModuloAssinatura> ModulosEssencial = ModulosPlus;

    private static readonly IReadOnlyList<ModuloAssinatura> ModulosPremium =
    [
        .. ModulosEssencial,
        ModuloAssinatura.Caixa,
        ModuloAssinatura.Financeiro,
        ModuloAssinatura.ComissaoProfissionais,
        ModuloAssinatura.Clientes
    ];

    private static readonly IReadOnlyList<ModuloAssinatura> ModulosAutonomoEssencial =
    [
        ModuloAssinatura.Agenda,
        ModuloAssinatura.Servicos,
        ModuloAssinatura.HorariosAtendimento,
        ModuloAssinatura.Notificacoes,
        ModuloAssinatura.Email,
        ModuloAssinatura.Clientes,
        ModuloAssinatura.ProfissionalAutonomo
    ];

    private static readonly IReadOnlyList<ModuloAssinatura> ModulosAutonomoPremium =
    [
        .. ModulosAutonomoEssencial,
        ModuloAssinatura.WhatsApp,
        ModuloAssinatura.Caixa,
        ModuloAssinatura.Financeiro
    ];

    public static PlanoComercialPerfil Obter(
        Plano? plano,
        TipoAssinatura tipoAssinatura = TipoAssinatura.Estabelecimento)
    {
        if (plano is null)
        {
            return PlanoComercialPerfil.Vazio;
        }

        return tipoAssinatura == TipoAssinatura.ProfissionalAutonomo
            ? ObterPerfilAutonomo(plano)
            : ObterPerfilEstabelecimento(plano);
    }

    public static bool PermiteMultiLoja(Plano? plano, TipoAssinatura tipoAssinatura = TipoAssinatura.Estabelecimento) =>
        tipoAssinatura == TipoAssinatura.Estabelecimento
        && plano?.LimiteEstabelecimentos is > 1;

    public static bool EhPlanoPremium(Plano? plano) =>
        plano is not null && Normalizar(plano.Nome).Contains("premium", StringComparison.Ordinal);

    public static bool EhModuloExclusivoEstabelecimento(ModuloAssinatura modulo) =>
        modulo is ModuloAssinatura.Profissionais
            or ModuloAssinatura.ComissaoProfissionais;

    /// <summary>
    /// Preço comercial efetivo. Autônomo usa tabela própria; loja usa <see cref="Plano.Preco"/>.
    /// </summary>
    public static decimal ResolverPreco(
        Plano? plano,
        TipoAssinatura tipoAssinatura = TipoAssinatura.Estabelecimento)
    {
        if (plano is null)
        {
            return 0m;
        }

        if (tipoAssinatura != TipoAssinatura.ProfissionalAutonomo)
        {
            return plano.Preco;
        }

        var nomeNormalizado = Normalizar(plano.Nome);
        if (nomeNormalizado.Contains("premium", StringComparison.Ordinal))
        {
            return PrecoAutonomoPremium;
        }

        // Essencial e legados (Plus/Basic) para autônomo
        return PrecoAutonomoEssencial;
    }

    /// <summary>
    /// Descrição comercial efetiva com ortografia correta.
    /// Autônomo usa copy sem equipe/comissões/multi-unidade; loja usa textos comerciais acentuados.
    /// </summary>
    public static string ResolverDescricao(
        Plano? plano,
        TipoAssinatura tipoAssinatura = TipoAssinatura.Estabelecimento)
    {
        if (plano is null)
        {
            return string.Empty;
        }

        var nomeNormalizado = Normalizar(plano.Nome);

        if (tipoAssinatura == TipoAssinatura.ProfissionalAutonomo)
        {
            if (nomeNormalizado.Contains("premium", StringComparison.Ordinal))
            {
                return "WhatsApp, caixa pessoal, financeiro e prioridade no marketplace";
            }

            // Essencial e legados (Plus/Basic) para autônomo
            return "Agenda, clientes e perfil público para quem atende sozinho";
        }

        if (nomeNormalizado.Contains("premium", StringComparison.Ordinal))
        {
            return "Caixa, financeiro, comissões, até 5 unidades e prioridade no marketplace";
        }

        if (nomeNormalizado.Contains("essencial", StringComparison.Ordinal))
        {
            return "Operação completa com equipe, WhatsApp e gestão para uma unidade";
        }

        return plano.Descricao;
    }

    private static PlanoComercialPerfil ObterPerfilEstabelecimento(Plano plano)
    {
        var nomeNormalizado = Normalizar(plano.Nome);

        if (nomeNormalizado.Contains("premium", StringComparison.Ordinal))
        {
            return new PlanoComercialPerfil(
                LimiteUsuarios: null,
                LimiteAgendamentosPorDia: null,
                LimiteEstabelecimentosEfetivo: plano.LimiteEstabelecimentos,
                LimiteProfissionaisEfetivo: plano.LimiteProfissionais,
                PrioridadeListagemPublica: true,
                Modulos: ModulosPremium,
                Funcionalidades: FuncionalidadesPremium);
        }

        if (nomeNormalizado.Contains("essencial", StringComparison.Ordinal))
        {
            return new PlanoComercialPerfil(
                LimiteUsuarios: null,
                LimiteAgendamentosPorDia: null,
                LimiteEstabelecimentosEfetivo: plano.LimiteEstabelecimentos ?? 1,
                LimiteProfissionaisEfetivo: plano.LimiteProfissionais,
                PrioridadeListagemPublica: false,
                Modulos: ModulosEssencial,
                Funcionalidades: FuncionalidadesEssencial);
        }

        if (nomeNormalizado.Contains("plus", StringComparison.Ordinal))
        {
            return new PlanoComercialPerfil(
                LimiteUsuarios: null,
                LimiteAgendamentosPorDia: null,
                LimiteEstabelecimentosEfetivo: plano.LimiteEstabelecimentos ?? 1,
                LimiteProfissionaisEfetivo: plano.LimiteProfissionais,
                PrioridadeListagemPublica: false,
                Modulos: ModulosPlus,
                Funcionalidades: FuncionalidadesPlus);
        }

        if (nomeNormalizado.Contains("basic", StringComparison.Ordinal)
            || nomeNormalizado.Contains("basico", StringComparison.Ordinal))
        {
            return new PlanoComercialPerfil(
                LimiteUsuarios: 1,
                LimiteAgendamentosPorDia: null,
                LimiteEstabelecimentosEfetivo: 1,
                LimiteProfissionaisEfetivo: 1,
                PrioridadeListagemPublica: false,
                Modulos: ModulosBasic,
                Funcionalidades: FuncionalidadesBasic);
        }

        return new PlanoComercialPerfil(
            LimiteUsuarios: plano.LimiteProfissionais,
            LimiteAgendamentosPorDia: plano.LimiteAgendamentos,
            LimiteEstabelecimentosEfetivo: plano.LimiteEstabelecimentos ?? 1,
            LimiteProfissionaisEfetivo: plano.LimiteProfissionais,
            PrioridadeListagemPublica: false,
            Modulos: ModulosBasic,
            Funcionalidades: FuncionalidadesBasic);
    }

    private static PlanoComercialPerfil ObterPerfilAutonomo(Plano plano)
    {
        var nomeNormalizado = Normalizar(plano.Nome);

        if (nomeNormalizado.Contains("premium", StringComparison.Ordinal))
        {
            return new PlanoComercialPerfil(
                LimiteUsuarios: 1,
                LimiteAgendamentosPorDia: null,
                LimiteEstabelecimentosEfetivo: 1,
                LimiteProfissionaisEfetivo: 1,
                PrioridadeListagemPublica: true,
                Modulos: ModulosAutonomoPremium,
                Funcionalidades: FuncionalidadesAutonomoPremium);
        }

        // Essencial (e legados Plus/Basic) para autônomo
        return new PlanoComercialPerfil(
            LimiteUsuarios: 1,
            LimiteAgendamentosPorDia: null,
            LimiteEstabelecimentosEfetivo: 1,
            LimiteProfissionaisEfetivo: 1,
            PrioridadeListagemPublica: false,
            Modulos: ModulosAutonomoEssencial,
            Funcionalidades: FuncionalidadesAutonomoEssencial);
    }

    private static string Normalizar(string valor)
    {
        var normalized = valor.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var caractere in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(caractere));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}

public record PlanoComercialPerfil(
    int? LimiteUsuarios,
    int? LimiteAgendamentosPorDia,
    int? LimiteEstabelecimentosEfetivo,
    int? LimiteProfissionaisEfetivo,
    bool PrioridadeListagemPublica,
    IReadOnlyList<ModuloAssinatura> Modulos,
    IReadOnlyList<string> Funcionalidades)
{
    public static PlanoComercialPerfil Vazio { get; } = new(
        LimiteUsuarios: null,
        LimiteAgendamentosPorDia: null,
        LimiteEstabelecimentosEfetivo: null,
        LimiteProfissionaisEfetivo: null,
        PrioridadeListagemPublica: false,
        Modulos: Array.Empty<ModuloAssinatura>(),
        Funcionalidades: Array.Empty<string>());
}
