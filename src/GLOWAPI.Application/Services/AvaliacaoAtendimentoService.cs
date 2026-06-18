using GLOWAPI.Application.DTOs.Avaliacao;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class AvaliacaoAtendimentoService : IAvaliacaoAtendimentoService
{
    private const int DiasValidadeConvite = 30;

    private readonly IAgendamentoRepository _agendamentoRepository;
    private readonly IAvaliacaoAtendimentoRepository _avaliacaoAtendimentoRepository;
    private readonly IAvaliacaoConviteRepository _avaliacaoConviteRepository;
    private readonly IAvaliacaoResumoService _avaliacaoResumoService;
    private readonly IAgendamentoNotificacaoService _agendamentoNotificacaoService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly AuthOptions _authOptions;

    public AvaliacaoAtendimentoService(
        IAgendamentoRepository agendamentoRepository,
        IAvaliacaoAtendimentoRepository avaliacaoAtendimentoRepository,
        IAvaliacaoConviteRepository avaliacaoConviteRepository,
        IAvaliacaoResumoService avaliacaoResumoService,
        IAgendamentoNotificacaoService agendamentoNotificacaoService,
        ICurrentUserContext currentUserContext,
        IOptions<AuthOptions> authOptions)
    {
        _agendamentoRepository = agendamentoRepository;
        _avaliacaoAtendimentoRepository = avaliacaoAtendimentoRepository;
        _avaliacaoConviteRepository = avaliacaoConviteRepository;
        _avaliacaoResumoService = avaliacaoResumoService;
        _agendamentoNotificacaoService = agendamentoNotificacaoService;
        _currentUserContext = currentUserContext;
        _authOptions = authOptions.Value;
    }

    public async Task<AvaliacaoContextoResponseDto> ObterContextoMeuAgendamentoAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default)
    {
        var agendamento = await ObterMeuAgendamentoAsync(agendamentoId, cancellationToken);
        return await MontarContextoAsync(agendamento, cancellationToken);
    }

    public async Task<AvaliacaoContextoResponseDto> ObterContextoPorTokenAsync(
        Guid token,
        CancellationToken cancellationToken = default)
    {
        var convite = await ObterConviteValidoAsync(token, cancellationToken);
        var agendamento = convite.Agendamento!;
        return await MontarContextoAsync(agendamento, cancellationToken);
    }

    public async Task<AvaliacaoContextoResponseDto> CriarMeuAgendamentoAsync(
        int agendamentoId,
        CriarAvaliacaoAtendimentoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var agendamento = await ObterMeuAgendamentoAsync(agendamentoId, cancellationToken);
        await CriarAvaliacaoAsync(
            agendamento,
            request,
            AvaliacaoOrigem.Historico,
            convite: null,
            cancellationToken);

        return await MontarContextoAsync(agendamento, cancellationToken);
    }

    public async Task<AvaliacaoContextoResponseDto> CriarPorTokenAsync(
        Guid token,
        CriarAvaliacaoAtendimentoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var convite = await ObterConviteValidoAsync(token, cancellationToken);
        var agendamento = convite.Agendamento!;

        await CriarAvaliacaoAsync(
            agendamento,
            request,
            AvaliacaoOrigem.LinkEmail,
            convite,
            cancellationToken);

        return await MontarContextoAsync(agendamento, cancellationToken);
    }

    public async Task SolicitarAposConclusaoAsync(
        Agendamento agendamento,
        CancellationToken cancellationToken = default)
    {
        if (agendamento.Status != AgendamentoStatus.Concluido
            || agendamento.EstabelecimentoId is null)
        {
            return;
        }

        if (agendamento.Estabelecimento is null || !agendamento.Itens.Any(i => i.Profissional is not null))
        {
            agendamento = await _agendamentoRepository.ObterPorIdEEstabelecimentoComItensAsync(
                agendamento.Id,
                agendamento.EstabelecimentoId.Value,
                cancellationToken) ?? agendamento;
        }

        if (agendamento.Estabelecimento is null)
        {
            return;
        }

        var conviteExistente = await _avaliacaoConviteRepository.ObterPorAgendamentoIdAsync(
            agendamento.Id,
            cancellationToken);

        AvaliacaoConvite convite;
        if (conviteExistente is null)
        {
            convite = new AvaliacaoConvite
            {
                AgendamentoId = agendamento.Id,
                TokenPublico = Guid.NewGuid(),
                ExpiraEm = DateTime.UtcNow.AddDays(DiasValidadeConvite),
                CriadoEm = DateTime.UtcNow
            };

            await _avaliacaoConviteRepository.AdicionarAsync(convite, cancellationToken);
            await _avaliacaoConviteRepository.SalvarAlteracoesAsync(cancellationToken);
        }
        else
        {
            convite = conviteExistente;
        }

        var estabelecimento = agendamento.Estabelecimento
            ?? throw new NegocioNaoEncontradoException();
        var profissional = ResolverProfissionalPrincipal(agendamento);
        var linkAvaliacao = $"{_authOptions.FrontendBaseUrl.TrimEnd('/')}/avaliar/{convite.TokenPublico}";

        await _agendamentoNotificacaoService.AgendamentoConcluidoAsync(
            agendamento,
            estabelecimento,
            profissional,
            linkAvaliacao,
            cancellationToken);
    }

    public async Task<string> ObterStatusAvaliacaoAsync(
        Agendamento agendamento,
        CancellationToken cancellationToken = default)
    {
        if (agendamento.Status != AgendamentoStatus.Concluido)
        {
            return "Indisponivel";
        }

        var existente = await _avaliacaoAtendimentoRepository.ObterPorAgendamentoIdAsync(
            agendamento.Id,
            cancellationToken);

        return existente is null ? "Pendente" : "Realizada";
    }

    public async Task<AvaliacaoResumoClienteDto?> ObterResumoClienteAsync(
        int agendamentoId,
        CancellationToken cancellationToken = default)
    {
        var avaliacao = await _avaliacaoAtendimentoRepository.ObterPorAgendamentoIdAsync(
            agendamentoId,
            cancellationToken);

        return avaliacao is null
            ? null
            : new AvaliacaoResumoClienteDto(
                avaliacao.NotaEstabelecimento,
                avaliacao.NotaProfissional,
                avaliacao.AvaliadoEm);
    }

    private async Task CriarAvaliacaoAsync(
        Agendamento agendamento,
        CriarAvaliacaoAtendimentoRequestDto request,
        AvaliacaoOrigem origem,
        AvaliacaoConvite? convite,
        CancellationToken cancellationToken)
    {
        ValidarElegibilidade(agendamento);

        var existente = await _avaliacaoAtendimentoRepository.ObterPorAgendamentoIdAsync(
            agendamento.Id,
            cancellationToken);

        if (existente is not null)
        {
            throw new AvaliacaoJaRealizadaException();
        }

        ValidarNotas(request);

        if (agendamento.EstabelecimentoId is null)
        {
            throw new AvaliacaoNaoElegivelException();
        }

        var profissional = ResolverProfissionalPrincipal(agendamento);
        var avaliacao = new AvaliacaoAtendimento
        {
            AgendamentoId = agendamento.Id,
            UsuarioClienteId = agendamento.UsuarioClienteId,
            EstabelecimentoId = agendamento.EstabelecimentoId.Value,
            ProfissionalId = profissional.Id,
            NotaEstabelecimento = (byte)request.NotaEstabelecimento,
            ComentarioEstabelecimento = NormalizarComentario(request.ComentarioEstabelecimento),
            NotaProfissional = (byte)request.NotaProfissional,
            ComentarioProfissional = NormalizarComentario(request.ComentarioProfissional),
            AvaliadoEm = DateTime.UtcNow,
            Origem = origem
        };

        await _avaliacaoAtendimentoRepository.AdicionarAsync(avaliacao, cancellationToken);

        if (convite is not null)
        {
            convite.UtilizadoEm = DateTime.UtcNow;
            _avaliacaoConviteRepository.Atualizar(convite);
        }

        await _avaliacaoAtendimentoRepository.SalvarAlteracoesAsync(cancellationToken);

        await _avaliacaoResumoService.RecalcularCacheEstabelecimentoAsync(
            avaliacao.EstabelecimentoId,
            cancellationToken);
        await _avaliacaoResumoService.RecalcularCacheProfissionalAsync(
            avaliacao.ProfissionalId,
            cancellationToken);
    }

    private async Task<AvaliacaoContextoResponseDto> MontarContextoAsync(
        Agendamento agendamento,
        CancellationToken cancellationToken)
    {
        var status = await ObterStatusAvaliacaoAsync(agendamento, cancellationToken);
        var avaliacao = await _avaliacaoAtendimentoRepository.ObterPorAgendamentoIdAsync(
            agendamento.Id,
            cancellationToken);

        var profissional = ResolverProfissionalPrincipal(agendamento);
        var estabelecimento = agendamento.Estabelecimento
            ?? throw new NegocioNaoEncontradoException();

        AvaliacaoResumoClienteDto? resumo = avaliacao is null
            ? null
            : new AvaliacaoResumoClienteDto(
                avaliacao.NotaEstabelecimento,
                avaliacao.NotaProfissional,
                avaliacao.AvaliadoEm);

        return new AvaliacaoContextoResponseDto(
            status,
            agendamento.Id,
            estabelecimento.PublicGuid,
            estabelecimento.Nome,
            estabelecimento.Logo,
            profissional.Id,
            profissional.NomePublico,
            profissional.Logo,
            AgendamentoHorarioHelper.ObterInicio(agendamento),
            AgendamentoHorarioHelper.ObterFim(agendamento),
            resumo);
    }

    private async Task<Agendamento> ObterMeuAgendamentoAsync(
        int agendamentoId,
        CancellationToken cancellationToken)
    {
        var usuarioId = _currentUserContext.UserId
            ?? throw new AvaliacaoNaoElegivelException("Usuario nao autenticado.");

        var agendamento = await _agendamentoRepository.ObterPorIdEUsuarioClienteAsync(
            agendamentoId,
            usuarioId,
            cancellationToken);

        return agendamento ?? throw new AgendamentoNaoEncontradoException();
    }

    private async Task<AvaliacaoConvite> ObterConviteValidoAsync(
        Guid token,
        CancellationToken cancellationToken)
    {
        var convite = await _avaliacaoConviteRepository.ObterPorTokenComAgendamentoAsync(token, cancellationToken);
        if (convite is null)
        {
            throw new AvaliacaoConviteInvalidoException();
        }

        if (convite.UtilizadoEm.HasValue)
        {
            throw new AvaliacaoJaRealizadaException();
        }

        if (convite.ExpiraEm <= DateTime.UtcNow)
        {
            throw new AvaliacaoConviteInvalidoException("Link de avaliacao expirado.");
        }

        if (convite.Agendamento?.Status != AgendamentoStatus.Concluido)
        {
            throw new AvaliacaoNaoElegivelException();
        }

        return convite;
    }

    private static void ValidarElegibilidade(Agendamento agendamento)
    {
        if (agendamento.Status != AgendamentoStatus.Concluido)
        {
            throw new AvaliacaoNaoElegivelException();
        }
    }

    private static void ValidarNotas(CriarAvaliacaoAtendimentoRequestDto request)
    {
        if (!AvaliacaoNotaFormatter.NotaValida(request.NotaEstabelecimento)
            || !AvaliacaoNotaFormatter.NotaValida(request.NotaProfissional))
        {
            throw new AvaliacaoNotaInvalidaException();
        }
    }

    private static string? NormalizarComentario(string? comentario)
    {
        if (string.IsNullOrWhiteSpace(comentario))
        {
            return null;
        }

        var texto = comentario.Trim();
        return texto.Length > 1000 ? texto[..1000] : texto;
    }

    private static Profissional ResolverProfissionalPrincipal(Agendamento agendamento)
    {
        var item = agendamento.Itens
            .Where(i => i.Status == AgendamentoItemStatus.Concluido)
            .OrderBy(i => i.Inicio)
            .FirstOrDefault()
            ?? agendamento.Itens.OrderBy(i => i.Inicio).FirstOrDefault();

        if (item?.Profissional is null)
        {
            throw new AvaliacaoNaoElegivelException("Profissional do atendimento nao encontrado.");
        }

        return item.Profissional;
    }
}
