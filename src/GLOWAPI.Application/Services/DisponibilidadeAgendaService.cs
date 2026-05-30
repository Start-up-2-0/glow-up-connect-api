using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class DisponibilidadeAgendaService : IDisponibilidadeAgendaService
{
    private const int IntervaloEntreSlotsMinutos = 15;
    private const int MaximoDiasConsulta = 31;

    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IServicoRepository _servicoRepository;
    private readonly IProfissionalServicoRepository _profissionalServicoRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IHorarioFuncionamentoEstabelecimentoRepository _horarioFuncionamentoRepository;
    private readonly IHorarioAtendimentoProfissionalRepository _horarioAtendimentoProfissionalRepository;
    private readonly IAgendamentoItemRepository _agendamentoItemRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;

    public DisponibilidadeAgendaService(
        IEstabelecimentoRepository estabelecimentoRepository,
        IProfissionalRepository profissionalRepository,
        IServicoRepository servicoRepository,
        IProfissionalServicoRepository profissionalServicoRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IHorarioFuncionamentoEstabelecimentoRepository horarioFuncionamentoRepository,
        IHorarioAtendimentoProfissionalRepository horarioAtendimentoProfissionalRepository,
        IAgendamentoItemRepository agendamentoItemRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService)
    {
        _estabelecimentoRepository = estabelecimentoRepository;
        _profissionalRepository = profissionalRepository;
        _servicoRepository = servicoRepository;
        _profissionalServicoRepository = profissionalServicoRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _horarioFuncionamentoRepository = horarioFuncionamentoRepository;
        _horarioAtendimentoProfissionalRepository = horarioAtendimentoProfissionalRepository;
        _agendamentoItemRepository = agendamentoItemRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
    }

    public async Task<DisponibilidadeAgendaResponseDto> ConsultarPorEstabelecimentoAsync(
        int estabelecimentoId,
        ConsultarDisponibilidadeAgendaDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.AgendaCriar,
            cancellationToken);

        return await ConsultarInternoAsync(
            estabelecimentoId,
            request,
            exigirFuncionamentoEstabelecimento: true,
            cancellationToken);
    }

    public async Task<DisponibilidadeAgendaResponseDto> ConsultarPublicoPorEstabelecimentoAsync(
        Guid publicGuid,
        ConsultarDisponibilidadeAgendaDto request,
        CancellationToken cancellationToken = default)
    {
        var estabelecimento = await _estabelecimentoRepository.ObterPorPublicGuidAsync(publicGuid, cancellationToken);
        if (estabelecimento is null || !estabelecimento.Ativo)
        {
            throw new NegocioNaoEncontradoException();
        }

        return await ConsultarInternoAsync(
            estabelecimento.Id,
            request,
            exigirFuncionamentoEstabelecimento: true,
            cancellationToken);
    }

    public async Task<DisponibilidadeAgendaResponseDto> ConsultarPublicoPorProfissionalAutonomoAsync(
        Guid publicGuid,
        ConsultarDisponibilidadeAgendaDto request,
        CancellationToken cancellationToken = default)
    {
        var profissional = await _profissionalRepository.ObterPorPublicGuidAsync(publicGuid, cancellationToken);
        if (profissional is null
            || !profissional.Ativo
            || profissional.TipoProfissional != ProfessionalType.Autonomo)
        {
            throw new RecursoProfissionalNaoEncontradoException();
        }

        var vinculo = await _profissionalEstabelecimentoRepository.ObterAtivoPorProfissionalAsync(
            profissional.Id,
            cancellationToken);
        if (vinculo?.EstabelecimentoId is null || !vinculo.Ativo || !vinculo.PodeReceberAgendamento)
        {
            throw new ProfissionalSemVinculoNegocioException();
        }

        request.ProfissionalId = profissional.Id;

        return await ConsultarInternoAsync(
            vinculo.EstabelecimentoId,
            request,
            exigirFuncionamentoEstabelecimento: false,
            cancellationToken);
    }

    private async Task<DisponibilidadeAgendaResponseDto> ConsultarInternoAsync(
        int estabelecimentoId,
        ConsultarDisponibilidadeAgendaDto request,
        bool exigirFuncionamentoEstabelecimento,
        CancellationToken cancellationToken)
    {
        ValidarPeriodo(request.DataInicio, request.DataFim);

        var servico = await _servicoRepository.ObterPorIdEEstabelecimentoAsync(
            request.ServicoId,
            estabelecimentoId,
            cancellationToken);
        if (servico is null)
        {
            throw new ServicoNegocioNaoEncontradoException();
        }

        var profissionais = await ResolverProfissionaisAsync(
            request.ProfissionalId,
            servico,
            estabelecimentoId,
            cancellationToken);

        if (profissionais.Count == 0)
        {
            return new DisponibilidadeAgendaResponseDto
            {
                ServicoId = servico.Id,
                DuracaoMinutos = servico.DuracaoMinutos,
                Slots = []
            };
        }

        var inicioUtc = request.DataInicio.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimUtc = request.DataFim.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var ocupacao = await _agendamentoItemRepository.ListarOcupacaoAsync(
            estabelecimentoId,
            request.ProfissionalId,
            inicioUtc,
            fimUtc,
            cancellationToken);

        var slots = new List<SlotDisponivelResponseDto>();

        for (var data = request.DataInicio; data <= request.DataFim; data = data.AddDays(1))
        {
            var diaSemana = data.DayOfWeek;
            var funcionamentos = exigirFuncionamentoEstabelecimento
                ? await _horarioFuncionamentoRepository.ListarAtivosPorEstabelecimentoEDiaAsync(
                    estabelecimentoId,
                    diaSemana,
                    cancellationToken)
                : [];

            foreach (var profissionalId in profissionais)
            {
                if (!await ProfissionalPodeAtenderAsync(profissionalId, estabelecimentoId, cancellationToken))
                {
                    continue;
                }

                var horariosProfissional = await _horarioAtendimentoProfissionalRepository.ListarPorEstabelecimentoAsync(
                    estabelecimentoId,
                    profissionalId,
                    diaSemana,
                    ativo: true,
                    cancellationToken);

                foreach (var horarioProfissional in horariosProfissional)
                {
                    var janelas = exigirFuncionamentoEstabelecimento
                        ? IntersectarComFuncionamento(funcionamentos, horarioProfissional)
                        : [(horarioProfissional.HoraInicio, horarioProfissional.HoraFim)];

                    foreach (var (janelaInicio, janelaFim) in janelas)
                    {
                        foreach (var (inicioSlot, fimSlot) in GeradorSlotsDisponibilidade.Gerar(
                                     data,
                                     janelaInicio,
                                     janelaFim,
                                     servico.DuracaoMinutos,
                                     IntervaloEntreSlotsMinutos))
                        {
                            if (inicioSlot < DateTime.UtcNow)
                            {
                                continue;
                            }

                            if (SlotOcupado(ocupacao, profissionalId, inicioSlot, fimSlot))
                            {
                                continue;
                            }

                            slots.Add(new SlotDisponivelResponseDto
                            {
                                ProfissionalId = profissionalId,
                                Inicio = inicioSlot,
                                Fim = fimSlot
                            });
                        }
                    }
                }
            }
        }

        return new DisponibilidadeAgendaResponseDto
        {
            ServicoId = servico.Id,
            DuracaoMinutos = servico.DuracaoMinutos,
            Slots = slots
                .OrderBy(slot => slot.Inicio)
                .ThenBy(slot => slot.ProfissionalId)
                .ToList()
        };
    }

    private async Task<IReadOnlyList<int>> ResolverProfissionaisAsync(
        int? profissionalId,
        Servico servico,
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        if (profissionalId.HasValue)
        {
            if (!await _profissionalServicoRepository.ExisteAtivoAsync(
                    profissionalId.Value,
                    servico.Id,
                    cancellationToken))
            {
                return [];
            }

            return [profissionalId.Value];
        }

        return await _profissionalServicoRepository.ListarProfissionaisAtivosPorServicoAsync(
            servico.Id,
            cancellationToken);
    }

    private async Task<bool> ProfissionalPodeAtenderAsync(
        int profissionalId,
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var vinculo = await _profissionalEstabelecimentoRepository.ObterPorProfissionalAsync(
            profissionalId,
            estabelecimentoId,
            cancellationToken);

        return vinculo?.Ativo == true && vinculo.PodeReceberAgendamento;
    }

    private static List<(TimeOnly Inicio, TimeOnly Fim)> IntersectarComFuncionamento(
        IReadOnlyList<HorarioFuncionamentoEstabelecimento> funcionamentos,
        HorarioAtendimentoProfissional horarioProfissional)
    {
        var janelas = new List<(TimeOnly Inicio, TimeOnly Fim)>();

        foreach (var funcionamento in funcionamentos)
        {
            var intersecao = GeradorSlotsDisponibilidade.Intersectar(
                funcionamento.HoraInicio,
                funcionamento.HoraFim,
                horarioProfissional.HoraInicio,
                horarioProfissional.HoraFim);

            if (intersecao.HasValue)
            {
                janelas.Add(intersecao.Value);
            }
        }

        return janelas;
    }

    private static bool SlotOcupado(
        IReadOnlyList<AgendamentoItem> ocupacao,
        int profissionalId,
        DateTime inicio,
        DateTime fim)
    {
        return ocupacao.Any(item =>
            item.ProfissionalId == profissionalId
            && item.Inicio < fim
            && item.Fim > inicio);
    }

    private static void ValidarPeriodo(DateOnly dataInicio, DateOnly dataFim)
    {
        if (dataFim < dataInicio)
        {
            throw new HorarioAtendimentoInvalidoException("A data final deve ser maior ou igual a data inicial.");
        }

        if (dataFim.DayNumber - dataInicio.DayNumber > MaximoDiasConsulta)
        {
            throw new HorarioAtendimentoInvalidoException($"O periodo de consulta nao pode exceder {MaximoDiasConsulta} dias.");
        }
    }
}
