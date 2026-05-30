using GLOWAPI.Application.DTOs.Agenda;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class AtendimentoProfissionalService : IAtendimentoProfissionalService
{
    private readonly IAgendamentoItemRepository _agendamentoItemRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IProfissionalEscopoAcessoService _profissionalEscopoAcessoService;

    public AtendimentoProfissionalService(
        IAgendamentoItemRepository agendamentoItemRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IProfissionalEscopoAcessoService profissionalEscopoAcessoService)
    {
        _agendamentoItemRepository = agendamentoItemRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _profissionalEscopoAcessoService = profissionalEscopoAcessoService;
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

        await _profissionalEscopoAcessoService.AutorizarAgendamentoItemAsync(
            estabelecimentoId,
            agendamentoItemId,
            cancellationToken);

        var item = await ObterItemAsync(agendamentoItemId, cancellationToken);
        if (item.Status != AgendamentoItemStatus.Confirmado)
        {
            throw new AtendimentoStatusInvalidoException("Somente atendimento confirmado pode ser iniciado.");
        }

        item.Status = AgendamentoItemStatus.EmAtendimento;
        item.UpdatedAt = DateTime.UtcNow;
        item.Agendamento!.Status = AgendamentoStatus.EmAtendimento;
        item.Agendamento.UpdatedAt = item.UpdatedAt;

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

        await _profissionalEscopoAcessoService.AutorizarAgendamentoItemAsync(
            estabelecimentoId,
            agendamentoItemId,
            cancellationToken);

        var item = await ObterItemAsync(agendamentoItemId, cancellationToken);
        if (item.Status != AgendamentoItemStatus.EmAtendimento)
        {
            throw new AtendimentoStatusInvalidoException("Somente atendimento em andamento pode ser finalizado.");
        }

        item.Status = AgendamentoItemStatus.Concluido;
        item.UpdatedAt = DateTime.UtcNow;
        AtualizarStatusAgendamentoAposConclusao(item);

        _agendamentoItemRepository.Atualizar(item);
        await _agendamentoItemRepository.SalvarAlteracoesAsync(cancellationToken);

        return AtendimentoProfissionalResponseDto.From(item);
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

    private static void AtualizarStatusAgendamentoAposConclusao(AgendamentoItem item)
    {
        var agendamento = item.Agendamento!;
        var todosConcluidos = agendamento.Itens.Count > 0
            && agendamento.Itens.All(i => i.Id == item.Id
                ? item.Status == AgendamentoItemStatus.Concluido
                : i.Status == AgendamentoItemStatus.Concluido);

        agendamento.Status = todosConcluidos
            ? AgendamentoStatus.Concluido
            : AgendamentoStatus.EmAtendimento;
        agendamento.UpdatedAt = item.UpdatedAt;
    }
}
