using GLOWAPI.Application.DTOs.Caixa;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class SessaoCaixaNegocioService : ISessaoCaixaNegocioService
{
    private readonly ICaixaRepository _caixaRepository;
    private readonly ISessaoCaixaRepository _sessaoCaixaRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IAuditoriaNegocioService _auditoriaNegocioService;
    private readonly ICurrentUserContext _currentUserContext;

    public SessaoCaixaNegocioService(
        ICaixaRepository caixaRepository,
        ISessaoCaixaRepository sessaoCaixaRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IAuditoriaNegocioService auditoriaNegocioService,
        ICurrentUserContext currentUserContext)
    {
        _caixaRepository = caixaRepository;
        _sessaoCaixaRepository = sessaoCaixaRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _auditoriaNegocioService = auditoriaNegocioService;
        _currentUserContext = currentUserContext;
    }

    public async Task<SessaoCaixaResponseDto> ObterSessaoAtualAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaVisualizar,
            cancellationToken);

        var caixa = await ObterCaixaAsync(estabelecimentoId, cancellationToken);
        var sessao = await _sessaoCaixaRepository.ObterSessaoAbertaPorCaixaAsync(
            caixa.Id,
            cancellationToken);

        if (sessao is null)
        {
            throw new SessaoCaixaInvalidaException("Nao ha sessao de caixa aberta.");
        }

        return Mapear(sessao);
    }

    public async Task<SessaoCaixaResponseDto> AbrirSessaoAsync(
        int estabelecimentoId,
        AbrirSessaoCaixaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        if (!_currentUserContext.UserId.HasValue)
        {
            throw new SessaoCaixaInvalidaException("Usuario nao autenticado.");
        }

        var caixa = await _caixaRepository.ObterPorEstabelecimentoComTrackingAsync(
            estabelecimentoId,
            cancellationToken);

        if (caixa is null)
        {
            throw new CaixaNegocioNaoEncontradoException();
        }

        var sessaoAberta = await _sessaoCaixaRepository.ObterSessaoAbertaPorCaixaAsync(
            caixa.Id,
            cancellationToken);

        if (sessaoAberta is not null)
        {
            throw new SessaoCaixaInvalidaException("Ja existe uma sessao de caixa aberta.");
        }

        var sessao = new SessaoCaixa
        {
            CaixaId = caixa.Id,
            UsuarioId = _currentUserContext.UserId.Value,
            AbertoEm = DateTime.UtcNow,
            SaldoInicial = request.SaldoInicial,
            Status = SessaoCaixaStatus.Aberta,
            CreateAd = DateTime.UtcNow
        };

        await _sessaoCaixaRepository.AdicionarAsync(sessao, cancellationToken);
        await _sessaoCaixaRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.SessaoCaixaAberta,
            nameof(SessaoCaixa),
            sessao.Id,
            new { sessao.Id, request.SaldoInicial },
            cancellationToken);

        return Mapear(sessao);
    }

    public async Task<SessaoCaixaResponseDto> FecharSessaoAsync(
        int estabelecimentoId,
        int sessaoId,
        FecharSessaoCaixaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.CaixaGerenciar,
            cancellationToken);

        var caixa = await ObterCaixaAsync(estabelecimentoId, cancellationToken);
        var sessao = await _sessaoCaixaRepository.ObterPorIdECaixaAsync(
            sessaoId,
            caixa.Id,
            cancellationToken);

        if (sessao is null)
        {
            throw new SessaoCaixaInvalidaException("Sessao de caixa nao encontrada.");
        }

        if (sessao.Status != SessaoCaixaStatus.Aberta)
        {
            throw new SessaoCaixaInvalidaException("A sessao de caixa ja foi fechada.");
        }

        sessao.FechadoEm = DateTime.UtcNow;
        sessao.SaldoInformadoFechamento = request.SaldoInformadoFechamento;
        sessao.Diferenca = request.SaldoInformadoFechamento - (caixa.SaldoDisponivel);
        sessao.Status = SessaoCaixaStatus.Fechada;
        sessao.UpdatedAt = DateTime.UtcNow;

        _sessaoCaixaRepository.Atualizar(sessao);
        await _sessaoCaixaRepository.SalvarAlteracoesAsync(cancellationToken);

        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.SessaoCaixaFechada,
            nameof(SessaoCaixa),
            sessao.Id,
            new
            {
                sessao.Id,
                request.SaldoInformadoFechamento,
                sessao.Diferenca
            },
            cancellationToken);

        return Mapear(sessao);
    }

    private static SessaoCaixaResponseDto Mapear(SessaoCaixa sessao) =>
        new(
            sessao.Id,
            sessao.CaixaId,
            sessao.UsuarioId,
            sessao.AbertoEm,
            sessao.FechadoEm,
            sessao.SaldoInicial,
            sessao.SaldoInformadoFechamento,
            sessao.Diferenca,
            sessao.Status.ToString());

    private async Task<Caixa> ObterCaixaAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken) =>
        await _caixaRepository.ObterOuProvisionarPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);
}
