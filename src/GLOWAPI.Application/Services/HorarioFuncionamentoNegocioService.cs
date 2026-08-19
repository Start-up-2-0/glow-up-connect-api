using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Validators;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class HorarioFuncionamentoNegocioService : IHorarioFuncionamentoNegocioService
{
    private readonly IHorarioFuncionamentoEstabelecimentoRepository _horarioFuncionamentoRepository;
    private readonly IHorarioAtendimentoProfissionalRepository _horarioAtendimentoProfissionalRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IAuditoriaNegocioService _auditoriaNegocioService;
    private readonly IOnboardingPublicacaoService _onboardingPublicacaoService;

    public HorarioFuncionamentoNegocioService(
        IHorarioFuncionamentoEstabelecimentoRepository horarioFuncionamentoRepository,
        IHorarioAtendimentoProfissionalRepository horarioAtendimentoProfissionalRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IAuditoriaNegocioService auditoriaNegocioService,
        IOnboardingPublicacaoService onboardingPublicacaoService)
    {
        _horarioFuncionamentoRepository = horarioFuncionamentoRepository;
        _horarioAtendimentoProfissionalRepository = horarioAtendimentoProfissionalRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _auditoriaNegocioService = auditoriaNegocioService;
        _onboardingPublicacaoService = onboardingPublicacaoService;
    }

    public async Task<IReadOnlyList<HorarioFuncionamentoResponseDto>> ListarAsync(
        int estabelecimentoId,
        HorarioFuncionamentoFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.HorarioVisualizar,
            cancellationToken);

        var horarios = await _horarioFuncionamentoRepository.ListarPorEstabelecimentoAsync(
            estabelecimentoId,
            filtro.DiaSemana,
            filtro.Ativo,
            cancellationToken);

        return horarios
            .Select(HorarioFuncionamentoResponseDto.From)
            .ToList();
    }

    public async Task<HorarioFuncionamentoResponseDto> CriarAsync(
        int estabelecimentoId,
        CriarHorarioFuncionamentoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.HorarioGerenciar,
            cancellationToken);

        HorarioIntervaloValidador.ValidarIntervalo(request.HoraInicio, request.HoraFim);
        await ValidarConflitoAsync(
            estabelecimentoId,
            request.DiaSemana,
            request.HoraInicio,
            request.HoraFim,
            ignorarHorarioId: null,
            cancellationToken);

        var horario = new HorarioFuncionamentoEstabelecimento
        {
            EstabelecimentoId = estabelecimentoId,
            DiaSemana = request.DiaSemana,
            HoraInicio = request.HoraInicio,
            HoraFim = request.HoraFim,
            Ativo = true
        };

        await _horarioFuncionamentoRepository.AdicionarAsync(horario, cancellationToken);
        await _horarioFuncionamentoRepository.SalvarAlteracoesAsync(cancellationToken);
        await SincronizarAtendimentoAutonomoAsync(horario, cancellationToken);
        await _onboardingPublicacaoService.RecalcularVisibilidadeAsync(estabelecimentoId, cancellationToken);
        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.HorarioFuncionamentoCriado,
            nameof(HorarioFuncionamentoEstabelecimento),
            horario.Id,
            new
            {
                horario.DiaSemana,
                horario.HoraInicio,
                horario.HoraFim,
                horario.Ativo
            },
            cancellationToken);

        return HorarioFuncionamentoResponseDto.From(horario);
    }

    public async Task<HorarioFuncionamentoResponseDto> AtualizarAsync(
        int estabelecimentoId,
        int horarioId,
        AtualizarHorarioFuncionamentoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.HorarioGerenciar,
            cancellationToken);

        HorarioIntervaloValidador.ValidarIntervalo(request.HoraInicio, request.HoraFim);

        var horario = await _horarioFuncionamentoRepository.ObterPorIdEEstabelecimentoAsync(
            horarioId,
            estabelecimentoId,
            cancellationToken);

        if (horario is null)
        {
            throw new HorarioFuncionamentoNaoEncontradoException();
        }

        await ValidarConflitoAsync(
            estabelecimentoId,
            request.DiaSemana,
            request.HoraInicio,
            request.HoraFim,
            horario.Id,
            cancellationToken);

        if (horario.Ativo)
        {
            await ValidarImpactoEmHorariosProfissionaisAsync(
                horario,
                request,
                incluirHorarioAtualNaProjecao: false,
                incluirRequestNaProjecao: true,
                cancellationToken);
        }

        var alteracaoAnterior = new
        {
            horario.DiaSemana,
            horario.HoraInicio,
            horario.HoraFim,
            horario.Ativo
        };

        horario.DiaSemana = request.DiaSemana;
        horario.HoraInicio = request.HoraInicio;
        horario.HoraFim = request.HoraFim;
        horario.UpdatedAt = DateTime.UtcNow;

        _horarioFuncionamentoRepository.Atualizar(horario);
        await _horarioFuncionamentoRepository.SalvarAlteracoesAsync(cancellationToken);
        await SincronizarAtendimentoAutonomoAsync(horario, cancellationToken);
        await _onboardingPublicacaoService.RecalcularVisibilidadeAsync(estabelecimentoId, cancellationToken);
        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.HorarioFuncionamentoAlterado,
            nameof(HorarioFuncionamentoEstabelecimento),
            horario.Id,
            new
            {
                anterior = alteracaoAnterior,
                atual = new
                {
                    horario.DiaSemana,
                    horario.HoraInicio,
                    horario.HoraFim,
                    horario.Ativo
                }
            },
            cancellationToken);

        return HorarioFuncionamentoResponseDto.From(horario);
    }

    public async Task<HorarioFuncionamentoResponseDto> AtualizarStatusAsync(
        int estabelecimentoId,
        int horarioId,
        AtualizarStatusHorarioFuncionamentoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.HorarioGerenciar,
            cancellationToken);

        var horario = await _horarioFuncionamentoRepository.ObterPorIdEEstabelecimentoAsync(
            horarioId,
            estabelecimentoId,
            cancellationToken);

        if (horario is null)
        {
            throw new HorarioFuncionamentoNaoEncontradoException();
        }

        if (!request.Ativo && horario.Ativo)
        {
            await ValidarImpactoAoInativarAsync(horario, cancellationToken);
        }

        if (request.Ativo)
        {
            HorarioIntervaloValidador.ValidarIntervalo(horario.HoraInicio, horario.HoraFim);
            await ValidarConflitoAsync(
                estabelecimentoId,
                horario.DiaSemana,
                horario.HoraInicio,
                horario.HoraFim,
                horario.Id,
                cancellationToken);
        }

        var statusAnterior = horario.Ativo;
        horario.Ativo = request.Ativo;
        horario.UpdatedAt = DateTime.UtcNow;

        _horarioFuncionamentoRepository.Atualizar(horario);
        await _horarioFuncionamentoRepository.SalvarAlteracoesAsync(cancellationToken);
        await SincronizarAtendimentoAutonomoAsync(horario, cancellationToken);
        await _onboardingPublicacaoService.RecalcularVisibilidadeAsync(estabelecimentoId, cancellationToken);
        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.HorarioFuncionamentoStatusAlterado,
            nameof(HorarioFuncionamentoEstabelecimento),
            horario.Id,
            new
            {
                statusAnterior,
                statusNovo = horario.Ativo,
                horario.DiaSemana,
                horario.HoraInicio,
                horario.HoraFim
            },
            cancellationToken);

        return HorarioFuncionamentoResponseDto.From(horario);
    }

    private async Task ValidarConflitoAsync(
        int estabelecimentoId,
        DayOfWeek diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFim,
        int? ignorarHorarioId,
        CancellationToken cancellationToken)
    {
        var possuiConflito = await _horarioFuncionamentoRepository.ExisteConflitoAtivoAsync(
            estabelecimentoId,
            diaSemana,
            horaInicio,
            horaFim,
            ignorarHorarioId,
            cancellationToken);

        if (possuiConflito)
        {
            throw new HorarioAtendimentoConflitanteException(
                "Ja existe horario de funcionamento ativo conflitante neste periodo.");
        }
    }

    private Task ValidarImpactoAoInativarAsync(
        HorarioFuncionamentoEstabelecimento horarioAtual,
        CancellationToken cancellationToken)
    {
        return ValidarImpactoEmHorariosProfissionaisAsync(
            horarioAtual,
            new AtualizarHorarioFuncionamentoRequestDto
            {
                DiaSemana = horarioAtual.DiaSemana,
                HoraInicio = horarioAtual.HoraInicio,
                HoraFim = horarioAtual.HoraFim
            },
            incluirHorarioAtualNaProjecao: false,
            incluirRequestNaProjecao: false,
            cancellationToken);
    }

    private async Task ValidarImpactoEmHorariosProfissionaisAsync(
        HorarioFuncionamentoEstabelecimento horarioAtual,
        AtualizarHorarioFuncionamentoRequestDto request,
        bool incluirHorarioAtualNaProjecao,
        bool incluirRequestNaProjecao,
        CancellationToken cancellationToken)
    {
        var diasAfetados = new HashSet<DayOfWeek> { horarioAtual.DiaSemana, request.DiaSemana };

        foreach (var diaSemana in diasAfetados)
        {
            var horariosProfissionais = await _horarioAtendimentoProfissionalRepository.ListarAtivosPorEstabelecimentoEDiaAsync(
                horarioAtual.EstabelecimentoId,
                diaSemana,
                cancellationToken);

            if (horariosProfissionais.Count == 0)
            {
                continue;
            }

            var funcionamentoDoDia = await _horarioFuncionamentoRepository.ListarAtivosPorEstabelecimentoEDiaAsync(
                horarioAtual.EstabelecimentoId,
                diaSemana,
                cancellationToken);

            var funcionamentoProjetado = funcionamentoDoDia
                .Where(horario => incluirHorarioAtualNaProjecao || horario.Id != horarioAtual.Id)
                .Select(horario => (horario.HoraInicio, horario.HoraFim))
                .ToList();

            if (incluirRequestNaProjecao && diaSemana == request.DiaSemana)
            {
                funcionamentoProjetado.Add((request.HoraInicio, request.HoraFim));
            }

            var possuiProfissionalForaDoFuncionamento = horariosProfissionais.Any(horarioProfissional =>
                !funcionamentoProjetado.Any(funcionamento =>
                    funcionamento.HoraInicio <= horarioProfissional.HoraInicio
                    && funcionamento.HoraFim >= horarioProfissional.HoraFim));

            if (possuiProfissionalForaDoFuncionamento)
            {
                throw new HorarioAtendimentoInvalidoException(
                    "A alteracao deixaria horarios ativos de profissionais fora do funcionamento do negocio.");
            }
        }
    }

    private async Task SincronizarAtendimentoAutonomoAsync(
        HorarioFuncionamentoEstabelecimento horario,
        CancellationToken cancellationToken)
    {
        var vinculos = await _profissionalEstabelecimentoRepository
            .ListarAtivosComAgendamentoPorEstabelecimentoAsync(horario.EstabelecimentoId, cancellationToken);
        var autonomos = vinculos
            .Where(vinculo => vinculo.Profissional?.TipoProfissional == ProfessionalType.Autonomo)
            .Take(2)
            .ToList();

        if (autonomos.Count != 1)
        {
            return;
        }

        var profissionalId = autonomos[0].ProfissionalId;
        var existentes = await _horarioAtendimentoProfissionalRepository.ListarPorEstabelecimentoAsync(
            horario.EstabelecimentoId,
            profissionalId,
            horario.DiaSemana,
            cancellationToken: cancellationToken);
        var existenteId = existentes.FirstOrDefault()?.Id;

        if (!existenteId.HasValue)
        {
            if (!horario.Ativo)
            {
                return;
            }

            await _horarioAtendimentoProfissionalRepository.AdicionarAsync(
                new HorarioAtendimentoProfissional
                {
                    EstabelecimentoId = horario.EstabelecimentoId,
                    ProfissionalId = profissionalId,
                    DiaSemana = horario.DiaSemana,
                    HoraInicio = horario.HoraInicio,
                    HoraFim = horario.HoraFim,
                    Ativo = true
                },
                cancellationToken);
        }
        else
        {
            var alvo = await _horarioAtendimentoProfissionalRepository.ObterPorIdEEstabelecimentoAsync(
                existenteId.Value,
                horario.EstabelecimentoId,
                cancellationToken);
            if (alvo is null)
            {
                return;
            }

            alvo.HoraInicio = horario.HoraInicio;
            alvo.HoraFim = horario.HoraFim;
            alvo.Ativo = horario.Ativo;
            alvo.UpdatedAt = DateTime.UtcNow;
            _horarioAtendimentoProfissionalRepository.Atualizar(alvo);
        }

        await _horarioAtendimentoProfissionalRepository.SalvarAlteracoesAsync(cancellationToken);
    }
}
