using System.Text.Json;
using GLOWAPI.Application.DTOs.Agenda;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class AtendimentoProfissionalService : IAtendimentoProfissionalService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly HashSet<AgendamentoStatus> StatusAgendamentoIniciaveis =
    [
        AgendamentoStatus.Confirmado,
        AgendamentoStatus.EmAtendimento
    ];

    private static readonly HashSet<AgendamentoStatus> StatusAgendamentoBloqueados =
    [
        AgendamentoStatus.Cancelado,
        AgendamentoStatus.Concluido,
        AgendamentoStatus.Expirado,
        AgendamentoStatus.Reembolsado,
        AgendamentoStatus.NaoCompareceu,
        AgendamentoStatus.PendenteConfirmacao,
        AgendamentoStatus.Remarcado,
        AgendamentoStatus.PendentePagamento
    ];

    private readonly IAgendamentoItemRepository _agendamentoItemRepository;
    private readonly IAgendamentoHistoricoRepository _agendamentoHistoricoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IProfissionalEscopoAcessoService _profissionalEscopoAcessoService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAvaliacaoAtendimentoService _avaliacaoAtendimentoService;

    public AtendimentoProfissionalService(
        IAgendamentoItemRepository agendamentoItemRepository,
        IAgendamentoHistoricoRepository agendamentoHistoricoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IProfissionalEscopoAcessoService profissionalEscopoAcessoService,
        ICurrentUserContext currentUserContext,
        IAvaliacaoAtendimentoService avaliacaoAtendimentoService)
    {
        _agendamentoItemRepository = agendamentoItemRepository;
        _agendamentoHistoricoRepository = agendamentoHistoricoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _profissionalEscopoAcessoService = profissionalEscopoAcessoService;
        _currentUserContext = currentUserContext;
        _avaliacaoAtendimentoService = avaliacaoAtendimentoService;
    }

    public async Task<AtendimentoProfissionalResponseDto> IniciarAsync(
        int estabelecimentoId,
        int agendamentoItemId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.AtendimentoIniciar,
            cancellationToken);

        await AutorizarEscopoItemAsync(estabelecimentoId, agendamentoItemId, cancellationToken);

        var item = await ObterItemAsync(agendamentoItemId, cancellationToken);
        ValidarInicio(item);

        var agendamento = item.Agendamento!;
        var statusAnteriorAgendamento = agendamento.Status;

        item.Status = AgendamentoItemStatus.EmAtendimento;
        item.UpdatedAt = DateTime.UtcNow;
        agendamento.Status = AgendamentoStatus.EmAtendimento;
        agendamento.UpdatedAt = item.UpdatedAt;

        if (statusAnteriorAgendamento != AgendamentoStatus.EmAtendimento)
        {
            await RegistrarHistoricoAsync(
                agendamento,
                statusAnteriorAgendamento,
                agendamento.Status,
                motivo: null,
                agendamentoItemId: item.Id,
                cancellationToken);
        }

        _agendamentoItemRepository.Atualizar(item);
        await _agendamentoItemRepository.SalvarAlteracoesAsync(cancellationToken);

        return AtendimentoProfissionalResponseDto.From(item);
    }

    public async Task<AtendimentoProfissionalResponseDto> FinalizarAsync(
        int estabelecimentoId,
        int agendamentoItemId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.AtendimentoFinalizar,
            cancellationToken);

        await AutorizarEscopoItemAsync(estabelecimentoId, agendamentoItemId, cancellationToken);

        var item = await ObterItemAsync(agendamentoItemId, cancellationToken);
        var agendamento = item.Agendamento!;

        if (agendamento.Status != AgendamentoStatus.EmAtendimento)
        {
            throw new AtendimentoStatusInvalidoException("O agendamento precisa estar em atendimento para ser concluido.");
        }

        var itensParaConcluir = agendamento.Itens
            .Where(i => i.Status is AgendamentoItemStatus.EmAtendimento or AgendamentoItemStatus.Confirmado)
            .ToList();

        if (itensParaConcluir.Count == 0
            || itensParaConcluir.All(i => i.Status != AgendamentoItemStatus.EmAtendimento))
        {
            throw new AtendimentoStatusInvalidoException("Somente atendimento em andamento pode ser finalizado.");
        }

        var agora = DateTime.UtcNow;
        var statusAnteriorAgendamento = agendamento.Status;

        foreach (var itemAtivo in itensParaConcluir)
        {
            itemAtivo.Status = AgendamentoItemStatus.Concluido;
            itemAtivo.UpdatedAt = agora;
            _agendamentoItemRepository.Atualizar(itemAtivo);
        }

        ConcluirAgendamento(agendamento, agora);

        if (statusAnteriorAgendamento != AgendamentoStatus.Concluido)
        {
            await RegistrarHistoricoAsync(
                agendamento,
                statusAnteriorAgendamento,
                agendamento.Status,
                motivo: null,
                agendamentoItemId: item.Id,
                cancellationToken);

            await _avaliacaoAtendimentoService.SolicitarAposConclusaoAsync(
                agendamento,
                cancellationToken);
        }

        await _agendamentoItemRepository.SalvarAlteracoesAsync(cancellationToken);

        return AtendimentoProfissionalResponseDto.From(item);
    }

    private async Task AutorizarEscopoItemAsync(
        int estabelecimentoId,
        int agendamentoItemId,
        CancellationToken cancellationToken)
    {
        var contexto = await _autorizacaoNegocioService.ObterContextoAsync(
            estabelecimentoId,
            cancellationToken);

        if (contexto.PossuiPermissao(PermissaoNegocio.AgendaVisualizarGeral))
        {
            var item = await _agendamentoItemRepository.ObterPorIdComAgendamentoAsync(
                agendamentoItemId,
                cancellationToken);

            if (item is null
                || item.Agendamento is null
                || item.Agendamento.EstabelecimentoId != estabelecimentoId)
            {
                throw new RecursoProfissionalNaoEncontradoException();
            }

            return;
        }

        await _profissionalEscopoAcessoService.AutorizarAgendamentoItemAsync(
            estabelecimentoId,
            agendamentoItemId,
            cancellationToken);
    }

    private static void ValidarInicio(AgendamentoItem item)
    {
        if (item.Status != AgendamentoItemStatus.Confirmado)
        {
            throw new AtendimentoStatusInvalidoException("Somente atendimento confirmado pode ser iniciado.");
        }

        var agendamento = item.Agendamento!;
        if (StatusAgendamentoBloqueados.Contains(agendamento.Status))
        {
            throw new AtendimentoStatusInvalidoException("Agendamento cancelado ou concluido nao pode ser iniciado.");
        }

        if (!StatusAgendamentoIniciaveis.Contains(agendamento.Status))
        {
            throw new AtendimentoStatusInvalidoException("Agendamento nao pode ser iniciado no status atual.");
        }

        if (DateTime.UtcNow < item.Inicio)
        {
            throw new AtendimentoStatusInvalidoException("O horario do atendimento ainda nao chegou.");
        }
    }

    private async Task<AgendamentoItem> ObterItemAsync(
        int agendamentoItemId,
        CancellationToken cancellationToken)
    {
        var item = await _agendamentoItemRepository.ObterPorIdComAgendamentoAsync(
            agendamentoItemId,
            cancellationToken);

        if (item is null || item.Agendamento is null)
        {
            throw new RecursoProfissionalNaoEncontradoException();
        }

        return item;
    }

    /// <summary>
    /// Conclusão é sempre do atendimento (agendamento) inteiro.
    /// Itens Cancelado/Repassado não bloqueiam o fechamento.
    /// </summary>
    private static void ConcluirAgendamento(Agendamento agendamento, DateTime atualizadoEm)
    {
        var itensRelevantes = agendamento.Itens
            .Where(i => i.Status is not AgendamentoItemStatus.Cancelado
                and not AgendamentoItemStatus.Repassado)
            .ToList();

        var todosConcluidos = itensRelevantes.Count > 0
            && itensRelevantes.All(i => i.Status == AgendamentoItemStatus.Concluido);

        if (!todosConcluidos)
        {
            throw new AtendimentoStatusInvalidoException(
                "Nao foi possivel concluir o atendimento: ainda ha itens pendentes.");
        }

        agendamento.Status = AgendamentoStatus.Concluido;
        agendamento.UpdatedAt = atualizadoEm;
    }

    private async Task RegistrarHistoricoAsync(
        Agendamento agendamento,
        AgendamentoStatus statusAnterior,
        AgendamentoStatus statusNovo,
        string? motivo,
        int agendamentoItemId,
        CancellationToken cancellationToken)
    {
        var historico = new AgendamentoHistorico
        {
            AgendamentoId = agendamento.Id,
            UsuarioExecutorId = _currentUserContext.UserId,
            StatusAnterior = statusAnterior,
            StatusNovo = statusNovo,
            Motivo = motivo,
            PayloadJson = JsonSerializer.Serialize(new
            {
                agendamento.ValorTotal,
                agendamentoItemId
            }, JsonOptions),
            CriadoEm = DateTime.UtcNow
        };

        await _agendamentoHistoricoRepository.AdicionarAsync(historico, cancellationToken);
    }
}
