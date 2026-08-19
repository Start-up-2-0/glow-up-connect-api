using GLOWAPI.Application.DTOs.Equipe;
using GLOWAPI.Application.Validators;
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
    private readonly IAgendamentoItemRepository _agendamentoItemRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IAuditoriaNegocioService _auditoriaNegocioService;
    private readonly IOnboardingPublicacaoService _onboardingPublicacaoService;

    public ProfissionalServicoNegocioService(
        IServicoRepository servicoRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IProfissionalServicoRepository profissionalServicoRepository,
        IAgendamentoItemRepository agendamentoItemRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IAuditoriaNegocioService auditoriaNegocioService,
        IOnboardingPublicacaoService onboardingPublicacaoService)
    {
        _servicoRepository = servicoRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _profissionalServicoRepository = profissionalServicoRepository;
        _agendamentoItemRepository = agendamentoItemRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _auditoriaNegocioService = auditoriaNegocioService;
        _onboardingPublicacaoService = onboardingPublicacaoService;
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

        var servico = await ObterServicoAtivoDoEstabelecimentoAsync(servicoId, estabelecimentoId, cancellationToken);
        await GarantirProfissionalDoEstabelecimentoAsync(profissionalId, estabelecimentoId, cancellationToken);

        var preco = request.Preco ?? servico.PrecoBase;
        var duracaoMinutos = request.DuracaoMinutos ?? servico.DuracaoMinutos;
        ServicoValidador.ValidarPrecoDuracaoVinculo(preco, duracaoMinutos);

        var vinculoExistente = await _profissionalServicoRepository.ObterPorProfissionalEServicoAsync(
            profissionalId,
            servicoId,
            cancellationToken);

        if (vinculoExistente?.Ativo == true)
        {
            throw new ProfissionalServicoDuplicadoException();
        }

        ProfissionalServico vinculo;
        if (vinculoExistente is not null)
        {
            vinculoExistente.Preco = preco;
            vinculoExistente.DuracaoMinutos = duracaoMinutos;
            vinculoExistente.Ativo = true;
            vinculoExistente.UpdatedAt = DateTime.UtcNow;

            _profissionalServicoRepository.Atualizar(vinculoExistente);
            await _profissionalServicoRepository.SalvarAlteracoesAsync(cancellationToken);
            vinculo = vinculoExistente;
        }
        else
        {
            vinculo = new ProfissionalServico
            {
                ProfissionalId = profissionalId,
                ServicoId = servicoId,
                Preco = preco,
                DuracaoMinutos = duracaoMinutos,
                Ativo = true
            };

            await _profissionalServicoRepository.AdicionarAsync(vinculo, cancellationToken);
            await _profissionalServicoRepository.SalvarAlteracoesAsync(cancellationToken);
        }

        await _onboardingPublicacaoService.RecalcularVisibilidadeAsync(estabelecimentoId, cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ProfissionalServicoVinculado,
            nameof(ProfissionalServico),
            vinculo.Id,
            new
            {
                vinculo.ProfissionalId,
                vinculo.ServicoId,
                vinculo.Preco,
                vinculo.DuracaoMinutos
            },
            cancellationToken);

        return ProfissionalServicoResponseDto.From(vinculo);
    }

    public async Task<ProfissionalServicoResponseDto> AtualizarAsync(
        int estabelecimentoId,
        int profissionalId,
        int servicoId,
        AtualizarProfissionalServicoRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ServicoGerenciar,
            cancellationToken);

        await ObterServicoAtivoDoEstabelecimentoAsync(servicoId, estabelecimentoId, cancellationToken);
        ServicoValidador.ValidarPrecoDuracaoVinculo(request.Preco, request.DuracaoMinutos);

        var vinculo = await ObterVinculoAtivoAsync(profissionalId, servicoId, cancellationToken);
        var alteracaoAnterior = new
        {
            vinculo.Preco,
            vinculo.DuracaoMinutos
        };

        vinculo.Preco = request.Preco;
        vinculo.DuracaoMinutos = request.DuracaoMinutos;
        vinculo.UpdatedAt = DateTime.UtcNow;

        _profissionalServicoRepository.Atualizar(vinculo);
        await _profissionalServicoRepository.SalvarAlteracoesAsync(cancellationToken);
        await _onboardingPublicacaoService.RecalcularVisibilidadeAsync(estabelecimentoId, cancellationToken);
        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ProfissionalServicoAlterado,
            nameof(ProfissionalServico),
            vinculo.Id,
            new
            {
                vinculo.ProfissionalId,
                vinculo.ServicoId,
                anterior = alteracaoAnterior,
                atual = new
                {
                    vinculo.Preco,
                    vinculo.DuracaoMinutos
                }
            },
            cancellationToken);

        return ProfissionalServicoResponseDto.From(vinculo);
    }

    public async Task<ProfissionalServicoResponseDto> DesvincularAsync(
        int estabelecimentoId,
        int profissionalId,
        int servicoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ServicoGerenciar,
            cancellationToken);

        await ObterServicoAtivoDoEstabelecimentoAsync(servicoId, estabelecimentoId, cancellationToken);

        var vinculo = await ObterVinculoAtivoAsync(profissionalId, servicoId, cancellationToken);

        if (await _agendamentoItemRepository.ExisteFuturoConfirmadoAsync(
                profissionalId,
                servicoId,
                cancellationToken))
        {
            throw new ProfissionalServicoComAgendamentoFuturoException();
        }

        vinculo.Ativo = false;
        vinculo.UpdatedAt = DateTime.UtcNow;

        _profissionalServicoRepository.Atualizar(vinculo);
        await _profissionalServicoRepository.SalvarAlteracoesAsync(cancellationToken);
        await _onboardingPublicacaoService.RecalcularVisibilidadeAsync(estabelecimentoId, cancellationToken);
        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ProfissionalServicoDesvinculado,
            nameof(ProfissionalServico),
            vinculo.Id,
            new
            {
                vinculo.ProfissionalId,
                vinculo.ServicoId
            },
            cancellationToken);

        return ProfissionalServicoResponseDto.From(vinculo);
    }

    private async Task<Servico> ObterServicoAtivoDoEstabelecimentoAsync(
        int servicoId,
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var servico = await _servicoRepository.ObterPorIdAsync(servicoId, cancellationToken);
        if (servico is null || !servico.Ativo || servico.EstabelecimentoId != estabelecimentoId)
        {
            throw new ServicoNegocioNaoEncontradoException();
        }

        return servico;
    }

    private async Task GarantirProfissionalDoEstabelecimentoAsync(
        int profissionalId,
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var profissionalNegocio = await _profissionalEstabelecimentoRepository.ObterPorProfissionalAsync(
            profissionalId,
            estabelecimentoId,
            cancellationToken);

        if (profissionalNegocio?.Ativo != true)
        {
            throw new ProfissionalSemVinculoNegocioException();
        }
    }

    private async Task<ProfissionalServico> ObterVinculoAtivoAsync(
        int profissionalId,
        int servicoId,
        CancellationToken cancellationToken)
    {
        var vinculo = await _profissionalServicoRepository.ObterPorProfissionalEServicoAsync(
            profissionalId,
            servicoId,
            cancellationToken);

        if (vinculo?.Ativo != true)
        {
            throw new ServicoNegocioNaoEncontradoException();
        }

        return vinculo;
    }
}
