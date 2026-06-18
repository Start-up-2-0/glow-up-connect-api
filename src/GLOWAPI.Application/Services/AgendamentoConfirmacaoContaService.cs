using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class AgendamentoConfirmacaoContaService : IAgendamentoConfirmacaoContaService
{
    private readonly IAgendamentoRepository _agendamentoRepository;
    private readonly IAgendamentoNotificacaoService _agendamentoNotificacaoService;
    private readonly IProfissionalRepository _profissionalRepository;

    public AgendamentoConfirmacaoContaService(
        IAgendamentoRepository agendamentoRepository,
        IAgendamentoNotificacaoService agendamentoNotificacaoService,
        IProfissionalRepository profissionalRepository)
    {
        _agendamentoRepository = agendamentoRepository;
        _agendamentoNotificacaoService = agendamentoNotificacaoService;
        _profissionalRepository = profissionalRepository;
    }

    public async Task ProcessarConfirmacaoContaClienteAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        var agendamentos = await _agendamentoRepository.ListarPorUsuarioClienteAsync(usuarioId, cancellationToken);
        var pendentes = agendamentos
            .Where(agendamento => agendamento.Origem == OrigemAgendamento.CadastroPublico)
            .ToList();

        foreach (var agendamento in pendentes)
        {
            agendamento.ClienteNome = null;
            agendamento.ClienteEmail = null;
            agendamento.ClienteTelefone = null;
            agendamento.UpdatedAt = DateTime.UtcNow;
            _agendamentoRepository.Atualizar(agendamento);

            if (!agendamento.EstabelecimentoId.HasValue)
            {
                continue;
            }

            var completo = await _agendamentoRepository.ObterPorIdEEstabelecimentoComItensAsync(
                agendamento.Id,
                agendamento.EstabelecimentoId.Value,
                cancellationToken);
            if (completo?.Estabelecimento is null)
            {
                continue;
            }

            var profissional = await _profissionalRepository.ObterPorIdAsync(
                completo.Itens.First().ProfissionalId,
                cancellationToken);
            if (profissional is null)
            {
                throw new RecursoProfissionalNaoEncontradoException();
            }

            await _agendamentoNotificacaoService.AgendamentoCriadoAsync(
                completo,
                completo.Estabelecimento,
                profissional,
                cancellationToken);
        }

        await _agendamentoRepository.SalvarAlteracoesAsync(cancellationToken);
    }
}
