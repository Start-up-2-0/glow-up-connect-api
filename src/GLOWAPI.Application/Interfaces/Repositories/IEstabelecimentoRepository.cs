using GLOWAPI.Application.Models.Geolocalizacao;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Interfaces.Repositories;

public interface IEstabelecimentoRepository : IRepository<Estabelecimento>
{
    Task<Estabelecimento?> ObterPorPublicGuidAsync(Guid publicGuid, CancellationToken cancellationToken = default);
    Task<Estabelecimento?> ObterPorIdComEnderecoAsync(int id, CancellationToken cancellationToken = default);
    Task<Estabelecimento?> ObterPorWhatsAppConfirmacaoTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<Estabelecimento?> ObterPorWhatsAppConfirmacaoCodigoHashAsync(string codigoHash, CancellationToken cancellationToken = default);
    Task<Estabelecimento?> ObterPorTelefoneNormalizadoAsync(string telefoneNormalizado, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<EstabelecimentoProximoConsulta> Itens, int Total)> ListarProximosAsync(
        string cidadeNormalizada,
        string estado,
        decimal latitudeCliente,
        decimal longitudeCliente,
        double raioKm,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default);
}
