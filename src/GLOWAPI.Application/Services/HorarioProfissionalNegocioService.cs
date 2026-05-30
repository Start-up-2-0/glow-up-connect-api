using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class HorarioProfissionalNegocioService : IHorarioProfissionalNegocioService
{
    private readonly IHorarioAtendimentoProfissionalRepository _horarioAtendimentoProfissionalRepository;
    private readonly IHorarioFuncionamentoEstabelecimentoRepository _horarioFuncionamentoEstabelecimentoRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;

    public HorarioProfissionalNegocioService(
        IHorarioAtendimentoProfissionalRepository horarioAtendimentoProfissionalRepository,
        IHorarioFuncionamentoEstabelecimentoRepository horarioFuncionamentoEstabelecimentoRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService)
    {
        _horarioAtendimentoProfissionalRepository = horarioAtendimentoProfissionalRepository;
        _horarioFuncionamentoEstabelecimentoRepository = horarioFuncionamentoEstabelecimentoRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
    }

    public async Task<HorarioProfissionalResponseDto> CriarAsync(
        int estabelecimentoId,
        int profissionalId,
        CriarHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ProfissionalGerenciar,
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

        return HorarioProfissionalResponseDto.From(horario);
    }

    public async Task<HorarioProfissionalResponseDto> AtualizarStatusAsync(
        int estabelecimentoId,
        int horarioId,
        AtualizarStatusHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ProfissionalGerenciar,
            cancellationToken);

        var horario = await _horarioAtendimentoProfissionalRepository.ObterPorIdEEstabelecimentoAsync(
            horarioId,
            estabelecimentoId,
            cancellationToken);

        if (horario is null)
        {
            throw new HorarioAtendimentoNaoEncontradoException();
        }

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

        horario.Ativo = request.Ativo;
        horario.UpdatedAt = DateTime.UtcNow;

        _horarioAtendimentoProfissionalRepository.Atualizar(horario);
        await _horarioAtendimentoProfissionalRepository.SalvarAlteracoesAsync(cancellationToken);

        return HorarioProfissionalResponseDto.From(horario);
    }

    private async Task ValidarProfissionalAtivoAsync(
        int estabelecimentoId,
        int profissionalId,
        CancellationToken cancellationToken)
    {
        var vinculo = await _profissionalEstabelecimentoRepository.ObterPorProfissionalAsync(
            profissionalId,
            estabelecimentoId,
            cancellationToken);

        if (vinculo?.Ativo != true)
        {
            throw new ProfissionalSemVinculoNegocioException();
        }
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
        if (horaInicio >= horaFim)
        {
            throw new HorarioAtendimentoInvalidoException("A hora de inicio deve ser menor que a hora de fim.");
        }

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
