using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IAuditoriaNegocioService
{
    Task RegistrarAsync(
        int estabelecimentoId,
        TipoAcaoAuditoriaNegocio tipoAcao,
        string entidade,
        int? entidadeId,
        object? payload = null,
        CancellationToken cancellationToken = default);
}
