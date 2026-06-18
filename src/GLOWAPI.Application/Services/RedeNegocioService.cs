using GLOWAPI.Application.DTOs.Rede;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Domain.Exceptions.Auth;

namespace GLOWAPI.Application.Services;

public class RedeNegocioService : IRedeNegocioService
{
    private readonly IAssinaturaRepository _assinaturaRepository;
    private readonly IAssinaturaEstabelecimentoRepository _assinaturaEstabelecimentoRepository;
    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly IAgendamentoRepository _agendamentoRepository;

    public RedeNegocioService(
        IAssinaturaRepository assinaturaRepository,
        IAssinaturaEstabelecimentoRepository assinaturaEstabelecimentoRepository,
        IEstabelecimentoUsuarioRepository estabelecimentoUsuarioRepository,
        IAgendamentoRepository agendamentoRepository)
    {
        _assinaturaRepository = assinaturaRepository;
        _assinaturaEstabelecimentoRepository = assinaturaEstabelecimentoRepository;
        _estabelecimentoUsuarioRepository = estabelecimentoUsuarioRepository;
        _agendamentoRepository = agendamentoRepository;
    }

    public async Task<RedeResumoResponseDto> ObterResumoAsync(
        int assinaturaId,
        int usuarioId,
        DateTime? inicio,
        DateTime? fim,
        CancellationToken cancellationToken = default)
    {
        var assinatura = await _assinaturaRepository.ObterPorIdComPlanoAsync(assinaturaId, cancellationToken);
        if (assinatura is null)
        {
            throw new AssinaturaNaoEncontradaException();
        }

        if (!assinatura.EstabelecimentoId.HasValue)
        {
            throw new AssinaturaTitularInvalidoException();
        }

        var vinculo = await _estabelecimentoUsuarioRepository.ObterAtivoAsync(
            assinatura.EstabelecimentoId.Value,
            usuarioId,
            cancellationToken);
        if (vinculo is null || vinculo.RoleNoEstabelecimento != EstablishmentUserRole.Owner)
        {
            throw new UsuarioSemPermissaoAssinaturaException();
        }

        if (!PlanoComercialCatalogo.PermiteMultiLoja(assinatura.Plano))
        {
            throw new TrocaPlanoAssinaturaInvalidaException("Resumo da rede disponivel apenas no plano Premium.");
        }

        var vinculos = await _assinaturaEstabelecimentoRepository.ListarPorAssinaturaAsync(
            assinaturaId,
            cancellationToken);
        var periodoInicio = (inicio ?? DateTime.UtcNow.AddDays(-30)).Date;
        var periodoFim = (fim ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

        var unidades = new List<RedeUnidadeResumoDto>(vinculos.Count);
        var totalAgendamentos = 0;
        foreach (var vinculoEstabelecimento in vinculos)
        {
            if (vinculoEstabelecimento.Estabelecimento is null)
            {
                continue;
            }

            var agendamentos = await _agendamentoRepository.ContarPorEstabelecimentoNoPeriodoAsync(
                vinculoEstabelecimento.EstabelecimentoId,
                periodoInicio,
                periodoFim,
                cancellationToken);
            totalAgendamentos += agendamentos;

            unidades.Add(new RedeUnidadeResumoDto(
                vinculoEstabelecimento.EstabelecimentoId,
                vinculoEstabelecimento.Estabelecimento.Nome,
                vinculoEstabelecimento.EhMatriz,
                agendamentos));
        }

        return new RedeResumoResponseDto(
            assinaturaId,
            unidades.Count,
            assinatura.Plano?.LimiteEstabelecimentos,
            totalAgendamentos,
            unidades);
    }
}
