using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IProvedorMensagem
{
    CanalMensagemNotificacao CanalSuportado { get; }

    Task<ResultadoEnvioMensagem> EnviarAsync(
        MensagemNotificacao mensagem,
        CancellationToken cancellationToken = default);
}
