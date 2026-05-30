using System.Text.Json;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Services;

public class AuditoriaNegocioService : IAuditoriaNegocioService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IRepository<AuditoriaNegocio> _auditoriaRepository;
    private readonly ICurrentUserContext _currentUserContext;

    public AuditoriaNegocioService(
        IRepository<AuditoriaNegocio> auditoriaRepository,
        ICurrentUserContext currentUserContext)
    {
        _auditoriaRepository = auditoriaRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task RegistrarAsync(
        int estabelecimentoId,
        TipoAcaoAuditoriaNegocio tipoAcao,
        string entidade,
        int? entidadeId,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        var auditoria = new AuditoriaNegocio
        {
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = _currentUserContext.UserId,
            TipoAcao = tipoAcao,
            Entidade = entidade,
            EntidadeId = entidadeId,
            PayloadJson = payload is null ? "{}" : JsonSerializer.Serialize(payload, JsonOptions),
            CriadoEm = DateTime.UtcNow
        };

        await _auditoriaRepository.AdicionarAsync(auditoria, cancellationToken);
        await _auditoriaRepository.SalvarAlteracoesAsync(cancellationToken);
    }
}
