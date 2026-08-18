using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Services;

public class AssinaturaEncerramentoService : IAssinaturaEncerramentoService
{
    private readonly IAssinaturaRepository _assinaturaRepository;
    private readonly IAssinaturaEstabelecimentoRepository _assinaturaEstabelecimentoRepository;
    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IAssinaturaVisibilidadeService _assinaturaVisibilidadeService;
    private readonly IAssinaturaHistoricoService _assinaturaHistoricoService;

    public AssinaturaEncerramentoService(
        IAssinaturaRepository assinaturaRepository,
        IAssinaturaEstabelecimentoRepository assinaturaEstabelecimentoRepository,
        IEstabelecimentoUsuarioRepository estabelecimentoUsuarioRepository,
        IUsuarioRepository usuarioRepository,
        IAssinaturaVisibilidadeService assinaturaVisibilidadeService,
        IAssinaturaHistoricoService assinaturaHistoricoService)
    {
        _assinaturaRepository = assinaturaRepository;
        _assinaturaEstabelecimentoRepository = assinaturaEstabelecimentoRepository;
        _estabelecimentoUsuarioRepository = estabelecimentoUsuarioRepository;
        _usuarioRepository = usuarioRepository;
        _assinaturaVisibilidadeService = assinaturaVisibilidadeService;
        _assinaturaHistoricoService = assinaturaHistoricoService;
    }

    public async Task EncerrarAsync(
        Assinatura assinatura,
        AssinaturaStatus statusFinal,
        string eventoHistorico,
        string observacao,
        Pagamento? pagamento = null,
        CancellationToken cancellationToken = default)
    {
        if (assinatura.Status is AssinaturaStatus.Cancelada or AssinaturaStatus.Expirada)
        {
            return;
        }

        var statusAnterior = assinatura.Status;
        assinatura.Status = statusFinal;
        assinatura.Fim ??= DateTime.UtcNow;
        assinatura.RenovacaoAutomatica = false;
        assinatura.PlanoAlteracaoPendenteId = null;
        assinatura.PlanoAlteracaoPendente = null;
        assinatura.UpdatedAt = DateTime.UtcNow;

        if (statusFinal == AssinaturaStatus.Cancelada && assinatura.CanceladoEm is null)
        {
            assinatura.CanceladoEm = DateTime.UtcNow;
        }

        _assinaturaRepository.Atualizar(assinatura);

        await _assinaturaVisibilidadeService.OcultarLojasVinculadasAsync(assinatura, cancellationToken);
        await RebaixarTitularesSeNecessarioAsync(assinatura, cancellationToken);

        await _assinaturaHistoricoService.RegistrarAssinaturaAsync(
            assinatura,
            eventoHistorico,
            statusAnterior,
            assinatura.Status,
            pagamento,
            observacao,
            cancellationToken: cancellationToken);
        await _assinaturaHistoricoService.RegistrarRecorrenciaAsync(
            assinatura,
            $"{eventoHistorico}Recorrencia",
            assinatura.Status.ToString(),
            pagamento,
            assinatura.Inicio,
            assinatura.Fim,
            observacao,
            cancellationToken: cancellationToken);
    }

    public async Task ProcessarCancelamentosAgendadosAsync(
        DateTime dataReferenciaUtc,
        CancellationToken cancellationToken = default)
    {
        var assinaturas = await _assinaturaRepository.ListarCancelamentosAgendadosParaEncerrarAsync(
            dataReferenciaUtc,
            cancellationToken);

        foreach (var assinatura in assinaturas)
        {
            await EncerrarAsync(
                assinatura,
                AssinaturaStatus.Cancelada,
                "AssinaturaEncerradaCancelamentoAgendado",
                "Assinatura encerrada automaticamente ao fim do periodo contratado.",
                cancellationToken: cancellationToken);
        }

        if (assinaturas.Count > 0)
        {
            await _assinaturaRepository.SalvarAlteracoesAsync(cancellationToken);
            await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);
        }
    }

    private async Task RebaixarTitularesSeNecessarioAsync(
        Assinatura assinatura,
        CancellationToken cancellationToken)
    {
        var titulares = await ObterTitularesUsuarioIdsAsync(assinatura, cancellationToken);

        foreach (var usuarioId in titulares)
        {
            if (await _assinaturaRepository.UsuarioPossuiAssinaturaComAcessoAsync(
                    usuarioId,
                    ignorarAssinaturaId: assinatura.Id,
                    cancellationToken))
            {
                continue;
            }

            var usuario = await _usuarioRepository.ObterPorIdAsync(usuarioId, cancellationToken);
            if (usuario is null || usuario.Role == UserRole.Cliente || usuario.Role == UserRole.Admin)
            {
                continue;
            }

            if (usuario.Role is UserRole.DonoEstabelecimento or UserRole.ProfissionalAutonomo)
            {
                usuario.Role = UserRole.Cliente;
                usuario.UpdatedAt = DateTime.UtcNow;
                _usuarioRepository.Atualizar(usuario);
            }
        }
    }

    private async Task<IReadOnlyList<int>> ObterTitularesUsuarioIdsAsync(
        Assinatura assinatura,
        CancellationToken cancellationToken)
    {
        var estabelecimentoIds = await ObterEstabelecimentoIdsVinculadosAsync(assinatura, cancellationToken);
        var titulares = new HashSet<int>();

        foreach (var estabelecimentoId in estabelecimentoIds)
        {
            var owner = await _estabelecimentoUsuarioRepository.ObterOwnerAtivoAsync(
                estabelecimentoId,
                cancellationToken);

            if (owner is not null)
            {
                titulares.Add(owner.UsuarioId);
            }
        }

        return titulares.ToList();
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

        var vinculos = await _assinaturaEstabelecimentoRepository.ListarPorAssinaturaAsync(
            assinatura.Id,
            cancellationToken);

        if (vinculos.Count > 0)
        {
            return vinculos.Select(vinculo => vinculo.EstabelecimentoId).Distinct().ToList();
        }

        return assinatura.EstabelecimentoId.HasValue
            ? [assinatura.EstabelecimentoId.Value]
            : Array.Empty<int>();
    }
}
