using GLOWAPI.Application.DTOs.Equipe;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class ProfissionalServicoNegocioService : IProfissionalServicoNegocioService
{
    private readonly IServicoRepository _servicoRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IProfissionalServicoRepository _profissionalServicoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;

    public ProfissionalServicoNegocioService(
        IServicoRepository servicoRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IProfissionalServicoRepository profissionalServicoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService)
    {
        _servicoRepository = servicoRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _profissionalServicoRepository = profissionalServicoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
    }

    public async Task<ProfissionalServicoResponseDto> VincularAsync(
        int estabelecimentoId,
        int profissionalId,
        int servicoId,
        VincularServicoProfissionalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ServicoGerenciar,
            cancellationToken);

        var servico = await _servicoRepository.ObterPorIdAsync(servicoId, cancellationToken);
        if (servico is null || !servico.Ativo || servico.EstabelecimentoId != estabelecimentoId)
        {
            throw new ServicoNegocioNaoEncontradoException();
        }

        var profissionalNegocio = await _profissionalEstabelecimentoRepository.ObterPorProfissionalAsync(
            profissionalId,
            estabelecimentoId,
            cancellationToken);

        if (profissionalNegocio?.Ativo != true)
        {
            throw new ProfissionalSemVinculoNegocioException();
        }

        var preco = request.Preco ?? servico.PrecoBase;
        var duracaoMinutos = request.DuracaoMinutos ?? servico.DuracaoMinutos;
        ValidarDados(preco, duracaoMinutos);

        var vinculoExistente = await _profissionalServicoRepository.ObterPorProfissionalEServicoAsync(
            profissionalId,
            servicoId,
            cancellationToken);

        if (vinculoExistente?.Ativo == true)
        {
            throw new ProfissionalServicoDuplicadoException();
        }

        if (vinculoExistente is not null)
        {
            vinculoExistente.Preco = preco;
            vinculoExistente.DuracaoMinutos = duracaoMinutos;
            vinculoExistente.Ativo = true;
            vinculoExistente.UpdatedAt = DateTime.UtcNow;

            _profissionalServicoRepository.Atualizar(vinculoExistente);
            await _profissionalServicoRepository.SalvarAlteracoesAsync(cancellationToken);

            return ProfissionalServicoResponseDto.From(vinculoExistente);
        }

        var vinculo = new ProfissionalServico
        {
            ProfissionalId = profissionalId,
            ServicoId = servicoId,
            Preco = preco,
            DuracaoMinutos = duracaoMinutos,
            Ativo = true
        };

        await _profissionalServicoRepository.AdicionarAsync(vinculo, cancellationToken);
        await _profissionalServicoRepository.SalvarAlteracoesAsync(cancellationToken);

        return ProfissionalServicoResponseDto.From(vinculo);
    }

    private static void ValidarDados(decimal preco, int duracaoMinutos)
    {
        if (preco < 0)
        {
            throw new ProfissionalServicoInvalidoException("O preco do servico para o profissional nao pode ser negativo.");
        }

        if (duracaoMinutos <= 0)
        {
            throw new ProfissionalServicoInvalidoException("A duracao do servico para o profissional deve ser maior que zero.");
        }
    }
}
