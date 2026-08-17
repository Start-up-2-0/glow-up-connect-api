using GLOWAPI.Application.Models.Manutencao;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IRetencaoDadosService
{
    Task<RetencaoDadosResultado> ExecutarAsync(CancellationToken cancellationToken = default);
}
