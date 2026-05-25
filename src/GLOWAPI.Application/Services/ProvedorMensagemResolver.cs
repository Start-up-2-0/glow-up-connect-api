using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Services;

public class ProvedorMensagemResolver : IProvedorMensagemResolver
{
    private readonly IReadOnlyDictionary<CanalMensagemNotificacao, IProvedorMensagem> _provedores;

    public ProvedorMensagemResolver(IEnumerable<IProvedorMensagem> provedores)
    {
        _provedores = provedores.ToDictionary(p => p.CanalSuportado);
    }

    public IProvedorMensagem Resolver(MensagemNotificacao mensagem)
    {
        if (!_provedores.TryGetValue(mensagem.Canal, out var provedor))
        {
            throw new InvalidOperationException($"Nenhum provedor registrado para o canal {mensagem.Canal}.");
        }

        return provedor;
    }
}
