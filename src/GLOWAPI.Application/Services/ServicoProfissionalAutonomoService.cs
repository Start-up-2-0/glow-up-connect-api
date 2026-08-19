using GLOWAPI.Application.DTOs.Equipe;
using GLOWAPI.Application.DTOs.Servicos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Domain.Exceptions.Auth;

namespace GLOWAPI.Application.Services;

public class ServicoProfissionalAutonomoService : IServicoProfissionalAutonomoService
{
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IServicoNegocioService _servicoNegocioService;
    private readonly IProfissionalServicoNegocioService _profissionalServicoNegocioService;
    private readonly ICurrentUserContext _currentUserContext;

    public ServicoProfissionalAutonomoService(
        IProfissionalRepository profissionalRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IServicoNegocioService servicoNegocioService,
        IProfissionalServicoNegocioService profissionalServicoNegocioService,
        ICurrentUserContext currentUserContext)
    {
        _profissionalRepository = profissionalRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _servicoNegocioService = servicoNegocioService;
        _profissionalServicoNegocioService = profissionalServicoNegocioService;
        _currentUserContext = currentUserContext;
    }

    public async Task<IReadOnlyList<ServicoResponseDto>> ListarAsync(
        int profissionalId,
        ServicoFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        var contexto = await ObterContextoAutonomoAsync(profissionalId, cancellationToken);

        return await _servicoNegocioService.ListarAsync(
            contexto.EstabelecimentoId,
            new ServicoFiltroDto
            {
                Ativo = filtro.Ativo,
                Nome = filtro.Nome,
                ProfissionalId = contexto.ProfissionalId
            },
            cancellationToken);
    }

    public async Task<ServicoResponseDto> CriarAsync(
        int profissionalId,
        CriarServicoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var contexto = await ObterContextoAutonomoAsync(profissionalId, cancellationToken);

        var servico = await _servicoNegocioService.CriarAsync(
            contexto.EstabelecimentoId,
            request,
            cancellationToken);

        if (!servico.Profissionais.Any(vinculo =>
                vinculo.ProfissionalId == contexto.ProfissionalId && vinculo.Ativo))
        {
            await _profissionalServicoNegocioService.VincularAsync(
                contexto.EstabelecimentoId,
                contexto.ProfissionalId,
                servico.Id,
                new VincularServicoProfissionalRequestDto(),
                cancellationToken);
        }

        var servicos = await _servicoNegocioService.ListarAsync(
            contexto.EstabelecimentoId,
            new ServicoFiltroDto { ProfissionalId = contexto.ProfissionalId },
            cancellationToken);

        return servicos.Single(item => item.Id == servico.Id);
    }

    public async Task<ServicoResponseDto> AtualizarAsync(
        int profissionalId,
        int servicoId,
        AtualizarServicoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var contexto = await ObterContextoAutonomoAsync(profissionalId, cancellationToken);
        await GarantirServicoDoProfissionalAsync(contexto, servicoId, cancellationToken);

        return await _servicoNegocioService.AtualizarAsync(
            contexto.EstabelecimentoId,
            servicoId,
            request,
            cancellationToken);
    }

    public async Task<ServicoResponseDto> AtualizarStatusAsync(
        int profissionalId,
        int servicoId,
        AtualizarStatusServicoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var contexto = await ObterContextoAutonomoAsync(profissionalId, cancellationToken);
        await GarantirServicoDoProfissionalAsync(contexto, servicoId, cancellationToken);

        return await _servicoNegocioService.AtualizarStatusAsync(
            contexto.EstabelecimentoId,
            servicoId,
            request,
            cancellationToken);
    }

    private async Task GarantirServicoDoProfissionalAsync(
        ContextoProfissionalAutonomo contexto,
        int servicoId,
        CancellationToken cancellationToken)
    {
        var servicos = await _servicoNegocioService.ListarAsync(
            contexto.EstabelecimentoId,
            new ServicoFiltroDto { ProfissionalId = contexto.ProfissionalId },
            cancellationToken);

        if (!servicos.Any(servico => servico.Id == servicoId))
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
