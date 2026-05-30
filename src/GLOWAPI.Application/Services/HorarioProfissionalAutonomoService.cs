using GLOWAPI.Application.DTOs.Horarios;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Domain.Exceptions.Auth;

namespace GLOWAPI.Application.Services;

public class HorarioProfissionalAutonomoService : IHorarioProfissionalAutonomoService
{
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IHorarioProfissionalNegocioService _horarioProfissionalNegocioService;
    private readonly ICurrentUserContext _currentUserContext;

    public HorarioProfissionalAutonomoService(
        IProfissionalRepository profissionalRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IHorarioProfissionalNegocioService horarioProfissionalNegocioService,
        ICurrentUserContext currentUserContext)
    {
        _profissionalRepository = profissionalRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _horarioProfissionalNegocioService = horarioProfissionalNegocioService;
        _currentUserContext = currentUserContext;
    }

    public async Task<IReadOnlyList<HorarioProfissionalResponseDto>> ListarAsync(
        int profissionalId,
        HorarioProfissionalAutonomoFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        var contexto = await ObterContextoAutonomoAsync(profissionalId, cancellationToken);

        return await _horarioProfissionalNegocioService.ListarAsync(
            contexto.EstabelecimentoId,
            new HorarioProfissionalFiltroDto
            {
                ProfissionalId = contexto.ProfissionalId,
                DiaSemana = filtro.DiaSemana,
                Ativo = filtro.Ativo
            },
            cancellationToken);
    }

    public async Task<HorarioProfissionalResponseDto> CriarAsync(
        int profissionalId,
        CriarHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var contexto = await ObterContextoAutonomoAsync(profissionalId, cancellationToken);

        return await _horarioProfissionalNegocioService.CriarAsync(
            contexto.EstabelecimentoId,
            contexto.ProfissionalId,
            request,
            cancellationToken);
    }

    public async Task<HorarioProfissionalResponseDto> AtualizarAsync(
        int profissionalId,
        int horarioId,
        AtualizarHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var contexto = await ObterContextoAutonomoAsync(profissionalId, cancellationToken);
        await GarantirHorarioDoProfissionalAsync(contexto, horarioId, cancellationToken);

        return await _horarioProfissionalNegocioService.AtualizarAsync(
            contexto.EstabelecimentoId,
            horarioId,
            request,
            cancellationToken);
    }

    public async Task<HorarioProfissionalResponseDto> AtualizarStatusAsync(
        int profissionalId,
        int horarioId,
        AtualizarStatusHorarioProfissionalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var contexto = await ObterContextoAutonomoAsync(profissionalId, cancellationToken);
        await GarantirHorarioDoProfissionalAsync(contexto, horarioId, cancellationToken);

        return await _horarioProfissionalNegocioService.AtualizarStatusAsync(
            contexto.EstabelecimentoId,
            horarioId,
            request,
            cancellationToken);
    }

    private async Task GarantirHorarioDoProfissionalAsync(
        ContextoProfissionalAutonomo contexto,
        int horarioId,
        CancellationToken cancellationToken)
    {
        var horarios = await _horarioProfissionalNegocioService.ListarAsync(
            contexto.EstabelecimentoId,
            new HorarioProfissionalFiltroDto
            {
                ProfissionalId = contexto.ProfissionalId
            },
            cancellationToken);

        if (!horarios.Any(horario => horario.Id == horarioId))
        {
            throw new UsuarioSemPermissaoAssinaturaException();
        }
    }

    private async Task<ContextoProfissionalAutonomo> ObterContextoAutonomoAsync(
        int profissionalId,
        CancellationToken cancellationToken)
    {
        var userId = ObterUsuarioAutenticado();
        var profissional = await _profissionalRepository.ObterPorIdAsync(profissionalId, cancellationToken);
        if (profissional is null || !profissional.Ativo || profissional.TipoProfissional != ProfessionalType.Autonomo)
        {
            throw new ProfissionalAutonomoAssinaturaInvalidoException("Profissional autonomo nao encontrado.");
        }

        if (profissional.UsuarioId != userId)
        {
            throw new UsuarioSemPermissaoAssinaturaException();
        }

        var vinculo = await _profissionalEstabelecimentoRepository.ObterAtivoPorProfissionalAsync(
            profissional.Id,
            cancellationToken);
        if (vinculo?.EstabelecimentoId is null)
        {
            throw new ProfissionalAutonomoAssinaturaInvalidoException("Profissional autonomo nao possui negocio vinculado.");
        }

        return new ContextoProfissionalAutonomo(profissional.Id, vinculo.EstabelecimentoId);
    }

    private int ObterUsuarioAutenticado()
    {
        if (!_currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedException();
        }

        return _currentUserContext.UserId.Value;
    }

    private sealed record ContextoProfissionalAutonomo(int ProfissionalId, int EstabelecimentoId);
}
