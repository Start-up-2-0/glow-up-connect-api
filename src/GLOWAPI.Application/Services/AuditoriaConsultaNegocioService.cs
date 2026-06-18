using GLOWAPI.Application.DTOs.Auditoria;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Services;

public class AuditoriaConsultaNegocioService : IAuditoriaConsultaNegocioService
{
    private readonly IAuditoriaNegocioRepository _auditoriaNegocioRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;

    public AuditoriaConsultaNegocioService(
        IAuditoriaNegocioRepository auditoriaNegocioRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService)
    {
        _auditoriaNegocioRepository = auditoriaNegocioRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
    }

    public async Task<IReadOnlyList<AuditoriaNegocioResponseDto>> ListarRecentesAsync(
        int estabelecimentoId,
        int limite,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.NegocioVisualizar,
            cancellationToken);

        var limiteNormalizado = Math.Clamp(limite, 1, 200);
        var registros = await _auditoriaNegocioRepository.ListarRecentesPorEstabelecimentoAsync(
            estabelecimentoId,
            limiteNormalizado,
            cancellationToken);

        return registros
            .Select(registro => new AuditoriaNegocioResponseDto(
                registro.Id,
                registro.EstabelecimentoId,
                registro.UsuarioId,
                registro.TipoAcao.ToString(),
                registro.Entidade,
                registro.EntidadeId,
                registro.PayloadJson,
                registro.CriadoEm))
            .ToList();
    }
}
