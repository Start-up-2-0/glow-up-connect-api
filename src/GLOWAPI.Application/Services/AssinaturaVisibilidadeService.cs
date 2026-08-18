using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;

namespace GLOWAPI.Application.Services;

public class AssinaturaVisibilidadeService : IAssinaturaVisibilidadeService
{
    private readonly IAssinaturaEstabelecimentoRepository _assinaturaEstabelecimentoRepository;
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;

    public AssinaturaVisibilidadeService(
        IAssinaturaEstabelecimentoRepository assinaturaEstabelecimentoRepository,
        IEstabelecimentoRepository estabelecimentoRepository)
    {
        _assinaturaEstabelecimentoRepository = assinaturaEstabelecimentoRepository;
        _estabelecimentoRepository = estabelecimentoRepository;
    }

    public async Task OcultarLojasVinculadasAsync(Assinatura assinatura, CancellationToken cancellationToken = default)
    {
        await AtualizarVisibilidadeAsync(assinatura, visivel: false, cancellationToken);
    }

    public async Task ReexibirLojasVinculadasAsync(Assinatura assinatura, CancellationToken cancellationToken = default)
    {
        await AtualizarVisibilidadeAsync(assinatura, visivel: true, cancellationToken);
    }

    private async Task AtualizarVisibilidadeAsync(
        Assinatura assinatura,
        bool visivel,
        CancellationToken cancellationToken)
    {
        var estabelecimentoIds = await ObterEstabelecimentoIdsVinculadosAsync(assinatura, cancellationToken);
        if (estabelecimentoIds.Count == 0)
        {
            return;
        }

        var alterado = false;
        foreach (var estabelecimentoId in estabelecimentoIds)
        {
            var estabelecimento = await _estabelecimentoRepository.ObterPorIdAsync(estabelecimentoId, cancellationToken);
            if (estabelecimento is null || estabelecimento.VisivelPublicamente == visivel)
            {
                continue;
            }

            estabelecimento.VisivelPublicamente = visivel;
            estabelecimento.UpdatedAt = DateTime.UtcNow;
            _estabelecimentoRepository.Atualizar(estabelecimento);
            alterado = true;
        }

        if (alterado)
        {
            await _estabelecimentoRepository.SalvarAlteracoesAsync(cancellationToken);
        }
    }

    private async Task<IReadOnlyList<int>> ObterEstabelecimentoIdsVinculadosAsync(
        Assinatura assinatura,
        CancellationToken cancellationToken)
    {
        if (assinatura.Id <= 0)
        {
            return assinatura.EstabelecimentoId.HasValue
                ? [assinatura.EstabelecimentoId.Value]
                : Array.Empty<int>();
        }

        var vinculos = await _assinaturaEstabelecimentoRepository.ListarPorAssinaturaAsync(assinatura.Id, cancellationToken);
        if (vinculos.Count > 0)
        {
            return vinculos.Select(vinculo => vinculo.EstabelecimentoId).Distinct().ToList();
        }

        return assinatura.EstabelecimentoId.HasValue
            ? [assinatura.EstabelecimentoId.Value]
            : Array.Empty<int>();
    }
}
