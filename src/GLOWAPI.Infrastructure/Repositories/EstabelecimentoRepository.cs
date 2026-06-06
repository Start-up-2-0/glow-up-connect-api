using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Models.Geolocalizacao;
using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class EstabelecimentoRepository : Repository<Estabelecimento>, IEstabelecimentoRepository
{
    public EstabelecimentoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<Estabelecimento?> ObterPorPublicGuidAsync(Guid publicGuid, CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(estabelecimento => estabelecimento.Endereco)
            .FirstOrDefaultAsync(estabelecimento => estabelecimento.PublicGuid == publicGuid, cancellationToken);
    }

    public Task<Estabelecimento?> ObterPorIdComEnderecoAsync(int id, CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(estabelecimento => estabelecimento.Endereco)
            .FirstOrDefaultAsync(estabelecimento => estabelecimento.Id == id, cancellationToken);
    }

    public Task<Estabelecimento?> ObterPorWhatsAppConfirmacaoTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            estabelecimento => estabelecimento.WhatsAppConfirmacaoTokenHash == tokenHash,
            cancellationToken);
    }

    public Task<Estabelecimento?> ObterPorWhatsAppConfirmacaoCodigoHashAsync(
        string codigoHash,
        CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(
            estabelecimento => estabelecimento.WhatsAppConfirmacaoCodigoHash == codigoHash,
            cancellationToken);
    }

    public async Task<Estabelecimento?> ObterPorTelefoneNormalizadoAsync(
        string telefoneNormalizado,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(telefoneNormalizado))
        {
            return null;
        }

        var candidatos = await DbSet
            .Where(estabelecimento => estabelecimento.Telefone != null && estabelecimento.Telefone != string.Empty)
            .ToListAsync(cancellationToken);

        return candidatos.FirstOrDefault(estabelecimento =>
            TelefoneHelper.SaoEquivalentes(estabelecimento.Telefone, telefoneNormalizado));
    }

    public async Task<(IReadOnlyList<EstabelecimentoProximoConsulta> Itens, int Total)> ListarProximosAsync(
        string cidadeNormalizada,
        string estado,
        decimal latitudeCliente,
        decimal longitudeCliente,
        double raioKm,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default)
    {
        var estadoNormalizado = estado.Trim().ToUpperInvariant();

        var candidatos = await DbSet
            .AsNoTracking()
            .Include(estabelecimento => estabelecimento.Endereco)
            .Where(estabelecimento =>
                estabelecimento.Ativo
                && estabelecimento.Endereco != null
                && estabelecimento.Endereco.Latitude != null
                && estabelecimento.Endereco.Longitude != null
                && estabelecimento.Endereco.Estado.ToUpper() == estadoNormalizado)
            .ToListAsync(cancellationToken);

        var filtrados = candidatos
            .Where(estabelecimento =>
                GeolocalizacaoHelper.NormalizarTextoLocalizacao(estabelecimento.Endereco!.Cidade) == cidadeNormalizada)
            .Select(estabelecimento => new EstabelecimentoProximoConsulta(
                estabelecimento,
                GeolocalizacaoHelper.CalcularDistanciaKm(
                    latitudeCliente,
                    longitudeCliente,
                    estabelecimento.Endereco!.Latitude!.Value,
                    estabelecimento.Endereco.Longitude!.Value)))
            .Where(consulta => consulta.DistanciaKm <= raioKm)
            .OrderBy(consulta => consulta.DistanciaKm)
            .ToList();

        var total = filtrados.Count;
        var itens = filtrados
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToList();

        return (itens, total);
    }
}
