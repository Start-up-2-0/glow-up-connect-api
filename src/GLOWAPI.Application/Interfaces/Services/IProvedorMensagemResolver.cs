using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IProvedorMensagemResolver
{
    IProvedorMensagem Resolver(MensagemNotificacao mensagem);
}
