using GLOWAPI.Application.DTOs.Mensageria;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IMensagemNotificacaoService
{
    Task<MensagemNotificacaoResponseDto> RegistrarAsync(
        RegistrarMensagemNotificacaoDto dto,
        CancellationToken cancellationToken = default);

    Task<MensagemNotificacaoResponseDto> RegistrarEnviadoAsync(
        RegistrarMensagemNotificacaoDto dto,
        string provedor,
        CancellationToken cancellationToken = default);

    Task CancelarPorGuidAsync(Guid guid, CancellationToken cancellationToken = default);
}
