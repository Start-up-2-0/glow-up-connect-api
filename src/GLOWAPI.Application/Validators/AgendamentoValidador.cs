using GLOWAPI.Application.DTOs.Agendamento;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Agendamento;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Validators;

public class AgendamentoValidador : IAgendamentoValidador
{
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IServicoRepository _servicoRepository;
    private readonly IProfissionalServicoRepository _profissionalServicoRepository;
    private readonly IHorarioFuncionamentoEstabelecimentoRepository _horarioFuncionamentoRepository;
    private readonly IHorarioAtendimentoProfissionalRepository _horarioAtendimentoProfissionalRepository;
    private readonly IAgendamentoItemRepository _agendamentoItemRepository;
    private readonly IUsuarioRepository _usuarioRepository;

    public AgendamentoValidador(
        IEstabelecimentoRepository estabelecimentoRepository,
        IProfissionalRepository profissionalRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IServicoRepository servicoRepository,
        IProfissionalServicoRepository profissionalServicoRepository,
        IHorarioFuncionamentoEstabelecimentoRepository horarioFuncionamentoRepository,
        IHorarioAtendimentoProfissionalRepository horarioAtendimentoProfissionalRepository,
        IAgendamentoItemRepository agendamentoItemRepository,
        IUsuarioRepository usuarioRepository)
    {
        _estabelecimentoRepository = estabelecimentoRepository;
        _profissionalRepository = profissionalRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _servicoRepository = servicoRepository;
        _profissionalServicoRepository = profissionalServicoRepository;
        _horarioFuncionamentoRepository = horarioFuncionamentoRepository;
        _horarioAtendimentoProfissionalRepository = horarioAtendimentoProfissionalRepository;
        _agendamentoItemRepository = agendamentoItemRepository;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<AgendamentoPreparacaoResultado> PrepararAsync(
        int estabelecimentoId,
        int profissionalId,
        int[] servicoIds,
        DateOnly data,
        TimeOnly horarioInicio,
        OrigemAgendamento origem,
        CriarAgendamentoRequestDto? dadosVisitante,
        int? usuarioClienteId,
        int? agendamentoIgnorarId = null,
        DateTime? inicioSelecionado = null,
        CancellationToken cancellationToken = default)
    {
        if (servicoIds.Length == 0)
        {
            throw new AgendamentoServicosInvalidosException("Informe ao menos um servico.");
        }

        var estabelecimento = await _estabelecimentoRepository.ObterPorIdAsync(estabelecimentoId, cancellationToken);
        if (estabelecimento is null || !estabelecimento.Ativo)
        {
            throw new NegocioNaoEncontradoException();
        }

        var profissional = await _profissionalRepository.ObterPorIdAsync(profissionalId, cancellationToken);
        if (profissional is null || !profissional.Ativo)
        {
            throw new RecursoProfissionalNaoEncontradoException();
        }

        var vinculoEstabelecimento = await _profissionalEstabelecimentoRepository.ObterPorProfissionalAsync(
            profissionalId,
            estabelecimentoId,
            cancellationToken);
        if (vinculoEstabelecimento is null || !vinculoEstabelecimento.Ativo)
        {
            throw new ProfissionalSemVinculoNegocioException();
        }

        if (!vinculoEstabelecimento.PodeReceberAgendamento
            && !vinculoEstabelecimento.SomenteExibicao)
        {
            throw new ProfissionalSemVinculoNegocioException();
        }

        var servicos = new List<Servico>();
        var vinculos = new List<ProfissionalServico?>();

        foreach (var servicoId in servicoIds.Distinct())
        {
            var servico = await _servicoRepository.ObterPorIdEEstabelecimentoAsync(
                servicoId,
                estabelecimentoId,
                ativo: true,
                cancellationToken);
            if (servico is null)
            {
                throw new ServicoNegocioNaoEncontradoException();
            }

            if (!ServicoExecucaoHelper.ProfissionalExecutaServico(servico, profissionalId))
            {
                throw new AgendamentoServicosInvalidosException(
                    "Profissional nao executa um ou mais servicos selecionados.");
            }

            ProfissionalServico? vinculoServico = null;
            if (ServicoExecucaoHelper.ServicoPossuiVinculosAtivos(servico))
            {
                vinculoServico = await _profissionalServicoRepository.ObterPorProfissionalEServicoAsync(
                    profissionalId,
                    servicoId,
                    cancellationToken);
                if (vinculoServico is null || !vinculoServico.Ativo)
                {
                    throw new AgendamentoServicosInvalidosException(
                        "Profissional nao executa um ou mais servicos selecionados.");
                }
            }

            servicos.Add(servico);
            vinculos.Add(vinculoServico);
        }

        var ordemServicos = servicoIds
            .Distinct()
            .Select(id => servicos.First(servico => servico.Id == id))
            .ToList();
        var ordemVinculos = servicoIds
            .Distinct()
            .Select(id => vinculos[servicos.FindIndex(servico => servico.Id == id)])
            .ToList();

        string? clienteNome = null;
        string? clienteEmail = null;
        string? clienteTelefone = null;

        if (usuarioClienteId.HasValue)
        {
            var usuario = await _usuarioRepository.ObterPorIdAsync(usuarioClienteId.Value, cancellationToken);
            if (usuario is null)
            {
                throw new AgendamentoDadosClienteInvalidosException("Usuario autenticado invalido.");
            }

            var cadastroPublicoPendente = origem == OrigemAgendamento.CadastroPublico && !usuario.Ativo;
            if (!usuario.Ativo && !cadastroPublicoPendente)
            {
                throw new AgendamentoDadosClienteInvalidosException("Usuario autenticado invalido.");
            }

            clienteNome = usuario.Nome;
            clienteEmail = usuario.Email;
            clienteTelefone = usuario.Telefone;
        }
        else
        {
            clienteNome = dadosVisitante?.ClienteNome?.Trim();
            clienteEmail = dadosVisitante?.ClienteEmail?.Trim();
            clienteTelefone = dadosVisitante?.ClienteTelefone?.Trim();

            if (string.IsNullOrWhiteSpace(clienteNome)
                || string.IsNullOrWhiteSpace(clienteEmail)
                || string.IsNullOrWhiteSpace(clienteTelefone))
            {
                throw new AgendamentoDadosClienteInvalidosException();
            }
        }

        var inicio = AgendaDateTimeHelper.ResolverInicio(data, horarioInicio, inicioSelecionado);
        if (inicio < DateTime.UtcNow)
        {
            throw new HorarioIndisponivelException("Nao e possivel agendar horarios no passado.");
        }

        var itensPreparacao = new List<AgendamentoItemPreparacao>();
        var cursor = inicio;
        decimal valorTotal = 0;

        for (var index = 0; index < ordemServicos.Count; index++)
        {
            var servico = ordemServicos[index];
            var vinculo = ordemVinculos[index];
            var duracao = ServicoPrecificacaoHelper.ObterDuracaoEfetiva(servico, vinculo);
            var valor = ServicoPrecificacaoHelper.ObterPrecoEfetivo(servico, vinculo);
            var fimItem = cursor.AddMinutes(duracao);

            itensPreparacao.Add(new AgendamentoItemPreparacao
            {
                Servico = servico,
                Vinculo = vinculo,
                Inicio = cursor,
                Fim = fimItem,
                Valor = valor
            });

            valorTotal += valor;
            cursor = fimItem;
        }

        var fim = cursor;
        await ValidarHorarioDentroDaAgendaAsync(
            estabelecimentoId,
            profissionalId,
            inicio,
            fim,
            exigirFuncionamentoEstabelecimento: profissional.TipoProfissional != ProfessionalType.Autonomo,
            cancellationToken);

        await ValidarConflitoAsync(
            estabelecimentoId,
            profissionalId,
            inicio,
            fim,
            agendamentoIgnorarId,
            cancellationToken);

        return new AgendamentoPreparacaoResultado
        {
            Estabelecimento = estabelecimento,
            Profissional = profissional,
            Servicos = ordemServicos,
            VinculosProfissionalServico = ordemVinculos,
            Inicio = inicio,
            Fim = fim,
            ValorTotal = valorTotal,
            DuracaoTotalMinutos = (int)(fim - inicio).TotalMinutes,
            Itens = itensPreparacao,
            ClienteNome = clienteNome,
            ClienteEmail = clienteEmail,
            ClienteTelefone = clienteTelefone,
            UsuarioClienteId = usuarioClienteId
        };
    }

    public async Task ValidarConflitoAsync(
        int estabelecimentoId,
        int profissionalId,
        DateTime inicio,
        DateTime fim,
        int? agendamentoIgnorarId,
        CancellationToken cancellationToken)
    {
        var ocupacao = await _agendamentoItemRepository.ListarOcupacaoAsync(
            estabelecimentoId,
            profissionalId,
            inicio,
            fim,
            cancellationToken);

        if (ocupacao.Any(item =>
                item.AgendamentoId != agendamentoIgnorarId
                && item.Inicio < fim
                && item.Fim > inicio))
        {
            throw new HorarioIndisponivelException();
        }
    }

    private async Task ValidarHorarioDentroDaAgendaAsync(
        int estabelecimentoId,
        int profissionalId,
        DateTime inicio,
        DateTime fim,
        bool exigirFuncionamentoEstabelecimento,
        CancellationToken cancellationToken)
    {
        var diaSemana = inicio.DayOfWeek;
        var horaInicio = TimeOnly.FromDateTime(inicio);
        var horaFim = TimeOnly.FromDateTime(fim);

        var horariosProfissional = await _horarioAtendimentoProfissionalRepository.ListarPorEstabelecimentoAsync(
            estabelecimentoId,
            profissionalId,
            diaSemana,
            ativo: true,
            cancellationToken);

        if (!exigirFuncionamentoEstabelecimento)
        {
            if (horariosProfissional.Count == 0)
            {
                throw new HorarioIndisponivelException("Profissional nao atende neste dia.");
            }

            var cabeNoAtendimento = horariosProfissional.Any(horario =>
                horaInicio >= horario.HoraInicio && horaFim <= horario.HoraFim);

            if (!cabeNoAtendimento)
            {
                throw new HorarioIndisponivelException("Horario fora do atendimento do profissional.");
            }

            return;
        }

        var funcionamentos = await _horarioFuncionamentoRepository.ListarAtivosPorEstabelecimentoEDiaAsync(
            estabelecimentoId,
            diaSemana,
            cancellationToken);
        if (funcionamentos.Count == 0)
        {
            throw new HorarioIndisponivelException("Estabelecimento fechado neste dia.");
        }

        if (horariosProfissional.Count == 0)
        {
            throw new HorarioIndisponivelException("Profissional nao atende neste dia.");
        }

        var cabeNaAgenda = false;

        foreach (var horarioProfissional in horariosProfissional)
        {
            foreach (var funcionamento in funcionamentos)
            {
                var intersecao = GeradorSlotsDisponibilidade.Intersectar(
                    funcionamento.HoraInicio,
                    funcionamento.HoraFim,
                    horarioProfissional.HoraInicio,
                    horarioProfissional.HoraFim);

                if (!intersecao.HasValue)
                {
                    continue;
                }

                if (horaInicio >= intersecao.Value.Inicio && horaFim <= intersecao.Value.Fim)
                {
                    cabeNaAgenda = true;
                    break;
                }
            }

            if (cabeNaAgenda)
            {
                break;
            }
        }

        if (!cabeNaAgenda)
        {
            throw new HorarioIndisponivelException("Horario fora do funcionamento ou atendimento do profissional.");
        }
    }
}
