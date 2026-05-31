using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IEnderecoGeocodificacaoService
{
    Task TentarGeocodificarAsync(Endereco endereco, CancellationToken cancellationToken = default);
}
