using GLOWAPI.Application.DTOs.Avaliacao;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class AvaliacaoResumoService : IAvaliacaoResumoService
{
    private readonly IAvaliacaoAtendimentoRepository _avaliacaoAtendimentoRepository;
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IProfissionalRepository _profissionalRepository;

    public AvaliacaoResumoService(
        IAvaliacaoAtendimentoRepository avaliacaoAtendimentoRepository,
        IEstabelecimentoRepository estabelecimentoRepository,
        IProfissionalRepository profissionalRepository)
    {
        _avaliacaoAtendimentoRepository = avaliacaoAtendimentoRepository;
        _estabelecimentoRepository = estabelecimentoRepository;
        _profissionalRepository = profissionalRepository;
    }

    public async Task<AvaliacaoResumoPublicoDto> ObterResumoEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        var avaliadoDesde = AvaliacaoNotaFormatter.ObterInicioJanela();
        var notas = await _avaliacaoAtendimentoRepository.ListarNotasEstabelecimentoAsync(
            estabelecimentoId,
            avaliadoDesde,
            cancellationToken);

        return MontarResumo(notas);
    }

    public async Task<AvaliacaoResumoPublicoDto> ObterResumoProfissionalAsync(
        int profissionalId,
        CancellationToken cancellationToken = default)
    {
        var avaliadoDesde = AvaliacaoNotaFormatter.ObterInicioJanela();
        var notas = await _avaliacaoAtendimentoRepository.ListarNotasProfissionalAsync(
            profissionalId,
            avaliadoDesde,
            cancellationToken);

        return MontarResumo(notas);
    }

    public async Task<AvaliacoesPaginadasResponseDto> ListarEstabelecimentoPublicoAsync(
        Guid publicGuid,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default)
    {
        var estabelecimento = await _estabelecimentoRepository.ObterPorPublicGuidAsync(publicGuid, cancellationToken)
            ?? throw new NegocioNaoEncontradoException();

        return await ListarEstabelecimentoAsync(
            estabelecimento.Id,
            pagina,
            tamanhoPagina,
            cancellationToken);
    }

    public async Task<AvaliacoesPaginadasResponseDto> ListarProfissionalPublicoAsync(
        Guid publicGuid,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default)
    {
        var profissional = await _profissionalRepository.ObterPorPublicGuidAsync(publicGuid, cancellationToken)
            ?? throw new ProfissionalNegocioNaoEncontradoException();

        var avaliadoDesde = AvaliacaoNotaFormatter.ObterInicioJanela();
        var resumo = await ObterResumoProfissionalAsync(profissional.Id, cancellationToken);
        var total = await _avaliacaoAtendimentoRepository.ContarPorProfissionalAsync(
            profissional.Id,
            avaliadoDesde,
            cancellationToken);

        var itens = await _avaliacaoAtendimentoRepository.ListarPorProfissionalAsync(
            profissional.Id,
            avaliadoDesde,
            pagina,
            tamanhoPagina,
            cancellationToken);

        return new AvaliacoesPaginadasResponseDto(
            resumo,
            total,
            pagina,
            tamanhoPagina,
            itens.Select(MapearComentarioProfissional).ToList());
    }

    public async Task<AvaliacoesNegocioPaginadasResponseDto> ListarNegocioAsync(
        int estabelecimentoId,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default)
    {
        var avaliadoDesde = AvaliacaoNotaFormatter.ObterInicioJanela();
        var resumo = await ObterResumoEstabelecimentoAsync(estabelecimentoId, cancellationToken);
        var total = await _avaliacaoAtendimentoRepository.ContarPorEstabelecimentoAsync(
            estabelecimentoId,
            avaliadoDesde,
            cancellationToken);

        var itens = await _avaliacaoAtendimentoRepository.ListarPorEstabelecimentoAsync(
            estabelecimentoId,
            avaliadoDesde,
            pagina,
            tamanhoPagina,
            cancellationToken);

        return new AvaliacoesNegocioPaginadasResponseDto(
            resumo,
            total,
            pagina,
            tamanhoPagina,
            itens.Select(MapearNegocio).ToList());
    }

    public async Task RecalcularCacheEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        var estabelecimento = await _estabelecimentoRepository.ObterPorIdAsync(estabelecimentoId, cancellationToken);
        if (estabelecimento is null)
        {
            return;
        }

        var resumo = await ObterResumoEstabelecimentoAsync(estabelecimentoId, cancellationToken);
        estabelecimento.NotaMedia = resumo.TotalAvaliacoes > 0 ? resumo.NotaMedia : null;
        estabelecimento.TotalAvaliacoes = resumo.TotalAvaliacoes;
        estabelecimento.UpdatedAt = DateTime.UtcNow;

        _estabelecimentoRepository.Atualizar(estabelecimento);
        await _estabelecimentoRepository.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task RecalcularCacheProfissionalAsync(
        int profissionalId,
        CancellationToken cancellationToken = default)
    {
        var profissional = await _profissionalRepository.ObterPorIdAsync(profissionalId, cancellationToken);
        if (profissional is null)
        {
            return;
        }

        var resumo = await ObterResumoProfissionalAsync(profissionalId, cancellationToken);
        profissional.NotaMedia = resumo.TotalAvaliacoes > 0 ? resumo.NotaMedia : null;
        profissional.TotalAvaliacoes = resumo.TotalAvaliacoes;
        profissional.UpdatedAt = DateTime.UtcNow;

        _profissionalRepository.Atualizar(profissional);
        await _profissionalRepository.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task RecalcularCachesDiarioAsync(CancellationToken cancellationToken = default)
    {
        var estabelecimentos = await _avaliacaoAtendimentoRepository.ListarEstabelecimentosComAvaliacoesAsync(cancellationToken);
        foreach (var estabelecimentoId in estabelecimentos)
        {
            await RecalcularCacheEstabelecimentoAsync(estabelecimentoId, cancellationToken);
        }

        var profissionais = await _avaliacaoAtendimentoRepository.ListarProfissionaisComAvaliacoesAsync(cancellationToken);
        foreach (var profissionalId in profissionais)
        {
            await RecalcularCacheProfissionalAsync(profissionalId, cancellationToken);
        }
    }

    private async Task<AvaliacoesPaginadasResponseDto> ListarEstabelecimentoAsync(
        int estabelecimentoId,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken)
    {
        var avaliadoDesde = AvaliacaoNotaFormatter.ObterInicioJanela();
        var resumo = await ObterResumoEstabelecimentoAsync(estabelecimentoId, cancellationToken);
        var total = await _avaliacaoAtendimentoRepository.ContarPorEstabelecimentoAsync(
            estabelecimentoId,
            avaliadoDesde,
            cancellationToken);

        var itens = await _avaliacaoAtendimentoRepository.ListarPorEstabelecimentoAsync(
            estabelecimentoId,
            avaliadoDesde,
            pagina,
            tamanhoPagina,
            cancellationToken);

        return new AvaliacoesPaginadasResponseDto(
            resumo,
            total,
            pagina,
            tamanhoPagina,
            itens.Select(MapearComentarioEstabelecimento).ToList());
    }

    private static AvaliacaoResumoPublicoDto MontarResumo(IReadOnlyList<byte> notas)
    {
        var mediaBruta = AvaliacaoNotaFormatter.CalcularMediaBruta(notas);
        var distribuicao = AvaliacaoNotaFormatter.CalcularDistribuicao(notas);

        return new AvaliacaoResumoPublicoDto(
            AvaliacaoNotaFormatter.FormatarMediaIfood(mediaBruta),
            notas.Count,
            AvaliacaoNotaFormatter.JanelaDias,
            Enumerable.Range(0, 6)
                .Select(nota => new AvaliacaoDistribuicaoItemDto(nota, distribuicao[nota]))
                .ToList());
    }

    private static AvaliacaoComentarioItemDto MapearComentarioEstabelecimento(AvaliacaoAtendimento avaliacao) =>
        new(
            avaliacao.NotaEstabelecimento,
            avaliacao.ComentarioEstabelecimento,
            avaliacao.AvaliadoEm,
            ObterNomeCliente(avaliacao));

    private static AvaliacaoComentarioItemDto MapearComentarioProfissional(AvaliacaoAtendimento avaliacao) =>
        new(
            avaliacao.NotaProfissional,
            avaliacao.ComentarioProfissional,
            avaliacao.AvaliadoEm,
            ObterNomeCliente(avaliacao));

    private static AvaliacaoNegocioItemDto MapearNegocio(AvaliacaoAtendimento avaliacao) =>
        new(
            avaliacao.Id,
            avaliacao.AgendamentoId,
            avaliacao.NotaEstabelecimento,
            avaliacao.ComentarioEstabelecimento,
            avaliacao.NotaProfissional,
            avaliacao.ComentarioProfissional,
            avaliacao.AvaliadoEm,
            ObterNomeCliente(avaliacao),
            avaliacao.Profissional?.NomePublico ?? string.Empty);

    private static string ObterNomeCliente(AvaliacaoAtendimento avaliacao) =>
        avaliacao.UsuarioCliente?.Nome
        ?? avaliacao.Agendamento?.ClienteNome
        ?? "Cliente";
}
