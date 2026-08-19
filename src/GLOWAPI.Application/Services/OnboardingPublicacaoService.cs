using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class OnboardingPublicacaoService : IOnboardingPublicacaoService
{
    public const string EtapaAssinatura = "assinatura";
    public const string EtapaEquipe = "equipe";
    public const string EtapaServicos = "servicos";
    public const string EtapaHorarios = "horarios";
    public const string EtapaPerfil = "perfil";
    public const string EtapaRevisao = "revisao";

    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IAssinaturaRepository _assinaturaRepository;
    private readonly IAssinaturaEstabelecimentoRepository _assinaturaEstabelecimentoRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IServicoRepository _servicoRepository;
    private readonly IProfissionalServicoRepository _profissionalServicoRepository;
    private readonly IHorarioFuncionamentoEstabelecimentoRepository _horarioFuncionamentoRepository;
    private readonly IHorarioAtendimentoProfissionalRepository _horarioAtendimentoProfissionalRepository;

    public OnboardingPublicacaoService(
        IEstabelecimentoRepository estabelecimentoRepository,
        IAssinaturaRepository assinaturaRepository,
        IAssinaturaEstabelecimentoRepository assinaturaEstabelecimentoRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IServicoRepository servicoRepository,
        IProfissionalServicoRepository profissionalServicoRepository,
        IHorarioFuncionamentoEstabelecimentoRepository horarioFuncionamentoRepository,
        IHorarioAtendimentoProfissionalRepository horarioAtendimentoProfissionalRepository)
    {
        _estabelecimentoRepository = estabelecimentoRepository;
        _assinaturaRepository = assinaturaRepository;
        _assinaturaEstabelecimentoRepository = assinaturaEstabelecimentoRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _servicoRepository = servicoRepository;
        _profissionalServicoRepository = profissionalServicoRepository;
        _horarioFuncionamentoRepository = horarioFuncionamentoRepository;
        _horarioAtendimentoProfissionalRepository = horarioAtendimentoProfissionalRepository;
    }

    public async Task<OnboardingPublicacaoStatusDto> ObterStatusAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        var estabelecimento = await _estabelecimentoRepository.ObterPorIdComEnderecoAsync(
            estabelecimentoId,
            cancellationToken);
        if (estabelecimento is null || !estabelecimento.Ativo)
        {
            throw new NegocioNaoEncontradoException();
        }

        var assinatura = await _assinaturaRepository.ObterAssinaturaEfetivaPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);
        var tipoAssinatura = assinatura?.TipoAssinatura
            ?? await _estabelecimentoRepository.ObterTipoAssinaturaPublicoAsync(
                estabelecimentoId,
                cancellationToken);
        var assinaturaAtiva = assinatura?.Status is AssinaturaStatus.Ativa or AssinaturaStatus.Trial;

        var avaliacao = await AvaliarProntidaoAsync(
            estabelecimento,
            tipoAssinatura,
            cancellationToken);

        var onboardingPendente = assinaturaAtiva
            && OnboardingPublicacaoObrigatorio(tipoAssinatura)
            && !avaliacao.ProntoParaPublicacao;

        var prontoParaPublicacao = OnboardingPublicacaoObrigatorio(tipoAssinatura)
            ? avaliacao.ProntoParaPublicacao
            : assinaturaAtiva;

        return new OnboardingPublicacaoStatusDto(
            estabelecimentoId,
            tipoAssinatura.ToString(),
            prontoParaPublicacao,
            estabelecimento.VisivelPublicamente,
            onboardingPendente,
            onboardingPendente ? avaliacao.ProximaEtapa : null,
            avaliacao.Etapas);
    }

    public async Task<bool> RecalcularVisibilidadeAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        var estabelecimento = await _estabelecimentoRepository.ObterPorIdComEnderecoAsync(
            estabelecimentoId,
            cancellationToken);
        if (estabelecimento is null || !estabelecimento.Ativo)
        {
            return false;
        }

        var assinatura = await _assinaturaRepository.ObterAssinaturaEfetivaPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);
        var assinaturaAtiva = assinatura?.Status is AssinaturaStatus.Ativa or AssinaturaStatus.Trial;
        var tipoAssinatura = assinatura?.TipoAssinatura
            ?? await _estabelecimentoRepository.ObterTipoAssinaturaPublicoAsync(
                estabelecimentoId,
                cancellationToken);

        var avaliacao = await AvaliarProntidaoAsync(
            estabelecimento,
            tipoAssinatura,
            cancellationToken);
        var deveSerVisivel = assinaturaAtiva && (
            !OnboardingPublicacaoObrigatorio(tipoAssinatura) || avaliacao.ProntoParaPublicacao);

        if (estabelecimento.VisivelPublicamente == deveSerVisivel)
        {
            return deveSerVisivel;
        }

        estabelecimento.VisivelPublicamente = deveSerVisivel;
        estabelecimento.UpdatedAt = DateTime.UtcNow;
        _estabelecimentoRepository.Atualizar(estabelecimento);
        await _estabelecimentoRepository.SalvarAlteracoesAsync(cancellationToken);

        return deveSerVisivel;
    }

    public async Task RecalcularVisibilidadePorAssinaturaAsync(
        Assinatura assinatura,
        CancellationToken cancellationToken = default)
    {
        var estabelecimentoIds = await ObterEstabelecimentoIdsVinculadosAsync(assinatura, cancellationToken);
        foreach (var estabelecimentoId in estabelecimentoIds)
        {
            await RecalcularVisibilidadeAsync(estabelecimentoId, cancellationToken);
        }
    }

    private static bool OnboardingPublicacaoObrigatorio(TipoAssinatura tipoAssinatura) =>
        tipoAssinatura == TipoAssinatura.ProfissionalAutonomo;

    private async Task<(bool ProntoParaPublicacao, string? ProximaEtapa, IReadOnlyList<OnboardingEtapaStatusDto> Etapas)> AvaliarProntidaoAsync(
        Estabelecimento estabelecimento,
        TipoAssinatura tipoAssinatura,
        CancellationToken cancellationToken)
    {
        var etapas = new List<OnboardingEtapaStatusDto>
        {
            CriarEtapaAssinatura()
        };

        if (tipoAssinatura == TipoAssinatura.ProfissionalAutonomo)
        {
            etapas.Add(await CriarEtapaPerfilAsync(estabelecimento, cancellationToken));
            etapas.Add(await CriarEtapaServicosAsync(
                estabelecimento.Id,
                exigirVinculoProfissional: false,
                cancellationToken));
            etapas.Add(await CriarEtapaHorariosAsync(
                estabelecimento.Id,
                exigirHorarioFuncionamento: false,
                cancellationToken));
        }
        else
        {
            etapas.Add(await CriarEtapaEquipeAsync(estabelecimento.Id, cancellationToken));
            etapas.Add(await CriarEtapaServicosAsync(
                estabelecimento.Id,
                exigirVinculoProfissional: true,
                cancellationToken));
            etapas.Add(await CriarEtapaHorariosAsync(
                estabelecimento.Id,
                exigirHorarioFuncionamento: true,
                cancellationToken));
        }

        var pronto = etapas.All(etapa => etapa.Concluida);
        var proximaEtapa = etapas.FirstOrDefault(etapa => !etapa.Concluida)?.Id ?? EtapaRevisao;

        return (pronto, proximaEtapa, etapas);
    }

    private static OnboardingEtapaStatusDto CriarEtapaAssinatura() =>
        new(
            EtapaAssinatura,
            "Assinatura confirmada",
            true,
            Array.Empty<string>());

    private static Task<OnboardingEtapaStatusDto> CriarEtapaPerfilAsync(
        Estabelecimento estabelecimento,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var pendencias = new List<string>();

        if (string.IsNullOrWhiteSpace(estabelecimento.Nome))
        {
            pendencias.Add("Informe o nome publico.");
        }

        if (string.IsNullOrWhiteSpace(estabelecimento.Logo))
        {
            pendencias.Add("Adicione uma foto ou logo.");
        }

        if (string.IsNullOrWhiteSpace(estabelecimento.Telefone))
        {
            pendencias.Add("Informe o telefone de contato.");
        }

        if (string.IsNullOrWhiteSpace(estabelecimento.Email))
        {
            pendencias.Add("Informe o e-mail de contato.");
        }

        if (!estabelecimento.CategoriaEstabelecimentoId.HasValue)
        {
            pendencias.Add("Selecione sua area de atuacao.");
        }

        if (estabelecimento.Endereco is null
            || !OperacaoPerfilValidation.EnderecoEstaCompletoParaGeocodificacao(estabelecimento.Endereco))
        {
            pendencias.Add("Complete o endereco de atendimento.");
        }
        else if (!OperacaoPerfilValidation.EnderecoPossuiCoordenadas(estabelecimento.Endereco))
        {
            pendencias.Add("Confirme a localizacao no mapa.");
        }

        return Task.FromResult(new OnboardingEtapaStatusDto(
            EtapaPerfil,
            "Perfil",
            pendencias.Count == 0,
            pendencias));
    }

    private async Task<OnboardingEtapaStatusDto> CriarEtapaEquipeAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var profissionais = await _profissionalEstabelecimentoRepository
            .ListarAtivosComAgendamentoPorEstabelecimentoAsync(estabelecimentoId, cancellationToken);

        var pendencias = profissionais.Count == 0
            ? new List<string> { "Cadastre pelo menos um profissional ativo." }
            : [];

        return new OnboardingEtapaStatusDto(
            EtapaEquipe,
            "Equipe",
            pendencias.Count == 0,
            pendencias);
    }

    private async Task<OnboardingEtapaStatusDto> CriarEtapaServicosAsync(
        int estabelecimentoId,
        bool exigirVinculoProfissional,
        CancellationToken cancellationToken)
    {
        var totalServicos = await _servicoRepository.ContarAtivosPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        var pendencias = new List<string>();
        if (totalServicos == 0)
        {
            pendencias.Add("Cadastre pelo menos um servico ativo.");
        }

        if (exigirVinculoProfissional)
        {
            var possuiVinculo = await _profissionalServicoRepository
                .ExisteVinculoAtivoPorEstabelecimentoAsync(estabelecimentoId, cancellationToken);
            if (!possuiVinculo)
            {
                pendencias.Add("Vincule pelo menos um servico a um profissional.");
            }
        }

        return new OnboardingEtapaStatusDto(
            EtapaServicos,
            "Servicos",
            pendencias.Count == 0,
            pendencias);
    }

    private async Task<OnboardingEtapaStatusDto> CriarEtapaHorariosAsync(
        int estabelecimentoId,
        bool exigirHorarioFuncionamento,
        CancellationToken cancellationToken)
    {
        var pendencias = new List<string>();

        if (exigirHorarioFuncionamento)
        {
            var horariosFuncionamento = await _horarioFuncionamentoRepository.ListarPorEstabelecimentoAsync(
                estabelecimentoId,
                ativo: true,
                cancellationToken: cancellationToken);
            if (horariosFuncionamento.Count == 0)
            {
                pendencias.Add("Defina pelo menos um horario de funcionamento da loja.");
            }
        }

        var horariosProfissionais = await _horarioAtendimentoProfissionalRepository.ListarPorEstabelecimentoAsync(
            estabelecimentoId,
            ativo: true,
            cancellationToken: cancellationToken);
        if (horariosProfissionais.Count == 0)
        {
            pendencias.Add("Defina pelo menos um horario de atendimento.");
        }

        return new OnboardingEtapaStatusDto(
            EtapaHorarios,
            "Horarios",
            pendencias.Count == 0,
            pendencias);
    }

    private async Task<IReadOnlyList<int>> ObterEstabelecimentoIdsVinculadosAsync(
        Assinatura assinatura,
        CancellationToken cancellationToken)
    {
        if (assinatura.Id <= 0)
        {
            return assinatura.EstabelecimentoId.HasValue
                ? [assinatura.EstabelecimentoId.Value]
                : Array.Empty<int>();
        }

        var vinculos = await _assinaturaEstabelecimentoRepository.ListarPorAssinaturaAsync(
            assinatura.Id,
            cancellationToken);
        if (vinculos.Count > 0)
        {
            return vinculos.Select(vinculo => vinculo.EstabelecimentoId).Distinct().ToList();
        }

        return assinatura.EstabelecimentoId.HasValue
            ? [assinatura.EstabelecimentoId.Value]
            : Array.Empty<int>();
    }
}
