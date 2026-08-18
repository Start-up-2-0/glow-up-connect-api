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

        // Sem bounding box no SQL (cidade + raio em memória). Logo é miniaturizada no service.
        var candidatos = await DbSet
            .AsNoTracking()
            .Where(estabelecimento =>
                estabelecimento.Ativo
                && estabelecimento.VisivelPublicamente
                && estabelecimento.Endereco != null
                && estabelecimento.Endereco.Latitude != null
                && estabelecimento.Endereco.Longitude != null
                && estabelecimento.Endereco.Estado.ToUpper() == estadoNormalizado
                && (categoriaId == null || estabelecimento.CategoriaEstabelecimentoId == categoriaId))
            .Select(estabelecimento => new
            {
                estabelecimento.Id,
                estabelecimento.PublicGuid,
                estabelecimento.Nome,
                estabelecimento.Logo,
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

        // Se o reverse-geocode da cidade não bater com o cadastro, não zera o mapa:
        // cai para distância no estado (mesmo raio).
        var baseFiltro = naCidade.Count > 0 ? naCidade : candidatos;

        var estabelecimentoIds = baseFiltro.Select(estabelecimento => estabelecimento.Id).ToList();
        var metadadosAssinatura = await ObterMetadadosAssinaturaAsync(estabelecimentoIds, cancellationToken);

        var filtrados = baseFiltro
            .Select(estabelecimento =>
            {
                var meta = metadadosAssinatura.TryGetValue(estabelecimento.Id, out var found)
                    ? found
                    : (false, TipoAssinatura.Estabelecimento);
                return new EstabelecimentoProximoConsulta(
                    estabelecimento.Id,
                    estabelecimento.PublicGuid,
                    estabelecimento.Nome,
                    estabelecimento.Logo ?? string.Empty,
                    estabelecimento.Descricao ?? string.Empty,
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
                    meta.Item1,
                    meta.Item2);
            })
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

    private async Task<Dictionary<int, (bool Destaque, TipoAssinatura TipoAssinatura)>> ObterMetadadosAssinaturaAsync(
        IReadOnlyList<int> estabelecimentoIds,
        CancellationToken cancellationToken)
    {
        if (estabelecimentoIds.Count == 0)
        {
            return [];
        }

        var resultado = new Dictionary<int, (bool Destaque, TipoAssinatura TipoAssinatura)>();

        var assinaturasDiretas = await Context.Assinaturas
            .AsNoTracking()
            .Include(assinatura => assinatura.Plano)
            .Where(assinatura =>
                assinatura.EstabelecimentoId.HasValue
                && estabelecimentoIds.Contains(assinatura.EstabelecimentoId.Value)
                && (assinatura.Status == AssinaturaStatus.Ativa
                    || assinatura.Status == AssinaturaStatus.Trial
                    || assinatura.Status == AssinaturaStatus.Inadimplente))
            .ToListAsync(cancellationToken);

        foreach (var assinatura in assinaturasDiretas)
        {
            var estabelecimentoId = assinatura.EstabelecimentoId!.Value;
            var destaque = PlanoComercialCatalogo.Obter(
                assinatura.Plano,
                assinatura.TipoAssinatura).PrioridadeListagemPublica;
            resultado[estabelecimentoId] = (destaque, assinatura.TipoAssinatura);
        }

        var faltantes = estabelecimentoIds.Where(id => !resultado.ContainsKey(id)).ToList();
        if (faltantes.Count == 0)
        {
            return resultado;
        }

        var vinculos = await Context.AssinaturaEstabelecimentos
            .AsNoTracking()
            .Include(vinculo => vinculo.Assinatura!)
                .ThenInclude(assinatura => assinatura.Plano)
            .Where(vinculo =>
                faltantes.Contains(vinculo.EstabelecimentoId)
                && vinculo.Assinatura != null
                && (vinculo.Assinatura.Status == AssinaturaStatus.Ativa
                    || vinculo.Assinatura.Status == AssinaturaStatus.Trial
                    || vinculo.Assinatura.Status == AssinaturaStatus.Inadimplente))
            .ToListAsync(cancellationToken);

        foreach (var vinculo in vinculos)
        {
            var assinatura = vinculo.Assinatura!;
            var destaque = PlanoComercialCatalogo.Obter(
                assinatura.Plano,
                assinatura.TipoAssinatura).PrioridadeListagemPublica;
            resultado[vinculo.EstabelecimentoId] = (destaque, assinatura.TipoAssinatura);
        }

        return resultado;
    }

    public async Task<TipoAssinatura> ObterTipoAssinaturaPublicoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        var mapa = await ObterMetadadosAssinaturaAsync([estabelecimentoId], cancellationToken);
        return mapa.TryGetValue(estabelecimentoId, out var meta)
            ? meta.TipoAssinatura
            : TipoAssinatura.Estabelecimento;
    }
}
