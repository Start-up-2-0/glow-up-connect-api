using System.Text.Json;
using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.DTOs.Pagamentos;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Pagamentos;
using GLOWAPI.Application.Options;
using Microsoft.Extensions.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Domain.Exceptions.Pagamentos;

namespace GLOWAPI.Application.Services;

public class CobrancaAssinaturaService : ICobrancaAssinaturaService
{
    private readonly IPagamentoRepository _pagamentoRepository;
    private readonly IAssinaturaRepository _assinaturaRepository;
    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly IGatewayPagamentoResolver _gatewayPagamentoResolver;
    private readonly IAssinaturaHistoricoService _assinaturaHistoricoService;
    private readonly IAssinaturaNotificacaoService _assinaturaNotificacaoService;
    private readonly IAssinaturaTitularContatoService _assinaturaTitularContatoService;
    private readonly ICicloCobrancaAssinaturaService _cicloCobrancaService;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAssinaturaOnboardingFinalizacaoService _assinaturaOnboardingFinalizacaoService;
    private readonly IAssinaturaVisibilidadeService _assinaturaVisibilidadeService;
    private readonly IOnboardingPublicacaoService _onboardingPublicacaoService;
    private readonly IAssinaturaEncerramentoService _assinaturaEncerramentoService;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly MercadoPagoOptions _mercadoPagoOptions;
    private readonly AssinaturaCobrancaOptions _assinaturaCobrancaOptions;

    public CobrancaAssinaturaService(
        IPagamentoRepository pagamentoRepository,
        IAssinaturaRepository assinaturaRepository,
        IEstabelecimentoUsuarioRepository estabelecimentoUsuarioRepository,
        IGatewayPagamentoResolver gatewayPagamentoResolver,
        IAssinaturaHistoricoService assinaturaHistoricoService,
        IAssinaturaNotificacaoService assinaturaNotificacaoService,
        IAssinaturaTitularContatoService assinaturaTitularContatoService,
        ICicloCobrancaAssinaturaService cicloCobrancaService,
        ICurrentUserContext currentUser,
        IAssinaturaOnboardingFinalizacaoService assinaturaOnboardingFinalizacaoService,
        IAssinaturaVisibilidadeService assinaturaVisibilidadeService,
        IOnboardingPublicacaoService onboardingPublicacaoService,
        IAssinaturaEncerramentoService assinaturaEncerramentoService,
        IUsuarioRepository usuarioRepository,
        IOptions<MercadoPagoOptions> mercadoPagoOptions,
        IOptions<AssinaturaCobrancaOptions> assinaturaCobrancaOptions)
    {
        _pagamentoRepository = pagamentoRepository;
        _assinaturaRepository = assinaturaRepository;
        _estabelecimentoUsuarioRepository = estabelecimentoUsuarioRepository;
        _gatewayPagamentoResolver = gatewayPagamentoResolver;
        _assinaturaHistoricoService = assinaturaHistoricoService;
        _assinaturaNotificacaoService = assinaturaNotificacaoService;
        _assinaturaTitularContatoService = assinaturaTitularContatoService;
        _cicloCobrancaService = cicloCobrancaService;
        _currentUser = currentUser;
        _assinaturaOnboardingFinalizacaoService = assinaturaOnboardingFinalizacaoService;
        _assinaturaVisibilidadeService = assinaturaVisibilidadeService;
        _onboardingPublicacaoService = onboardingPublicacaoService;
        _assinaturaEncerramentoService = assinaturaEncerramentoService;
        _usuarioRepository = usuarioRepository;
        _mercadoPagoOptions = mercadoPagoOptions.Value;
        _assinaturaCobrancaOptions = assinaturaCobrancaOptions.Value;
    }

