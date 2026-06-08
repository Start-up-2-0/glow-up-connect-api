using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Assinaturas;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Services;

public class AssinaturaTitularContatoService : IAssinaturaTitularContatoService
{
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly ICurrentUserContext _currentUser;

    public AssinaturaTitularContatoService(
        IEstabelecimentoRepository estabelecimentoRepository,
        IEstabelecimentoUsuarioRepository estabelecimentoUsuarioRepository,
        ICurrentUserContext currentUser)
    {
        _estabelecimentoRepository = estabelecimentoRepository;
        _estabelecimentoUsuarioRepository = estabelecimentoUsuarioRepository;
        _currentUser = currentUser;
    }

    public async Task<AssinaturaTitularContato> ResolverAsync(
        Assinatura assinatura,
        CancellationToken cancellationToken = default)
    {
        var contato = new AssinaturaTitularContato();

        if (assinatura.EstabelecimentoId.HasValue)
        {
            var estabelecimento = assinatura.Estabelecimento
                ?? await _estabelecimentoRepository.ObterPorIdAsync(assinatura.EstabelecimentoId.Value, cancellationToken);

            if (estabelecimento is not null)
            {
                contato.EstabelecimentoId = estabelecimento.Id;
                contato.NomeEstabelecimento = estabelecimento.Nome;

                if (!string.IsNullOrWhiteSpace(estabelecimento.Email))
                {
                    contato.Email = estabelecimento.Email.Trim();
                }

                if (estabelecimento.PodeReceberAlertasWhatsApp())
                {
                    contato.TelefoneWhatsApp = TelefoneHelper.NormalizarParaWhatsApp(estabelecimento.Telefone);
                }
            }

            var owner = await _estabelecimentoUsuarioRepository.ObterOwnerAtivoAsync(
                assinatura.EstabelecimentoId.Value,
                cancellationToken);

            if (owner?.Usuario is not null)
            {
                contato.Nome = owner.Usuario.Nome;

                if (string.IsNullOrWhiteSpace(contato.Email) && !string.IsNullOrWhiteSpace(owner.Usuario.Email))
                {
                    contato.Email = owner.Usuario.Email.Trim();
                }
            }
        }

        if (string.IsNullOrWhiteSpace(contato.Email) && !string.IsNullOrWhiteSpace(_currentUser.Email))
        {
            contato.Email = _currentUser.Email.Trim();
        }

        return contato;
    }
}
