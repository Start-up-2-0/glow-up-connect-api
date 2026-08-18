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
        "Cadastro de servicos",
        "Agenda simples",
        "Configuracao de horarios",
        "Pagina publica basica",
        "Gestao simples de clientes",
        "Confirmacao por e-mail",
        "Cancelamento por e-mail",
        "Lembrete por e-mail"
    ];

    private static readonly IReadOnlyList<string> FuncionalidadesPlus =
    [
        .. FuncionalidadesBasic,
        "Multiusuario",
        "Agenda compartilhada",
        "Gestao de profissionais",
        "Historico de clientes",
        "Confirmacao automatica via WhatsApp",
        "Lembrete automatico de agendamento",
        "Aviso de cancelamento",
        "Dashboard basico",
        "Relatorios basicos"
    ];

    private static readonly IReadOnlyList<string> FuncionalidadesEssencial =
    [
        .. FuncionalidadesPlus,
        "Operacao completa para uma unidade"
    ];

    private static readonly IReadOnlyList<string> FuncionalidadesPremium =
    [
        .. FuncionalidadesEssencial,
        "Ate 5 unidades na mesma assinatura",
        "Painel consolidado da rede",
        "Controle de caixa",
        "Fluxo financeiro",
        "Comissao automatica",
        "Relatorios financeiros",
        "CRM de clientes",
        "Auditoria de operacoes",
        "Dashboard avancado",
        "Metricas do estabelecimento",
        "Historico financeiro",
        "Gestao completa da equipe",
        "Prioridade na busca e listagem do marketplace"
    ];

    private static readonly IReadOnlyList<string> FuncionalidadesAutonomoEssencial =
    [
        "Agenda pessoal",
        "Cadastro de servicos",
        "Configuracao de horarios",
        "Perfil profissional publico",
        "Presenca no Explorar Lojas",
        "Gestao de clientes e historico",
        "Notificacoes por e-mail",
        "Confirmacao e lembrete por e-mail",
        "Historico de atendimentos"
    ];

    private static readonly IReadOnlyList<string> FuncionalidadesAutonomoPremium =
    [
        .. FuncionalidadesAutonomoEssencial,
        "Confirmacao automatica via WhatsApp",
        "Lembrete automatico de agendamento",
        "Aviso de cancelamento via WhatsApp",
        "Controle de caixa pessoal",
        "Fluxo financeiro",
        "Relatorios financeiros",
        "Dashboard avancado",
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