    public async Task<(Pagamento Pagamento, string? CheckoutUrl, string? QrCode)> GerarCobrancaInicialAsync(
        Assinatura assinatura,
        Plano plano,
        PagamentoTransparenteMercadoPagoDto? pagamentoTransparente,
        CancellationToken cancellationToken = default)
    {
        if (assinatura.Id > 0 && assinatura.ProximaDataVencimento.HasValue)
        {
            var cicloJaPago = await _pagamentoRepository.ExistePagoPorAssinaturaCicloAsync(
                assinatura.Id,
                1,
                assinatura.ProximaDataVencimento.Value,
                cancellationToken: cancellationToken);

            if (cicloJaPago)
            {
                throw new PagamentoAssinaturaInvalidoException("A cobranca inicial deste ciclo ja foi paga.");
            }
        }

        var ciclo = assinatura.ProximaDataVencimento.HasValue
            ? new CicloCobrancaDatasDto(
                assinatura.ProximaDataVencimento.Value,
                assinatura.ProximaDataGeracaoCobranca ?? DateTime.UtcNow.Date,
                assinatura.ProximaDataAlerta ?? DateTime.UtcNow.Date)
            : _cicloCobrancaService.CalcularPrimeiroCiclo(
                assinatura.DataReferenciaCiclo,
                DateTime.UtcNow,
                plano.Periodo);

        var resultado = await CriarCobrancaGatewayAsync(
            assinatura,
            plano,
            ResolverValorMensalidade(assinatura, plano),
            TipoCobrancaAssinatura.Inicial,
            1,
            ciclo,
            $"assinatura-{Guid.NewGuid():N}",
            $"Assinatura {plano.Nome}",
            pagamentoTransparente,
            cancellationToken,
            comExpiracaoCheckout: true);

        await NotificarCobrancaPendenteComLinkAsync(
            assinatura,
            resultado.Pagamento,
            resultado.CheckoutUrl,
            cancellationToken);

        return resultado;
    }

    public async Task<(Pagamento Pagamento, string? CheckoutUrl, string? QrCode, bool Novo)> ObterOuRenovarCheckoutInicialAsync(
        Assinatura assinatura,
        Plano plano,
        PagamentoTransparenteMercadoPagoDto? pagamentoTransparente,
        CancellationToken cancellationToken = default)
    {
        if (assinatura.Id > 0)
        {
            var pendente = await _pagamentoRepository.ObterUltimoPendenteInicialPorAssinaturaAsync(
                assinatura.Id,
                cancellationToken);

            if (pendente is not null && CheckoutAindaValido(pendente))
            {
                return (pendente, ReconstruirCheckoutUrl(pendente), null, false);
            }

            if (pendente is not null)
            {
                pendente.Status = PagamentoStatus.Expirado;
                pendente.UpdatedAt = DateTime.UtcNow;
                _pagamentoRepository.Atualizar(pendente);
                await _pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);
            }
        }

