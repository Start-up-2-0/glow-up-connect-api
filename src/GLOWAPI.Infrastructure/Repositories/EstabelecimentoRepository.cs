using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Models.Geolocalizacao;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure.Repositories;

public class EstabelecimentoRepository : Repository<Estabelecimento>, IEstabelecimentoRepository
{
    public EstabelecimentoRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<CategoriaEstabelecimento>> ListarCategoriasAsync(
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<CategoriaEstabelecimento>()
            .AsNoTracking()
            .Where(categoria => categoria.Ativo)
            .OrderBy(categoria => categoria.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Estabelecimento?> ObterPorPublicGuidAsync(Guid publicGuid, CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(estabelecimento => estabelecimento.Endereco)
            .Include(estabelecimento => estabelecimento.CategoriaEstabelecimento)
            .FirstOrDefaultAsync(estabelecimento => estabelecimento.PublicGuid == publicGuid, cancellationToken);
    }

    public Task<Estabelecimento?> ObterPorIdComEnderecoAsync(int id, CancellationToken cancellationToken = default)
    {
        return DbSet
            .Include(estabelecimento => estabelecimento.Endereco)
            .Include(estabelecimento => estabelecimento.CategoriaEstabelecimento)
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
        int? categoriaId,
        CancellationToken cancellationToken = default)
    {
        var estadoNormalizado = estado.Trim().ToUpperInvariant();

        // Bounding box aproxima o raio e evita materializar o estado inteiro + longtext.
        var deltaLat = (decimal)(raioKm / 111.0d * 1.35d);
        var cosLat = Math.Cos((double)latitudeCliente * Math.PI / 180d);
        var deltaLng = (decimal)(raioKm / (111.0d * Math.Max(0.01d, Math.Abs(cosLat))) * 1.35d);
        var latMin = latitudeCliente - deltaLat;
        var latMax = latitudeCliente + deltaLat;
        var lngMin = longitudeCliente - deltaLng;
        var lngMax = longitudeCliente + deltaLng;

        var candidatos = await DbSet
            .AsNoTracking()
            .Where(estabelecimento =>
                estabelecimento.Ativo
                && estabelecimento.VisivelPublicamente
                && estabelecimento.Endereco != null
                && estabelecimento.Endereco.Latitude != null
                && estabelecimento.Endereco.Longitude != null
                && estabelecimento.Endereco.Estado.ToUpper() == estadoNormalizado
                && estabelecimento.Endereco.Latitude >= latMin
                && estabelecimento.Endereco.Latitude <= latMax
                && estabelecimento.Endereco.Longitude >= lngMin
                && estabelecimento.Endereco.Longitude <= lngMax
                && (categoriaId == null || estabelecimento.CategoriaEstabelecimentoId == categoriaId))
            .Select(estabelecimento => new
            {
                estabelecimento.Id,
                estabelecimento.PublicGuid,
                estabelecimento.Nome,
                estabelecimento.Descricao,
                estabelecimento.NotaMedia,
                estabelecimento.TotalAvaliacoes,
                estabelecimento.CategoriaEstabelecimentoId,
                CategoriaNome = estabelecimento.CategoriaEstabelecimento != null
                    ? estabelecimento.CategoriaEstabelecimento.Nome
                    : null,
                estabelecimento.Endereco!.Logradouro,
                estabelecimento.Endereco.Bairro,
                estabelecimento.Endereco.Cidade,
                estabelecimento.Endereco.Estado,
                Latitude = estabelecimento.Endereco.Latitude!.Value,
                Longitude = estabelecimento.Endereco.Longitude!.Value,
            })
            .ToListAsync(cancellationToken);

        var naCidade = candidatos
            .Where(estabelecimento =>
                GeolocalizacaoHelper.NormalizarTextoLocalizacao(estabelecimento.Cidade) == cidadeNormalizada)
            .ToList();

        var estabelecimentoIds = naCidade.Select(estabelecimento => estabelecimento.Id).ToList();
        var destaqueIds = await ObterEstabelecimentosDestaqueAsync(estabelecimentoIds, cancellationToken);

        var filtrados = naCidade
            .Select(estabelecimento => new EstabelecimentoProximoConsulta(
                estabelecimento.Id,
                estabelecimento.PublicGuid,
                estabelecimento.Nome,
                estabelecimento.Descricao,
                estabelecimento.NotaMedia,
                estabelecimento.TotalAvaliacoes,
                estabelecimento.CategoriaEstabelecimentoId,
                estabelecimento.CategoriaNome,
                estabelecimento.Logradouro,
                estabelecimento.Bairro,
                estabelecimento.Cidade,
                estabelecimento.Estado,
                estabelecimento.Latitude,
                estabelecimento.Longitude,
                GeolocalizacaoHelper.CalcularDistanciaKm(
                    latitudeCliente,
                    longitudeCliente,
                    estabelecimento.Latitude,
                    estabelecimento.Longitude),
                destaqueIds.Contains(estabelecimento.Id)))
            .Where(consulta => consulta.DistanciaKm <= raioKm)
            .OrderByDescending(consulta => consulta.DestaqueMarketplace)
            .ThenBy(consulta => consulta.DistanciaKm)
            .ToList();

        var total = filtrados.Count;
        var itens = filtrados
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToList();

        return (itens, total);
    }

    private async Task<HashSet<int>> ObterEstabelecimentosDestaqueAsync(
        IReadOnlyList<int> estabelecimentoIds,
        CancellationToken cancellationToken)
    {
        if (estabelecimentoIds.Count == 0)
        {
            return [];
        }

        var assinaturas = await Context.Assinaturas
            .AsNoTracking()
            .Include(assinatura => assinatura.Plano)
            .Where(assinatura =>
                assinatura.EstabelecimentoId.HasValue
                && estabelecimentoIds.Contains(assinatura.EstabelecimentoId.Value)
                && (assinatura.Status == AssinaturaStatus.Ativa
                    || assinatura.Status == AssinaturaStatus.Trial
                    || assinatura.Status == AssinaturaStatus.Inadimplente))
            .ToListAsync(cancellationToken);

        return assinaturas
            .Where(assinatura => PlanoComercialCatalogo.Obter(assinatura.Plano).PrioridadeListagemPublica)
            .Select(assinatura => assinatura.EstabelecimentoId!.Value)
            .ToHashSet();
    }
}
