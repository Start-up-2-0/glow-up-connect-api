using GLOWAPI.Application.Models.Mensageria;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IWhatsAppEnvioImediatoService
{
    Task<ResultadoEnvioMensagem> EnviarTextoAsync(
        string destinatario,
        string conteudo,
        CancellationToken cancellationToken = default,
        string? remoteJidConversa = null,
        string? remoteJidAlt = null);
}
