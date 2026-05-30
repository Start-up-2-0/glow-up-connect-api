using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Validators;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class HorarioProfissionalNegocioService : IHorarioProfissionalNegocioService
{
    private readonly IHorarioAtendimentoProfissionalRepository _horarioAtendimentoProfissionalRepository;
    private readonly IHorarioFuncionamentoEstabelecimentoRepository _horarioFuncionamentoEstabelecimentoRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IAgendamentoItemRepository _agendamentoItemRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IProfissionalEscopoAcessoService _profissionalEscopoAcessoService;
    private readonly IAuditoriaNegocioService _auditoriaNegocioService;

    public HorarioProfissionalNegocioService(
        IHorarioAtendimentoProfissionalRepository horarioAtendimentoProfissionalRepository,
        IHorarioFuncionamentoEstabelecimentoRepository horarioFuncionamentoEstabelecimentoRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IAgendamentoItemRepository agendamentoItemRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IProfissionalEscopoAcessoService profissionalEscopoAcessoService,
        IAuditoriaNegocioService auditoriaNegocioService)
    {
        _horarioAtendimentoProfissionalRepository = horarioAtendimentoProfissionalRepository;
        _horarioFuncionamentoEstabelecimentoRepository = horarioFuncionamentoEstabelecimentoRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _agendamentoItemRepository = agendamentoItemRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _profissionalEscopoAcessoService = profissionalEscopoAcessoService;
        _auditoriaNegocioService = auditoriaNegocioService;
    }

    public async Task<IReadOnlyList<HorarioProfissionalResponseDto>> ListarAsync(
        int estabelecimentoId,
        HorarioProfissionalFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        var autorizacao = await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.HorarioVisualizar,
            cancellationToken);

        var profissionalId = filtro.ProfissionalId;
        if (autorizacao.Role == EstablishmentUserRole.Profissional)
        {
            var escopo = await _profissionalEscopoAcessoService.ObterEscopoAsync(
                estabelecimentoId,
                cancellationToken);

            profissionalId = escopo.ProfissionalId;
        }

        var horarios = await _horarioAtendimentoProfissionalRepository.ListarPorEstabelecimentoAsync(
            estabelecimentoId,
            profissionalId,
            filtro.DiaSemana,
            filtro.Ativo,
            cancellationToken);

        return horarios.Select(HorarioProfissionalResponseDto.From).ToList();
    }

    public async Task<HorarioProfissionalResponseDto> CriarAsync(
        int estabelecimentoId,
        int profissionalId,
        CriarHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await AutorizarGerenciamentoHorarioProfissionalAsync(
            estabelecimentoId,
            profissionalId,
            cancellationToken);

        await ValidarProfissionalAtivoAsync(estabelecimentoId, profissionalId, cancellationToken);
        await ValidarHorarioAsync(
            estabelecimentoId,
            profissionalId,
            request.DiaSemana,
            request.HoraInicio,
            request.HoraFim,
            ignorarHorarioId: null,
            cancellationToken);

        var horario = new HorarioAtendimentoProfissional
        {
            EstabelecimentoId = estabelecimentoId,
            ProfissionalId = profissionalId,
            DiaSemana = request.DiaSemana,
            HoraInicio = request.HoraInicio,
            HoraFim = request.HoraFim,
            Ativo = true
        };

        await _horarioAtendimentoProfissionalRepository.AdicionarAsync(horario, cancellationToken);
        await _horarioAtendimentoProfissionalRepository.SalvarAlteracoesAsync(cancellationToken);
        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.HorarioProfissionalCriado,
            nameof(HorarioAtendimentoProfissional),
            horario.Id,
            new
            {
                horario.ProfissionalId,
                horario.DiaSemana,
                horario.HoraInicio,
                horario.HoraFim,
                horario.Ativo
            },
            cancellationToken);

        return HorarioProfissionalResponseDto.From(horario);
    }

    public async Task<HorarioProfissionalResponseDto> AtualizarAsync(
        int estabelecimentoId,
        int horarioId,
        AtualizarHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var horario = await _horarioAtendimentoProfissionalRepository.ObterPorIdEEstabelecimentoAsync(
            horarioId,
            estabelecimentoId,
            cancellationToken);

        if (horario is null)
        {
            throw new HorarioAtendimentoNaoEncontradoException();
        }

        await AutorizarGerenciamentoHorarioProfissionalAsync(
            estabelecimentoId,
            horario.ProfissionalId,
            cancellationToken);

        await ValidarProfissionalAtivoAsync(estabelecimentoId, horario.ProfissionalId, cancellationToken);
        await ValidarHorarioAsync(
            estabelecimentoId,
            horario.ProfissionalId,
            request.DiaSemana,
            request.HoraInicio,
            request.HoraFim,
            horario.Id,
            cancellationToken);
        await HorarioAgendamentoImpactoValidador.ValidarAlteracaoAsync(
            _agendamentoItemRepository,
            estabelecimentoId,
            horario.ProfissionalId,
            horario.DiaSemana,
            horario.HoraInicio,
            horario.HoraFim,
            request.DiaSemana,
            request.HoraInicio,
            request.HoraFim,
            cancellationToken);

        var alteracaoAnterior = new
        {
            horario.ProfissionalId,
            horario.DiaSemana,
            horario.HoraInicio,
            horario.HoraFim,
            horario.Ativo
        };

        horario.DiaSemana = request.DiaSemana;
        horario.HoraInicio = request.HoraInicio;
        horario.HoraFim = request.HoraFim;
        horario.UpdatedAt = DateTime.UtcNow;

        _horarioAtendimentoProfissionalRepository.Atualizar(horario);
        await _horarioAtendimentoProfissionalRepository.SalvarAlteracoesAsync(cancellationToken);
        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.HorarioProfissionalAlterado,
            nameof(HorarioAtendimentoProfissional),
            horario.Id,
            new
            {
                anterior = alteracaoAnterior,
                atual = new
                {
                    horario.ProfissionalId,
                    horario.DiaSemana,
                    horario.HoraInicio,
                    horario.HoraFim,
                    horario.Ativo
                }
            },
            cancellationToken);

        return HorarioProfissionalResponseDto.From(horario);
    }

    public async Task<HorarioProfissionalResponseDto> AtualizarStatusAsync(
        int estabelecimentoId,
        int horarioId,
        AtualizarStatusHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var horario = await _horarioAtendimentoProfissionalRepository.ObterPorIdEEstabelecimentoAsync(
            horarioId,
            estabelecimentoId,
            cancellationToken);

        if (horario is null)
        {
            throw new HorarioAtendimentoNaoEncontradoException();
        }

        await AutorizarGerenciamentoHorarioProfissionalAsync(
            estabelecimentoId,
            horario.ProfissionalId,
            cancellationToken);

        if (request.Ativo)
        {
            await ValidarProfissionalAtivoAsync(estabelecimentoId, horario.ProfissionalId, cancellationToken);
            await ValidarHorarioAsync(
                estabelecimentoId,
                horario.ProfissionalId,
                horario.DiaSemana,
                horario.HoraInicio,
                horario.HoraFim,
                horario.Id,
                cancellationToken);
        }
        else if (horario.Ativo)
        {
            await HorarioAgendamentoImpactoValidador.ValidarInativacaoAsync(
                _agendamentoItemRepository,
                estabelecimentoId,
                horario.ProfissionalId,
                horario.DiaSemana,
                horario.HoraInicio,
                horario.HoraFim,
                cancellationToken);
        }

        var statusAnterior = horario.Ativo;
        horario.Ativo = request.Ativo;
        horario.UpdatedAt = DateTime.UtcNow;

        _horarioAtendimentoProfissionalRepository.Atualizar(horario);
        await _horarioAtendimentoProfissionalRepository.SalvarAlteracoesAsync(cancellationToken);
        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.HorarioProfissionalStatusAlterado,
            nameof(HorarioAtendimentoProfissional),
            horario.Id,
            new
            {
                horario.ProfissionalId,
                statusAnterior,
                statusNovo = horario.Ativo,
                horario.DiaSemana,
                horario.HoraInicio,
                horario.HoraFim
            },
            cancellationToken);

        return HorarioProfissionalResponseDto.From(horario);
    }

    private async Task AutorizarGerenciamentoHorarioProfissionalAsync(
        int estabelecimentoId,
        int profissionalId,
        CancellationToken cancellationToken)
    {
        if (await _autorizacaoNegocioService.PossuiPermissaoAsync(
                estabelecimentoId,
                PermissaoNegocio.HorarioGerenciar,
                cancellationToken))
        {
            return;
        }

        if (!await _autorizacaoNegocioService.PossuiPermissaoAsync(
                estabelecimentoId,
                PermissaoNegocio.HorarioGerenciarProprio,
                cancellationToken))
        {
            throw new UsuarioSemPermissaoNegocioException();
        }

        var escopo = await _profissionalEscopoAcessoService.ObterEscopoAsync(
            estabelecimentoId,
            cancellationToken);

        if (escopo.ProfissionalId != profissionalId)
        {
            throw new RecursoForaEscopoProfissionalException();
        }
    }

    private async Task ValidarProfissionalAtivoAsync(
        int estabelecimentoId,
        int profissionalId,
        CancellationToken cancellationToken)
    {
        _ = await ObterVinculoProfissionalAtivoAsync(estabelecimentoId, profissionalId, cancellationToken);
    }

    private async Task<ProfissionalEstabelecimento> ObterVinculoProfissionalAtivoAsync(
        int estabelecimentoId,
        int? profissionalId,
        CancellationToken cancellationToken)
    {
        if (!profissionalId.HasValue)
        {
            throw new ProfissionalSemVinculoNegocioException();
        }

        var vinculo = await _profissionalEstabelecimentoRepository.ObterPorProfissionalAsync(
            profissionalId.Value,
            estabelecimentoId,
            cancellationToken);

        if (vinculo?.Ativo != true || vinculo.Profissional?.Ativo != true)
        {
            throw new ProfissionalSemVinculoNegocioException();
        }

        return vinculo;
    }

    private async Task ValidarHorarioAsync(
        int estabelecimentoId,
        int profissionalId,
        DayOfWeek diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFim,
        int? ignorarHorarioId,
        CancellationToken cancellationToken)
    {
        HorarioIntervaloValidador.ValidarIntervalo(horaInicio, horaFim);

        var vinculo = await ObterVinculoProfissionalAtivoAsync(estabelecimentoId, profissionalId, cancellationToken);
        if (vinculo.Profissional?.TipoProfissional != ProfessionalType.Autonomo)
        {
            var funcionamento = await _horarioFuncionamentoEstabelecimentoRepository.ListarAtivosPorEstabelecimentoEDiaAsync(
                estabelecimentoId,
                diaSemana,
                cancellationToken);

            var dentroDoFuncionamento = funcionamento.Any(horario =>
                horario.HoraInicio <= horaInicio && horario.HoraFim >= horaFim);

            if (!dentroDoFuncionamento)
            {
                throw new HorarioAtendimentoInvalidoException("O horario do profissional deve estar dentro do funcionamento ativo do negocio.");
            }
        }

        var possuiConflito = await _horarioAtendimentoProfissionalRepository.ExisteConflitoAtivoAsync(
            profissionalId,
            estabelecimentoId,
            diaSemana,
            horaInicio,
            horaFim,
            ignorarHorarioId,
            cancellationToken);

        if (possuiConflito)
        {
            throw new HorarioAtendimentoConflitanteException();
        }
    }
}