        var gerado = await GerarCobrancaInicialAsync(assinatura, plano, pagamentoTransparente, cancellationToken);
        return (gerado.Pagamento, gerado.CheckoutUrl, gerado.QrCode, true);
    }

    public async Task<Pagamento> GerarCobrancaRecorrenteAsync(
        Assinatura assinatura,
        CancellationToken cancellationToken = default)
    {
        if (assinatura.Plano is null)
        {
            throw new AssinaturaNaoEncontradaException();
        }

        if (!assinatura.ProximaDataVencimento.HasValue)
        {
            throw new PagamentoAssinaturaInvalidoException("Assinatura sem proxima data de vencimento configurada.");
        }

        var periodo = assinatura.Plano.Periodo;
        var vencimento = assinatura.ProximaDataVencimento.Value.Date;
        var cobrancasExistentes = await _pagamentoRepository.ListarPorAssinaturaAsync(assinatura.Id, cancellationToken);

        while (true)
        {
            var cobrancasDoVencimento = cobrancasExistentes
                .Where(pagamento =>
                    pagamento.DataVencimento.HasValue
                    && pagamento.DataVencimento.Value.Date == vencimento)
                .ToList();

            var pendente = cobrancasDoVencimento.FirstOrDefault(pagamento =>
                pagamento.Status is PagamentoStatus.Pendente or PagamentoStatus.Atrasado);
            if (pendente is not null)
            {
                return pendente;
            }

            var pago = cobrancasDoVencimento.FirstOrDefault(pagamento =>
                pagamento.Status == PagamentoStatus.Pago);
            if (pago is null)
            {
                break;
            }

            var proximoCiclo = _cicloCobrancaService.CalcularProximoCiclo(vencimento, periodo);
            _cicloCobrancaService.AplicarCicloNaAssinatura(assinatura, proximoCiclo);
            assinatura.UpdatedAt = DateTime.UtcNow;
            _assinaturaRepository.Atualizar(assinatura);
            await _assinaturaRepository.SalvarAlteracoesAsync(cancellationToken);
            vencimento = proximoCiclo.Vencimento.Date;
        }

        var numeroCiclo = cobrancasExistentes.Count == 0
            ? 1
            : cobrancasExistentes.Max(pagamento => pagamento.NumeroCiclo) + 1;

        var ciclo = new CicloCobrancaDatasDto(
            vencimento,
            assinatura.ProximaDataGeracaoCobranca ?? DateTime.UtcNow.Date,
            assinatura.ProximaDataAlerta ?? vencimento.AddDays(-3));

        string? checkoutUrl = null;
        Pagamento pagamento;
        var usarLinkCheckoutPorCobranca = _mercadoPagoOptions.UsarCheckoutPro
            || string.IsNullOrWhiteSpace(assinatura.GatewaySubscriptionId);

        if (usarLinkCheckoutPorCobranca)
        {
            var resultado = await CriarCobrancaGatewayAsync(
                assinatura,
                assinatura.Plano,
                ResolverValorMensalidade(assinatura, assinatura.Plano),
                TipoCobrancaAssinatura.Recorrente,
                numeroCiclo,
                ciclo,
                $"recorrente-{assinatura.Id}-{numeroCiclo}-{Guid.NewGuid():N}",
                $"Assinatura {assinatura.Plano.Nome} - ciclo {numeroCiclo}",
                null,
                cancellationToken);
            pagamento = resultado.Pagamento;
            checkoutUrl = resultado.CheckoutUrl;
        }
        else
        {
            pagamento = CriarPagamentoInterno(
                assinatura,
                ResolverValorMensalidade(assinatura, assinatura.Plano),
                TipoCobrancaAssinatura.Recorrente,
                numeroCiclo,
                ciclo,
                $"sub-{assinatura.GatewaySubscriptionId}-ciclo-{numeroCiclo}",
                "subscription");
        }

        await _pagamentoRepository.AdicionarAsync(pagamento, cancellationToken);
        await _assinaturaHistoricoService.RegistrarPagamentoAsync(
            pagamento,
            "CobrancaRecorrenteGerada",
            null,
            pagamento.Status,
            "Cobranca recorrente gerada pelo worker.",
            cancellationToken: cancellationToken);
        await _assinaturaHistoricoService.RegistrarRecorrenciaAsync(
            assinatura,
            "CobrancaCicloGerada",
            pagamento.Status.ToString(),
            pagamento,
            ciclo.Vencimento.AddMonths(-1),
            ciclo.Vencimento,
            $"Cobranca do ciclo {numeroCiclo} gerada.",
            cancellationToken: cancellationToken);
        await _pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);

        await NotificarCobrancaPendenteComLinkAsync(
            assinatura,
            pagamento,
            checkoutUrl,
            cancellationToken);

        return pagamento;
    }

    public async Task<(Pagamento Pagamento, string? CheckoutUrl, string? QrCode)> GerarCobrancaTrocaPlanoAsync(
        Assinatura assinatura,
        Plano novoPlano,
        GatewayPagamento gateway,
        PagamentoTransparenteMercadoPagoDto? pagamentoTransparente,
        CancellationToken cancellationToken = default)
    {
        var ciclo = assinatura.ProximaDataVencimento.HasValue
            ? new CicloCobrancaDatasDto(
                assinatura.ProximaDataVencimento.Value,
                assinatura.ProximaDataGeracaoCobranca ?? DateTime.UtcNow.Date,
                assinatura.ProximaDataAlerta ?? DateTime.UtcNow.Date)
            : _cicloCobrancaService.CalcularPrimeiroCiclo(
                assinatura.DataReferenciaCiclo,
                DateTime.UtcNow,
                novoPlano.Periodo);

        var resultado = await CriarCobrancaGatewayAsync(
            assinatura,
            novoPlano,
            ResolverValorMensalidade(assinatura, novoPlano),
            TipoCobrancaAssinatura.TrocaPlano,
            1,
            ciclo,
            $"troca-plano-{assinatura.Id}-{Guid.NewGuid():N}",
            $"Troca de plano para {novoPlano.Nome}",
            pagamentoTransparente,
            cancellationToken,
            gateway,
            comExpiracaoCheckout: true);

        await NotificarCobrancaPendenteComLinkAsync(
            assinatura,
            resultado.Pagamento,
            resultado.CheckoutUrl,
            cancellationToken);

        return resultado;
    }

    public async Task ProcessarPagamentoAprovadoAsync(
        Pagamento pagamento,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        if (pagamento.Status == PagamentoStatus.Pago)
        {
            return;
        }

        if (pagamento.AssinaturaId is int assinaturaId
            && pagamento.DataVencimento is DateTime dataVencimento
            && await _pagamentoRepository.ExistePagoPorAssinaturaCicloAsync(
                assinaturaId,
                pagamento.NumeroCiclo,
                dataVencimento,
                pagamento.Id,
                cancellationToken))
        {
            var statusDuplicadoAnterior = pagamento.Status;
            pagamento.Status = PagamentoStatus.Cancelado;
            pagamento.UpdatedAt = DateTime.UtcNow;
            _pagamentoRepository.Atualizar(pagamento);

            await _assinaturaHistoricoService.RegistrarPagamentoAsync(
                pagamento,
                "PagamentoDuplicadoIgnorado",
                statusDuplicadoAnterior,
                pagamento.Status,
                "Pagamento duplicado do mesmo ciclo ignorado.",
                payloadJson,
                cancellationToken);
            await _pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);
            return;
        }

        var statusPagamentoAnterior = pagamento.Status;
        pagamento.Status = PagamentoStatus.Pago;
        pagamento.PagoEm = DateTime.UtcNow;
        pagamento.UpdatedAt = DateTime.UtcNow;
        _pagamentoRepository.Atualizar(pagamento);

        await _assinaturaHistoricoService.RegistrarPagamentoAsync(
            pagamento,
            "PagamentoAprovado",
            statusPagamentoAnterior,
            pagamento.Status,
            "Pagamento aprovado pelo gateway.",
            payloadJson,
            cancellationToken);

        if (pagamento.Assinatura is null)
        {
            await _pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);
            return;
        }

        var assinatura = pagamento.Assinatura;
        await _assinaturaOnboardingFinalizacaoService.FinalizarSePendenteAsync(assinatura, cancellationToken);
        var statusAssinaturaAnterior = assinatura.Status;

        if (assinatura.PlanoAlteracaoPendenteId.HasValue)
        {
            assinatura.PlanoId = assinatura.PlanoAlteracaoPendenteId.Value;
            assinatura.Plano = assinatura.PlanoAlteracaoPendente;
            assinatura.PlanoAlteracaoPendenteId = null;
            assinatura.PlanoAlteracaoPendente = null;
        }

        if (assinatura.Status is AssinaturaStatus.PendentePagamento or AssinaturaStatus.Trial or AssinaturaStatus.Inadimplente)
        {
            assinatura.Status = AssinaturaStatus.Ativa;
            if (statusAssinaturaAnterior != AssinaturaStatus.Inadimplente)
            {
                assinatura.Inicio = pagamento.PagoEm.Value;
            }
        }

        var fimAnterior = assinatura.Fim;
        assinatura.Fim = CalcularFimEncadeado(fimAnterior, pagamento.PagoEm.Value, assinatura.Plano?.Periodo);
        assinatura.UltimoPagamentoId = pagamento.Id;
        assinatura.UpdatedAt = DateTime.UtcNow;

        AvancarCicloSePagamentoQuitouVencimentoAtual(assinatura, pagamento);

        if (statusAssinaturaAnterior == AssinaturaStatus.Inadimplente)
        {
            await _onboardingPublicacaoService.RecalcularVisibilidadePorAssinaturaAsync(
                assinatura,
                cancellationToken);
        }
        else if (statusAssinaturaAnterior is AssinaturaStatus.PendentePagamento or AssinaturaStatus.Trial)
        {
            await _onboardingPublicacaoService.RecalcularVisibilidadePorAssinaturaAsync(
                assinatura,
                cancellationToken);
        }

        _assinaturaRepository.Atualizar(assinatura);
        await _assinaturaHistoricoService.RegistrarAssinaturaAsync(
            assinatura,
            statusAssinaturaAnterior == AssinaturaStatus.Trial
                ? "AssinaturaAtivadaPosTrial"
                : "AssinaturaAtivadaPorPagamento",
            statusAssinaturaAnterior,
            assinatura.Status,
            pagamento,
            "Assinatura atualizada apos pagamento aprovado.",
            payloadJson,
            cancellationToken);
        await _assinaturaHistoricoService.RegistrarRecorrenciaAsync(
            assinatura,
            "RecorrenciaLiberada",
            "Ativa",
            pagamento,
            pagamento.CicloInicio ?? assinatura.Inicio,
            pagamento.CicloFim ?? assinatura.Fim,
            "Ciclo liberado apos pagamento aprovado.",
            payloadJson,
            cancellationToken);

        await _assinaturaNotificacaoService.PagamentoConfirmadoAsync(
            assinatura,
            pagamento,
            ExtrairEmail(payloadJson) ?? _currentUser.Email,
            cancellationToken);

        await _pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task ProcessarPagamentoRecusadoAsync(
        Pagamento pagamento,
        string payloadJson,
        PagamentoStatus? novoStatus = null,
        CancellationToken cancellationToken = default)
    {
        var statusDestino = novoStatus ?? PagamentoStatus.Recusado;
        if (pagamento.Status == statusDestino || pagamento.Status == PagamentoStatus.Pago)
        {
            return;
        }

        var statusAnterior = pagamento.Status;
        pagamento.Status = statusDestino;
        pagamento.UpdatedAt = DateTime.UtcNow;
        _pagamentoRepository.Atualizar(pagamento);

        await _assinaturaHistoricoService.RegistrarPagamentoAsync(
            pagamento,
            "PagamentoNaoAprovado",
            statusAnterior,
            pagamento.Status,
            "Pagamento nao aprovado pelo gateway.",
            payloadJson,
            cancellationToken);

        if (pagamento.Assinatura is not null)
        {
            await _assinaturaHistoricoService.RegistrarRecorrenciaAsync(
                pagamento.Assinatura,
                "RecorrenciaPagamentoNaoAprovado",
                pagamento.Status.ToString(),
                pagamento,
                pagamento.Assinatura.Inicio,
                pagamento.Assinatura.Fim,
                "Ciclo aguardando regularizacao de pagamento.",
                payloadJson,
                cancellationToken);
        }

        await _assinaturaNotificacaoService.PagamentoRecusadoAsync(
            pagamento,
            ExtrairEmail(payloadJson) ?? _currentUser.Email,
            cancellationToken);

        await _pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task<int> MarcarAtrasadasAsync(CancellationToken cancellationToken = default)
    {
        var pendentesVencidos = await _pagamentoRepository.ListarPendentesVencidosAsync(
            DateTime.UtcNow,
            cancellationToken);

        var marcados = 0;
        foreach (var pagamento in pendentesVencidos)
        {
            var statusAnterior = pagamento.Status;
            pagamento.Status = PagamentoStatus.Atrasado;
            pagamento.UpdatedAt = DateTime.UtcNow;
            _pagamentoRepository.Atualizar(pagamento);

            await _assinaturaHistoricoService.RegistrarPagamentoAsync(
                pagamento,
                "CobrancaAtrasada",
                statusAnterior,
                pagamento.Status,
                "Cobranca marcada como atrasada.",
                cancellationToken: cancellationToken);

            if (pagamento.Assinatura is not null)
            {
                var assinatura = pagamento.Assinatura;
                if (assinatura.Status is AssinaturaStatus.Ativa or AssinaturaStatus.Trial)
                {
                    var statusAssinaturaAnterior = assinatura.Status;
                    assinatura.Status = AssinaturaStatus.Inadimplente;
                    assinatura.UpdatedAt = DateTime.UtcNow;
                    _assinaturaRepository.Atualizar(assinatura);
                    await _assinaturaHistoricoService.RegistrarAssinaturaAsync(
                        assinatura,
                        "AssinaturaInadimplente",
                        statusAssinaturaAnterior,
                        assinatura.Status,
                        pagamento,
                        "Assinatura em tolerancia de inadimplencia.",
                        cancellationToken: cancellationToken);
                }

                await _assinaturaHistoricoService.RegistrarRecorrenciaAsync(
                    pagamento.Assinatura,
                    "CobrancaAtrasada",
                    pagamento.Status.ToString(),
                    pagamento,
                    pagamento.Assinatura.Inicio,
                    pagamento.Assinatura.Fim,
                    "Cobranca em atraso.",
                    cancellationToken: cancellationToken);
            }

            marcados++;
        }

        if (marcados > 0)
        {
            await _pagamentoRepository.SalvarAlteracoesAsync(cancellationToken);
        }

        return marcados;
    }

    public async Task<int> EncerrarInadimplentesAsync(CancellationToken cancellationToken = default)
    {
        var dataLimiteVencimento = DateTime.UtcNow.Date
            .AddDays(-_assinaturaCobrancaOptions.DiasToleranciaInadimplencia);
        var pagamentos = await _pagamentoRepository.ListarAtrasadosAlemToleranciaAsync(
            dataLimiteVencimento,
            cancellationToken);

        var assinaturasProcessadas = new HashSet<int>();
        var encerradas = 0;

        foreach (var pagamento in pagamentos)
        {
            if (pagamento.Assinatura is null || pagamento.AssinaturaId is null)
            {
                continue;
            }

            if (!assinaturasProcessadas.Add(pagamento.AssinaturaId.Value))
            {
                continue;
            }

            var assinatura = pagamento.Assinatura;
            if (assinatura.Status is not (AssinaturaStatus.Ativa or AssinaturaStatus.Inadimplente or AssinaturaStatus.Trial or AssinaturaStatus.CancelamentoAgendado))
            {
                continue;
            }

            await _assinaturaEncerramentoService.EncerrarAsync(
                assinatura,
                AssinaturaStatus.Expirada,
                "AssinaturaEncerradaInadimplencia",
                "Assinatura encerrada automaticamente apos tolerancia de inadimplencia.",
                pagamento,
                cancellationToken);

            encerradas++;
        }

        if (encerradas > 0)
        {
            await _assinaturaRepository.SalvarAlteracoesAsync(cancellationToken);
            await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);
        }

        return encerradas;
    }

    public async Task<IReadOnlyList<CobrancaAssinaturaResponseDto>> ListarPorAssinaturaAsync(
        int assinaturaId,
        int usuarioId,
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

        if (vinculo is null)
        {
            throw new UsuarioSemPermissaoAssinaturaException();
        }

        var cobrancas = await _pagamentoRepository.ListarPorAssinaturaAsync(assinaturaId, cancellationToken);
        return cobrancas.Select(CobrancaAssinaturaResponseDto.From).ToList();
    }

    private async Task<(Pagamento Pagamento, string? CheckoutUrl, string? QrCode)> CriarCobrancaGatewayAsync(
        Assinatura assinatura,
        Plano plano,
        decimal valor,
        TipoCobrancaAssinatura tipoCobranca,
        int numeroCiclo,
        CicloCobrancaDatasDto ciclo,
        string referenciaInterna,
        string descricao,
        PagamentoTransparenteMercadoPagoDto? pagamentoTransparente,
        CancellationToken cancellationToken,
        GatewayPagamento? gatewayOverride = null,
        bool comExpiracaoCheckout = false)
    {
        var gatewayPagamento = gatewayOverride ?? assinatura.Gateway;
        var gateway = _gatewayPagamentoResolver.Resolver(gatewayPagamento);
        var titular = await _assinaturaTitularContatoService.ResolverAsync(assinatura, cancellationToken);
        var pagadorNome = !string.IsNullOrWhiteSpace(titular.Nome)
            ? titular.Nome
            : !string.IsNullOrWhiteSpace(titular.NomeEstabelecimento)
                ? titular.NomeEstabelecimento
                : _currentUser.Email ?? "Usuario Glow";
        var pagadorEmail = !string.IsNullOrWhiteSpace(titular.Email)
            ? titular.Email
            : _currentUser.Email ?? string.Empty;

        DateTime? expiraEm = null;
        if (comExpiracaoCheckout)
        {
            expiraEm = CalcularExpiraEmCheckout();
        }

        var response = await gateway.CriarCobrancaAsync(new CriarCobrancaGatewayRequest(
            Gateway: gatewayPagamento,
            ReferenciaInterna: referenciaInterna,
            Descricao: descricao,
            Valor: valor,
            Moeda: "BRL",
            PagadorNome: pagadorNome,
            PagadorEmail: pagadorEmail,
            ExpiraEm: expiraEm,
            Metadados: new Dictionary<string, string>
            {
                ["assinaturaId"] = assinatura.Id > 0 ? assinatura.Id.ToString() : string.Empty,
                ["planoId"] = plano.Id.ToString(),
                ["tipoCobranca"] = tipoCobranca.ToString(),
                ["numeroCiclo"] = numeroCiclo.ToString()
            },
            PagamentoTransparente: CriarPagamentoTransparenteRequest(gatewayPagamento, pagamentoTransparente)),
            cancellationToken);

        if (!response.Sucesso)
        {
            throw new GatewayPagamentoException(
                response.MensagemErro ?? "Nao foi possivel criar a cobranca no gateway.",
                GatewayPagamentoErrorDetails.FromCobranca(response));
        }

        var pagamento = new Pagamento
        {
            Assinatura = assinatura,
            AssinaturaId = assinatura.Id > 0 ? assinatura.Id : null,
            Gateway = gatewayPagamento,
            GatewayPaymentId = response.GatewayPaymentId,
            ReferenciaInterna = referenciaInterna,
            MetodoPagamento = response.MetodoPagamento,
            Status = PagamentoStatus.Pendente,
            Valor = valor,
            Moeda = "BRL",
            TipoCobranca = tipoCobranca,
            NumeroCiclo = numeroCiclo,
            DataVencimento = ciclo.Vencimento,
            DataGeracao = ciclo.Geracao,
            CicloInicio = ciclo.Vencimento.AddMonths(-1),
            CicloFim = ciclo.Vencimento,
            ExpiraEm = expiraEm
        };

        return (pagamento, response.CheckoutUrl, response.QrCode);
    }

    private PagamentoTransparenteGatewayRequest? CriarPagamentoTransparenteRequest(
        GatewayPagamento gateway,
        PagamentoTransparenteMercadoPagoDto? pagamento)
    {
        if (gateway != GatewayPagamento.MercadoPago)
        {
            return null;
        }

        if (pagamento is null)
        {
            if (_mercadoPagoOptions.UsarCheckoutPro)
            {
                return null;
            }

            throw new PagamentoAssinaturaInvalidoException(
                "Dados do Checkout Transparente sao obrigatorios para pagamento via Mercado Pago.");
        }

        if (string.IsNullOrWhiteSpace(pagamento.PaymentMethodId))
        {
            throw new PagamentoAssinaturaInvalidoException("PaymentMethodId do pagamento e obrigatorio.");
        }

        return new PagamentoTransparenteGatewayRequest(
            pagamento.PaymentMethodId.Trim(),
            pagamento.Token,
            pagamento.IssuerId,
            pagamento.Installments,
            pagamento.IdentificationType,
            pagamento.IdentificationNumber);
    }

    private async Task NotificarCobrancaPendenteComLinkAsync(
        Assinatura assinatura,
        Pagamento pagamento,
        string? checkoutUrl,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(checkoutUrl))
        {
            return;
        }

        var titular = await _assinaturaTitularContatoService.ResolverAsync(assinatura, cancellationToken);
        await _assinaturaNotificacaoService.CobrancaPendenteComLinkAsync(
            assinatura,
            pagamento,
            checkoutUrl,
            titular,
            cancellationToken);
    }

    private static decimal ResolverValorMensalidade(Assinatura assinatura, Plano plano) =>
        AssinaturaValorCobranca.CalcularMensalidade(
            PlanoComercialCatalogo.ResolverPreco(plano, assinatura.TipoAssinatura),
            assinatura.PercentualDescontoPermanente);

    private static Pagamento CriarPagamentoInterno(
        Assinatura assinatura,
        decimal valor,
        TipoCobrancaAssinatura tipoCobranca,
        int numeroCiclo,
        CicloCobrancaDatasDto ciclo,
        string gatewayPaymentId,
        string metodoPagamento) =>
        new()
        {
            Assinatura = assinatura,
            AssinaturaId = assinatura.Id > 0 ? assinatura.Id : null,
            Gateway = assinatura.Gateway,
            GatewayPaymentId = gatewayPaymentId,
            MetodoPagamento = metodoPagamento,
            Status = PagamentoStatus.Pendente,
            Valor = valor,
            Moeda = "BRL",
            TipoCobranca = tipoCobranca,
            NumeroCiclo = numeroCiclo,
            DataVencimento = ciclo.Vencimento,
            DataGeracao = ciclo.Geracao,
            CicloInicio = ciclo.Vencimento.AddMonths(-1),
            CicloFim = ciclo.Vencimento
        };

    private static string? ExtrairEmail(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            foreach (var property in new[] { "email", "payerEmail", "pagadorEmail", "customerEmail" })
            {
                if (root.TryGetProperty(property, out var value)
                    && value.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(value.GetString()))
                {
                    return value.GetString();
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private void AvancarCicloSePagamentoQuitouVencimentoAtual(Assinatura assinatura, Pagamento pagamento)
    {
        if (!assinatura.ProximaDataVencimento.HasValue || !pagamento.DataVencimento.HasValue)
        {
            return;
        }

        if (pagamento.TipoCobranca == TipoCobrancaAssinatura.Inicial)
        {
            return;
        }

        if (pagamento.DataVencimento.Value.Date != assinatura.ProximaDataVencimento.Value.Date)
        {
            return;
        }

        var proximoCiclo = _cicloCobrancaService.CalcularProximoCiclo(
            assinatura.ProximaDataVencimento.Value,
            assinatura.Plano?.Periodo ?? PlanoPeriodo.Mensal);
        _cicloCobrancaService.AplicarCicloNaAssinatura(assinatura, proximoCiclo);
    }

    private static DateTime? CalcularFimEncadeado(DateTime? fimAnterior, DateTime pagoEm, PlanoPeriodo? periodo)
    {
        var baseCalculo = fimAnterior.HasValue && fimAnterior.Value > pagoEm
            ? fimAnterior.Value
            : pagoEm;

        return periodo switch
        {
            PlanoPeriodo.Mensal => baseCalculo.AddMonths(1),
            PlanoPeriodo.Trimestral => baseCalculo.AddMonths(3),
            PlanoPeriodo.Semestral => baseCalculo.AddMonths(6),
            PlanoPeriodo.Anual => baseCalculo.AddYears(1),
            _ => baseCalculo.AddMonths(1)
        };
    }

    private DateTime CalcularExpiraEmCheckout()
    {
        var minutos = _assinaturaCobrancaOptions.MinutosExpiracaoCheckout;
        if (minutos <= 0)
        {
            minutos = 5;
        }

        if (_mercadoPagoOptions.SandboxAtivo())
        {
            minutos = Math.Max(minutos, 30);
        }

        return BrasilDateTimeHelper.Agora().AddMinutes(minutos);
    }

    private static bool CheckoutAindaValido(Pagamento pagamento) =>
        pagamento.ExpiraEm.HasValue
        && pagamento.ExpiraEm.Value > BrasilDateTimeHelper.Agora();

    private string? ReconstruirCheckoutUrl(Pagamento pagamento)
    {
        if (string.IsNullOrWhiteSpace(pagamento.GatewayPaymentId))
        {
            return null;
        }

        if (!string.Equals(pagamento.MetodoPagamento, "checkout_pro", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var host = _mercadoPagoOptions.SandboxAtivo()
            ? "https://sandbox.mercadopago.com.br"
            : "https://www.mercadopago.com.br";
        return $"{host}/checkout/v1/redirect?pref_id={Uri.EscapeDataString(pagamento.GatewayPaymentId)}";
    }
}
