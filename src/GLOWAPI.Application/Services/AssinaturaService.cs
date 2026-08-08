using System.Text.Json;
using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.DTOs.Pagamentos;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Assinaturas;
using GLOWAPI.Application.Models.Pagamentos;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Domain.Exceptions.Auth;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class AssinaturaService : IAssinaturaService
{
    private readonly IAssinaturaRepository _assinaturaRepository;
    private readonly IAssinaturaEstabelecimentoRepository _assinaturaEstabelecimentoRepository;
    private readonly IPlanoRepository _planoRepository;
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IPagamentoRepository _pagamentoRepository;
    private readonly ICampanhaPromocionalRepository _campanhaPromocionalRepository;
    private readonly IGatewayPagamentoResolver _gatewayPagamentoResolver;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAssinaturaNotificacaoService _assinaturaNotificacaoService;
    private readonly IAssinaturaHistoricoService _assinaturaHistoricoService;
    private readonly IEnderecoGeocodificacaoService _enderecoGeocodificacaoService;
    private readonly IPromocaoLancamentoService _promocaoLancamentoService;
    private readonly ICicloCobrancaAssinaturaService _cicloCobrancaService;
    private readonly ICobrancaAssinaturaService _cobrancaAssinaturaService;
    private readonly IAvatarBase64Decoder _avatarBase64Decoder;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IAssinaturaVisibilidadeService _assinaturaVisibilidadeService;
    private readonly IAssinaturaEncerramentoService _assinaturaEncerramentoService;
    private readonly MercadoPagoOptions _mercadoPagoOptions;

    public AssinaturaService(
        IAssinaturaRepository assinaturaRepository,
        IAssinaturaEstabelecimentoRepository assinaturaEstabelecimentoRepository,
        IPlanoRepository planoRepository,
        IEstabelecimentoRepository estabelecimentoRepository,
        IEstabelecimentoUsuarioRepository estabelecimentoUsuarioRepository,
        IProfissionalRepository profissionalRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IPagamentoRepository pagamentoRepository,
        ICampanhaPromocionalRepository campanhaPromocionalRepository,
        IGatewayPagamentoResolver gatewayPagamentoResolver,
        ICurrentUserContext currentUser,
        IAssinaturaNotificacaoService assinaturaNotificacaoService,
        IAssinaturaHistoricoService assinaturaHistoricoService,
        IEnderecoGeocodificacaoService enderecoGeocodificacaoService,
        IPromocaoLancamentoService promocaoLancamentoService,
        ICicloCobrancaAssinaturaService cicloCobrancaService,
        ICobrancaAssinaturaService cobrancaAssinaturaService,
        IAvatarBase64Decoder avatarBase64Decoder,
        IUsuarioRepository usuarioRepository,
        IAssinaturaVisibilidadeService assinaturaVisibilidadeService,
        IAssinaturaEncerramentoService assinaturaEncerramentoService,
        IOptions<MercadoPagoOptions> mercadoPagoOptions)
    {
        _assinaturaRepository = assinaturaRepository;
        _assinaturaEstabelecimentoRepository = assinaturaEstabelecimentoRepository;
        _planoRepository = planoRepository;
        _estabelecimentoRepository = estabelecimentoRepository;
        _estabelecimentoUsuarioRepository = estabelecimentoUsuarioRepository;
        _profissionalRepository = profissionalRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _pagamentoRepository = pagamentoRepository;
        _campanhaPromocionalRepository = campanhaPromocionalRepository;
        _gatewayPagamentoResolver = gatewayPagamentoResolver;
        _currentUser = currentUser;
        _assinaturaNotificacaoService = assinaturaNotificacaoService;
        _assinaturaHistoricoService = assinaturaHistoricoService;
        _enderecoGeocodificacaoService = enderecoGeocodificacaoService;
        _promocaoLancamentoService = promocaoLancamentoService;
        _cicloCobrancaService = cicloCobrancaService;
        _cobrancaAssinaturaService = cobrancaAssinaturaService;
        _avatarBase64Decoder = avatarBase64Decoder;
        _usuarioRepository = usuarioRepository;
        _assinaturaVisibilidadeService = assinaturaVisibilidadeService;
        _assinaturaEncerramentoService = assinaturaEncerramentoService;
        _mercadoPagoOptions = mercadoPagoOptions.Value;
    }

    public async Task<AssinaturaResponseDto> IniciarAsync(
        IniciarAssinaturaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = ObterUserIdAutenticado();
        var plano = await _planoRepository.ObterPorIdAsync(request.PlanoId, cancellationToken);
        if (plano is null || !plano.Ativo)
        {
            throw new PlanoNaoEncontradoException();
        }

        ValidarTitular(request);

        var elegivelTrial = await ElegivelPromocaoTrialAsync(request, userId, cancellationToken);
        var onboardingPendente = DeveAdiarOnboarding(request, elegivelTrial);
        Assinatura assinatura;
        if (onboardingPendente)
        {
            assinatura = await CriarAssinaturaComOnboardingPendenteAsync(request, userId, cancellationToken);
        }
        else
        {
            assinatura = request.TipoAssinatura switch
            {
                TipoAssinatura.Estabelecimento => await CriarParaEstabelecimentoAsync(request, userId, cancellationToken),
                TipoAssinatura.ProfissionalAutonomo => await CriarParaProfissionalAutonomoAsync(request, userId, cancellationToken),
                _ => throw new AssinaturaTitularInvalidoException()
            };
        }

        InicializarReferenciaCiclo(assinatura);

        var diasTrialIniciado = await TentarIniciarComTrialAsync(assinatura, plano, request, cancellationToken);
        if (diasTrialIniciado.HasValue)
        {
            await _assinaturaRepository.AdicionarAsync(assinatura, cancellationToken);
            await _assinaturaRepository.SalvarAlteracoesAsync(cancellationToken);

            if (assinatura.Estabelecimento is not null && !assinatura.EstabelecimentoId.HasValue)
            {
                assinatura.EstabelecimentoId = assinatura.Estabelecimento.Id;
            }

            await GarantirVinculoMatrizAsync(assinatura, cancellationToken);

            var diasTrial = diasTrialIniciado.Value;

            await _assinaturaNotificacaoService.TrialIniciadoAsync(
                assinatura,
                plano,
                diasTrial,
                _currentUser.Email,
                cancellationToken);

            if (!onboardingPendente)
            {
                await PromoverRoleOnboardingAsync(userId, request.TipoAssinatura, cancellationToken);
            }

            return await MontarRespostaInicioAsync(assinatura, cancellationToken, diasTrial: diasTrial);
        }

        var ciclo = _cicloCobrancaService.CalcularPrimeiroCiclo(
            assinatura.DataReferenciaCiclo,
            DateTime.UtcNow,
            plano.Periodo);
        _cicloCobrancaService.AplicarCicloNaAssinatura(assinatura, ciclo);
        assinatura.Fim = ciclo.Vencimento;

        var pagamentoInicial = await _cobrancaAssinaturaService.GerarCobrancaInicialAsync(
            assinatura,
            plano,
            request.Pagamento,
            cancellationToken);

        await _assinaturaHistoricoService.RegistrarAssinaturaAsync(
            assinatura,
            "AssinaturaIniciada",
            null,
            assinatura.Status,
            pagamentoInicial.Pagamento,
            "Assinatura criada aguardando pagamento inicial.",
            cancellationToken: cancellationToken);
        await _assinaturaHistoricoService.RegistrarPagamentoAsync(
            pagamentoInicial.Pagamento,
            "PagamentoInicialCriado",
            null,
            pagamentoInicial.Pagamento.Status,
            "Cobranca inicial criada no gateway.",
            pagamentoInicial.Pagamento.GatewayPaymentId,
            cancellationToken);
        await _assinaturaHistoricoService.RegistrarRecorrenciaAsync(
            assinatura,
            "RecorrenciaAguardandoPagamento",
            "AguardandoPagamento",
            pagamentoInicial.Pagamento,
            assinatura.Inicio,
            assinatura.Fim,
            "Primeiro ciclo aguardando confirmacao de pagamento.",
            cancellationToken: cancellationToken);

        await _assinaturaRepository.AdicionarAsync(assinatura, cancellationToken);
        await _pagamentoRepository.AdicionarAsync(pagamentoInicial.Pagamento, cancellationToken);
        await _assinaturaRepository.SalvarAlteracoesAsync(cancellationToken);

        if (assinatura.Estabelecimento is not null && !assinatura.EstabelecimentoId.HasValue)
        {
            assinatura.EstabelecimentoId = assinatura.Estabelecimento.Id;
        }

        await GarantirVinculoMatrizAsync(assinatura, cancellationToken);

        if (!pagamentoInicial.Pagamento.AssinaturaId.HasValue)
        {
            pagamentoInicial.Pagamento.AssinaturaId = assinatura.Id;
        }

        await _assinaturaNotificacaoService.AssinaturaIniciadaAsync(
            assinatura,
            plano,
            _currentUser.Email,
            cancellationToken);

        if (!onboardingPendente)
        {
            await PromoverRoleOnboardingAsync(userId, request.TipoAssinatura, cancellationToken);
        }

        return await MontarRespostaInicioAsync(
            assinatura,
            cancellationToken,
            PagamentoAssinaturaResponseDto.From(
                pagamentoInicial.Pagamento,
                pagamentoInicial.CheckoutUrl,
                pagamentoInicial.QrCode));
    }

    public async Task<AssinaturaResponseDto> TrocarPlanoAsync(
        int assinaturaId,
        TrocarPlanoAssinaturaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = ObterUserIdAutenticado();
        var assinatura = await _assinaturaRepository.ObterPorIdComPlanoAsync(assinaturaId, cancellationToken);
        if (assinatura is null)
        {
            throw new AssinaturaNaoEncontradaException();
        }

        await ValidarPermissaoGerenciarAssinaturaAsync(assinatura, userId, cancellationToken);
        ValidarAssinaturaPermiteTroca(assinatura);

        var novoPlano = await _planoRepository.ObterPorIdAsync(request.NovoPlanoId, cancellationToken);
        if (novoPlano is null || !novoPlano.Ativo)
        {
            throw new PlanoNaoEncontradoException();
        }

        if (assinatura.PlanoId == novoPlano.Id)
        {
            throw new TrocaPlanoAssinaturaInvalidaException("Assinatura ja esta vinculada ao plano informado.");
        }

        await ValidarDowngradeMultiLojaAsync(assinatura, novoPlano, cancellationToken);

        if (TrocaExigeCobranca(assinatura.Plano, novoPlano, assinatura.TipoAssinatura))
        {
            assinatura.PlanoAlteracaoPendenteId = novoPlano.Id;
            assinatura.PlanoAlteracaoPendente = novoPlano;
            assinatura.UpdatedAt = DateTime.UtcNow;

            var pagamentoTroca = await _cobrancaAssinaturaService.GerarCobrancaTrocaPlanoAsync(
                assinatura,
                novoPlano,
                request.Gateway ?? assinatura.Gateway,
                request.Pagamento,
                cancellationToken);

            await _pagamentoRepository.AdicionarAsync(pagamentoTroca.Pagamento, cancellationToken);
            _assinaturaRepository.Atualizar(assinatura);
            await _assinaturaHistoricoService.RegistrarAssinaturaAsync(
                assinatura,
                "TrocaPlanoSolicitada",
                AssinaturaStatus.Ativa,
                assinatura.Status,
                pagamentoTroca.Pagamento,
                $"Troca de plano solicitada para o plano {novoPlano.Id}.",
                cancellationToken: cancellationToken);
            await _assinaturaHistoricoService.RegistrarPagamentoAsync(
                pagamentoTroca.Pagamento,
                "PagamentoTrocaPlanoCriado",
                null,
                pagamentoTroca.Pagamento.Status,
                "Cobranca de troca de plano criada no gateway.",
                pagamentoTroca.Pagamento.GatewayPaymentId,
                cancellationToken);
            await _assinaturaRepository.SalvarAlteracoesAsync(cancellationToken);

            return AssinaturaResponseDto.From(
                assinatura,
                PagamentoAssinaturaResponseDto.From(
                    pagamentoTroca.Pagamento,
                    pagamentoTroca.CheckoutUrl,
                    pagamentoTroca.QrCode));
        }

        var planoAnteriorId = assinatura.PlanoId;
        assinatura.PlanoId = novoPlano.Id;
        assinatura.Plano = novoPlano;
        assinatura.PlanoAlteracaoPendenteId = null;
        assinatura.PlanoAlteracaoPendente = null;
        assinatura.UpdatedAt = DateTime.UtcNow;

        _assinaturaRepository.Atualizar(assinatura);
        await _assinaturaHistoricoService.RegistrarAssinaturaAsync(
            assinatura,
            "TrocaPlanoAplicadaSemCobranca",
            AssinaturaStatus.Ativa,
            assinatura.Status,
            observacao: $"Plano alterado de {planoAnteriorId} para {novoPlano.Id} sem cobranca.",
            cancellationToken: cancellationToken);
        await _assinaturaRepository.SalvarAlteracoesAsync(cancellationToken);

        return AssinaturaResponseDto.From(assinatura);
    }

    public async Task<AssinaturaResponseDto> CancelarAsync(
        int assinaturaId,
        CancellationToken cancellationToken = default)
    {
        var userId = ObterUserIdAutenticado();
        var assinatura = await _assinaturaRepository.ObterPorIdComPlanoAsync(assinaturaId, cancellationToken);
        if (assinatura is null)
        {
            throw new AssinaturaNaoEncontradaException();
        }

        await ValidarPermissaoGerenciarAssinaturaAsync(assinatura, userId, cancellationToken);

        if (assinatura.Status is not (AssinaturaStatus.Ativa or AssinaturaStatus.Trial or AssinaturaStatus.Inadimplente))
        {
            throw new CancelamentoAssinaturaInvalidoException("Somente assinatura ativa, em trial ou inadimplente pode ser cancelada pelo usuario.");
        }

        var fimPeriodo = assinatura.ProximaDataVencimento
            ?? assinatura.Fim
            ?? DateTime.UtcNow;

        if (fimPeriodo.Date <= DateTime.UtcNow.Date)
        {
            await _assinaturaEncerramentoService.EncerrarAsync(
                assinatura,
                AssinaturaStatus.Cancelada,
                "AssinaturaCanceladaPeloUsuario",
                "Cancelamento solicitado pelo usuario.",
                cancellationToken: cancellationToken);

            await _assinaturaRepository.SalvarAlteracoesAsync(cancellationToken);
            await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

            await _assinaturaNotificacaoService.AssinaturaCanceladaAsync(
                assinatura,
                _currentUser.Email,
                cancellationToken);

            return AssinaturaResponseDto.From(assinatura);
        }

        var statusAnterior = assinatura.Status;
        assinatura.Status = AssinaturaStatus.CancelamentoAgendado;
        assinatura.CanceladoEm = DateTime.UtcNow;
        assinatura.RenovacaoAutomatica = false;
        assinatura.PlanoAlteracaoPendenteId = null;
        assinatura.PlanoAlteracaoPendente = null;
        assinatura.Fim = fimPeriodo;
        assinatura.UpdatedAt = DateTime.UtcNow;

        _assinaturaRepository.Atualizar(assinatura);
        await _assinaturaHistoricoService.RegistrarAssinaturaAsync(
            assinatura,
            "AssinaturaCancelamentoAgendado",
            statusAnterior,
            assinatura.Status,
            observacao: "Cancelamento agendado para o fim do periodo contratado.",
            cancellationToken: cancellationToken);
        await _assinaturaHistoricoService.RegistrarRecorrenciaAsync(
            assinatura,
            "RecorrenciaCancelamentoAgendado",
            "CancelamentoAgendado",
            cicloInicio: assinatura.Inicio,
            cicloFim: assinatura.Fim,
            observacao: "Renovacao automatica desativada; acesso mantido ate o fim do periodo.",
            cancellationToken: cancellationToken);
        await _assinaturaRepository.SalvarAlteracoesAsync(cancellationToken);

        await _assinaturaNotificacaoService.AssinaturaCanceladaAsync(
            assinatura,
            _currentUser.Email,
            cancellationToken);

        return AssinaturaResponseDto.From(assinatura);
    }

    public async Task<AssinaturaResponseDto> ObterAtualPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        var userId = ObterUserIdAutenticado();
        var vinculo = await _estabelecimentoUsuarioRepository.ObterAtivoAsync(
            estabelecimentoId,
            userId,
            cancellationToken);

        if (vinculo is null)
        {
            throw new UsuarioSemPermissaoAssinaturaException();
        }

        var assinatura = await _assinaturaRepository.ObterAssinaturaEfetivaPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        if (assinatura is null)
        {
            throw new AssinaturaNaoEncontradaException();
        }

        int? diasTrial = null;
        if (assinatura.CampanhaPromocionalId.HasValue)
        {
            var campanha = await _campanhaPromocionalRepository.ObterPorIdAsync(
                assinatura.CampanhaPromocionalId.Value,
                cancellationToken);
            diasTrial = campanha?.DiasTrial;
        }

        return AssinaturaResponseDto.From(assinatura, diasTrial: diasTrial);
    }

    public async Task<AdicionarEstabelecimentoAssinaturaResponseDto> AdicionarEstabelecimentoAsync(
        int assinaturaId,
        AdicionarEstabelecimentoAssinaturaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = ObterUserIdAutenticado();
        var assinatura = await _assinaturaRepository.ObterPorIdComPlanoAsync(assinaturaId, cancellationToken);
        if (assinatura is null)
        {
            throw new AssinaturaNaoEncontradaException();
        }

        await ValidarPermissaoOwnerAssinaturaAsync(assinatura, userId, cancellationToken);

        if (assinatura.Status is not (AssinaturaStatus.Ativa or AssinaturaStatus.Trial))
        {
            throw new TrocaPlanoAssinaturaInvalidaException(
                "Somente assinatura ativa ou em trial pode receber novas unidades.");
        }

        if (assinatura.TipoAssinatura == TipoAssinatura.ProfissionalAutonomo
            || !PlanoComercialCatalogo.PermiteMultiLoja(assinatura.Plano, assinatura.TipoAssinatura))
        {
            throw new TrocaPlanoAssinaturaInvalidaException(
                "O plano atual nao permite multiplas unidades.");
        }

        var lojasVinculadas = await _assinaturaEstabelecimentoRepository.ContarPorAssinaturaAsync(
            assinaturaId,
            cancellationToken);
        var limite = assinatura.Plano?.LimiteEstabelecimentos;
        if (!limite.HasValue || lojasVinculadas >= limite.Value)
        {
            throw new LimiteEstabelecimentosExcedidoException();
        }

        var estabelecimento = CriarEstabelecimento(request.Estabelecimento);
        await TentarGeocodificarEstabelecimentoAsync(estabelecimento, cancellationToken);
        await _estabelecimentoRepository.AdicionarAsync(estabelecimento, cancellationToken);

        await _estabelecimentoUsuarioRepository.AdicionarAsync(new EstabelecimentoUsuario
        {
            Estabelecimento = estabelecimento,
            UsuarioId = userId,
            RoleNoEstabelecimento = EstablishmentUserRole.Owner,
            Ativo = true
        }, cancellationToken);

        await _assinaturaEstabelecimentoRepository.AdicionarAsync(new AssinaturaEstabelecimento
        {
            AssinaturaId = assinaturaId,
            Estabelecimento = estabelecimento,
            EhMatriz = false
        }, cancellationToken);

        await _assinaturaEstabelecimentoRepository.SalvarAlteracoesAsync(cancellationToken);

        return new AdicionarEstabelecimentoAssinaturaResponseDto(
            estabelecimento.Id,
            estabelecimento.Nome,
            assinaturaId);
    }

    private async Task GarantirVinculoMatrizAsync(
        Assinatura assinatura,
        CancellationToken cancellationToken)
    {
        if (!assinatura.EstabelecimentoId.HasValue
            || !PlanoComercialCatalogo.PermiteMultiLoja(assinatura.Plano, assinatura.TipoAssinatura))
        {
            return;
        }

        var vinculoExistente = await _assinaturaEstabelecimentoRepository.ObterPorEstabelecimentoAsync(
            assinatura.EstabelecimentoId.Value,
            cancellationToken);
        if (vinculoExistente is not null)
        {
            return;
        }

        await _assinaturaEstabelecimentoRepository.AdicionarAsync(new AssinaturaEstabelecimento
        {
            AssinaturaId = assinatura.Id,
            EstabelecimentoId = assinatura.EstabelecimentoId.Value,
            EhMatriz = true
        }, cancellationToken);

        await _assinaturaEstabelecimentoRepository.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task ValidarDowngradeMultiLojaAsync(
        Assinatura assinatura,
        Plano novoPlano,
        CancellationToken cancellationToken)
    {
        if (!PlanoComercialCatalogo.PermiteMultiLoja(assinatura.Plano, assinatura.TipoAssinatura)
            || PlanoComercialCatalogo.PermiteMultiLoja(novoPlano, assinatura.TipoAssinatura))
        {
            return;
        }

        var lojasVinculadas = await _assinaturaEstabelecimentoRepository.ContarPorAssinaturaAsync(
            assinatura.Id,
            cancellationToken);
        if (lojasVinculadas > 1)
        {
            throw new DowngradeComMultiplasLojasException();
        }
    }

    private bool DeveAdiarOnboarding(IniciarAssinaturaRequestDto request, bool elegivelTrial) =>
        _mercadoPagoOptions.UsarCheckoutPro
        && !elegivelTrial
        && (request.Estabelecimento is not null || request.ProfissionalAutonomo is not null);

    private async Task<Assinatura> CriarAssinaturaComOnboardingPendenteAsync(
        IniciarAssinaturaRequestDto request,
        int userId,
        CancellationToken cancellationToken)
    {
        if (request.TipoAssinatura == TipoAssinatura.Estabelecimento)
        {
            var vinculos = await _estabelecimentoUsuarioRepository.ListarAtivosPorUsuarioAsync(userId, cancellationToken)
                ?? Array.Empty<EstabelecimentoUsuario>();
            if (vinculos.Any(v =>
                    v.RoleNoEstabelecimento == EstablishmentUserRole.Owner
                    && v.Estabelecimento is not null
                    && v.Estabelecimento.Ativo))
            {
                throw new EstabelecimentoOnboardingDuplicadoException();
            }
        }

        var payload = new AssinaturaOnboardingPendentePayload(
            userId,
            request.TipoAssinatura,
            request.Estabelecimento,
            request.ProfissionalAutonomo);

        var assinatura = CriarAssinaturaBase(request.PlanoId, request.Gateway, request.TipoAssinatura);
        assinatura.OnboardingPendenteJson = JsonSerializer.Serialize(payload, JsonOptions);
        return assinatura;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private async Task<Assinatura> CriarParaEstabelecimentoAsync(
        IniciarAssinaturaRequestDto request,
        int userId,
        CancellationToken cancellationToken)
    {
        if (request.Estabelecimento is not null)
        {
            return await CriarParaNovoEstabelecimentoAsync(request, userId, cancellationToken);
        }

        var estabelecimentoId = request.EstabelecimentoId!.Value;
        var estabelecimento = await _estabelecimentoRepository.ObterPorIdAsync(estabelecimentoId, cancellationToken);
        if (estabelecimento is null || !estabelecimento.Ativo)
        {
            throw new TitularAssinaturaNaoEncontradoException();
        }

        var vinculo = await _estabelecimentoUsuarioRepository.ObterAtivoAsync(estabelecimentoId, userId, cancellationToken);
        if (vinculo is null)
        {
            throw new UsuarioSemPermissaoAssinaturaException();
        }

        if (await _assinaturaRepository.ExisteAtivaOuPendentePorEstabelecimentoAsync(estabelecimentoId, cancellationToken))
        {
            throw new AssinaturaDuplicadaException();
        }

        var assinatura = CriarAssinaturaBase(request.PlanoId, request.Gateway, request.TipoAssinatura);
        assinatura.EstabelecimentoId = estabelecimentoId;

        return assinatura;
    }

    private async Task<Assinatura> CriarParaNovoEstabelecimentoAsync(
        IniciarAssinaturaRequestDto request,
        int userId,
        CancellationToken cancellationToken)
    {
        var vinculos = await _estabelecimentoUsuarioRepository.ListarAtivosPorUsuarioAsync(
            userId,
            cancellationToken)
            ?? Array.Empty<EstabelecimentoUsuario>();
        if (vinculos.Any(v =>
                v.RoleNoEstabelecimento == EstablishmentUserRole.Owner
                && v.Estabelecimento is not null
                && v.Estabelecimento.Ativo))
        {
            throw new EstabelecimentoOnboardingDuplicadoException();
        }

        var estabelecimento = CriarEstabelecimento(request.Estabelecimento!);
        await TentarGeocodificarEstabelecimentoAsync(estabelecimento, cancellationToken);
        await _estabelecimentoRepository.AdicionarAsync(estabelecimento, cancellationToken);

        await _estabelecimentoUsuarioRepository.AdicionarAsync(new EstabelecimentoUsuario
        {
            Estabelecimento = estabelecimento,
            UsuarioId = userId,
            RoleNoEstabelecimento = EstablishmentUserRole.Owner,
            Ativo = true
        }, cancellationToken);

        var assinatura = CriarAssinaturaBase(request.PlanoId, request.Gateway, request.TipoAssinatura);
        assinatura.Estabelecimento = estabelecimento;

        return assinatura;
    }

    private async Task<Assinatura> CriarParaProfissionalAutonomoAsync(
        IniciarAssinaturaRequestDto request,
        int userId,
        CancellationToken cancellationToken)
    {
        if (request.ProfissionalAutonomo is not null)
        {
            return await CriarTenantParaNovoOuExistenteProfissionalAutonomoAsync(request, userId, cancellationToken);
        }

        var profissionalId = request.ProfissionalAutonomoId!.Value;
        var profissional = await _profissionalRepository.ObterPorIdAsync(profissionalId, cancellationToken);
        if (profissional is null || !profissional.Ativo || profissional.TipoProfissional != ProfessionalType.Autonomo)
        {
            throw new TitularAssinaturaNaoEncontradoException();
        }

        if (profissional.UsuarioId != userId)
        {
            throw new UsuarioSemPermissaoAssinaturaException();
        }

        var vinculo = await _profissionalEstabelecimentoRepository.ObterAtivoPorProfissionalAsync(
            profissionalId,
            cancellationToken);

        Estabelecimento estabelecimento;
        if (vinculo?.Estabelecimento is not null)
        {
            estabelecimento = vinculo.Estabelecimento;
        }
        else
        {
            estabelecimento = CriarEstabelecimentoParaProfissionalAutonomo(profissional);
            await TentarGeocodificarEstabelecimentoAsync(estabelecimento, cancellationToken);
            await _estabelecimentoRepository.AdicionarAsync(estabelecimento, cancellationToken);
            await CriarVinculosTenantProfissionalAutonomoAsync(
                estabelecimento,
                profissional,
                userId,
                cancellationToken);
        }

        if (await _assinaturaRepository.ExisteAtivaOuPendentePorEstabelecimentoAsync(estabelecimento.Id, cancellationToken))
        {
            throw new AssinaturaDuplicadaException();
        }

        var assinatura = CriarAssinaturaBase(request.PlanoId, request.Gateway, request.TipoAssinatura);
        assinatura.Estabelecimento = estabelecimento;
        if (estabelecimento.Id > 0)
        {
            assinatura.EstabelecimentoId = estabelecimento.Id;
        }

        return assinatura;
    }

    private async Task<Assinatura> CriarTenantParaNovoOuExistenteProfissionalAutonomoAsync(
        IniciarAssinaturaRequestDto request,
        int userId,
        CancellationToken cancellationToken)
    {
        var profissional = await _profissionalRepository.ObterPorUsuarioIdAsync(userId, cancellationToken);
        if (profissional is null)
        {
            profissional = CriarProfissionalAutonomo(request.ProfissionalAutonomo!, userId);
            await _profissionalRepository.AdicionarAsync(profissional, cancellationToken);
        }
        else
        {
            ValidarPerfilProfissionalAutonomoExistente(profissional);

            AtualizarProfissionalAutonomo(profissional, request.ProfissionalAutonomo!);
            _profissionalRepository.Atualizar(profissional);
        }

        var vinculo = await _profissionalEstabelecimentoRepository.ObterAtivoPorProfissionalAsync(
            profissional.Id,
            cancellationToken);
        var estabelecimento = vinculo?.Estabelecimento;

        if (estabelecimento is not null)
        {
            if (await _assinaturaRepository.ExisteAtivaOuPendentePorEstabelecimentoAsync(estabelecimento.Id, cancellationToken))
            {
                throw new AssinaturaDuplicadaException();
            }

            AtualizarEstabelecimentoAutonomo(estabelecimento, request.ProfissionalAutonomo!);
            await TentarGeocodificarEstabelecimentoAsync(estabelecimento, cancellationToken);
            _estabelecimentoRepository.Atualizar(estabelecimento);
        }
        else
        {
            estabelecimento = CriarEstabelecimentoParaProfissionalAutonomo(request.ProfissionalAutonomo!);
            await TentarGeocodificarEstabelecimentoAsync(estabelecimento, cancellationToken);
            await _estabelecimentoRepository.AdicionarAsync(estabelecimento, cancellationToken);
            await CriarVinculosTenantProfissionalAutonomoAsync(
                estabelecimento,
                profissional,
                userId,
                cancellationToken);
        }

        var assinatura = CriarAssinaturaBase(request.PlanoId, request.Gateway, request.TipoAssinatura);
        assinatura.Estabelecimento = estabelecimento;

        return assinatura;
    }

    private async Task CriarVinculosTenantProfissionalAutonomoAsync(
        Estabelecimento estabelecimento,
        Profissional profissional,
        int userId,
        CancellationToken cancellationToken)
    {
        await _estabelecimentoUsuarioRepository.AdicionarAsync(new EstabelecimentoUsuario
        {
            Estabelecimento = estabelecimento,
            UsuarioId = userId,
            RoleNoEstabelecimento = EstablishmentUserRole.Owner,
            Ativo = true
        }, cancellationToken);

        await _profissionalEstabelecimentoRepository.AdicionarAsync(new ProfissionalEstabelecimento
        {
            Estabelecimento = estabelecimento,
            Profissional = profissional,
            Ativo = true,
            PodeReceberAgendamento = true
        }, cancellationToken);
    }

    private static void ValidarTitular(IniciarAssinaturaRequestDto request)
    {
        var titularEstabelecimento = request.EstabelecimentoId.HasValue;
        var novoEstabelecimento = request.Estabelecimento is not null;
        var titularAutonomo = request.ProfissionalAutonomoId.HasValue;
        var novoAutonomo = request.ProfissionalAutonomo is not null;

        var titularValido = request.TipoAssinatura switch
        {
            TipoAssinatura.Estabelecimento => (titularEstabelecimento ^ novoEstabelecimento) && !titularAutonomo && !novoAutonomo,
            TipoAssinatura.ProfissionalAutonomo => (titularAutonomo ^ novoAutonomo) && !titularEstabelecimento && !novoEstabelecimento,
            _ => false
        };

        if (!titularValido)
        {
            throw new AssinaturaTitularInvalidoException();
        }
    }

    private Estabelecimento CriarEstabelecimento(CriarEstabelecimentoAssinaturaDto dto)
    {
        static Exception CriarExcecao(string mensagem) => new EstabelecimentoAssinaturaInvalidoException(mensagem);

        if (dto.Descricao.Length > 500)
        {
            throw new EstabelecimentoAssinaturaInvalidoException("Descricao do estabelecimento deve ter no maximo 500 caracteres.");
        }

        if (dto.CategoriaEstabelecimentoId is null)
        {
            throw new EstabelecimentoAssinaturaInvalidoException("Categoria do estabelecimento e obrigatoria.");
        }

        return new Estabelecimento
        {
            Nome = OperacaoPerfilValidation.ValidarTextoObrigatorio(dto.Nome, "Nome do estabelecimento", 150, CriarExcecao),
            Descricao = dto.Descricao.Trim(),
            Logo = OperacaoPerfilValidation.ValidarLogoBase64(
                dto.Logo,
                "Logo do estabelecimento",
                _avatarBase64Decoder,
                CriarExcecao),
            Telefone = TelefoneHelper.NormalizarParaArmazenamento(
                OperacaoPerfilValidation.ValidarTextoObrigatorio(dto.Telefone, "Telefone do estabelecimento", 20, CriarExcecao)),
            Email = OperacaoPerfilValidation.ValidarTextoObrigatorio(dto.Email, "Email do estabelecimento", 255, CriarExcecao),
            CategoriaEstabelecimentoId = dto.CategoriaEstabelecimentoId,
            Ativo = true,
            Endereco = OperacaoPerfilValidation.CriarEndereco(dto.Endereco, CriarExcecao),
            Caixa = new Caixa()
        };
    }

    private Estabelecimento CriarEstabelecimentoParaProfissionalAutonomo(
        CriarProfissionalAutonomoAssinaturaDto dto)
    {
        var logo = ValidarLogoProfissionalAutonomo(dto);

        return new Estabelecimento
        {
            Nome = dto.NomePublico.Trim(),
            Descricao = dto.Biografia.Trim(),
            Logo = logo,
            Telefone = TelefoneHelper.NormalizarParaArmazenamento(dto.Telefone),
            Email = dto.Email.Trim(),
            Ativo = true,
            Endereco = OperacaoPerfilValidation.CriarEndereco(
                dto.Endereco,
                mensagem => new ProfissionalAutonomoAssinaturaInvalidoException(mensagem)),
            Caixa = new Caixa()
        };
    }

    private static Estabelecimento CriarEstabelecimentoParaProfissionalAutonomo(Profissional profissional)
    {
        return new Estabelecimento
        {
            Nome = profissional.NomePublico.Trim(),
            Descricao = profissional.Biografia.Trim(),
            Logo = profissional.Logo.Trim(),
            Telefone = TelefoneHelper.NormalizarParaArmazenamento(profissional.Telefone),
            Email = profissional.Email.Trim(),
            Ativo = true,
            Caixa = new Caixa()
        };
    }

    private void AtualizarEstabelecimentoAutonomo(
        Estabelecimento estabelecimento,
        CriarProfissionalAutonomoAssinaturaDto dto)
    {
        var logo = ValidarLogoProfissionalAutonomo(dto);

        estabelecimento.Nome = dto.NomePublico.Trim();
        estabelecimento.Descricao = dto.Biografia.Trim();
        estabelecimento.Logo = logo;
        estabelecimento.Telefone = TelefoneHelper.NormalizarParaArmazenamento(dto.Telefone);
        estabelecimento.Email = dto.Email.Trim();
        estabelecimento.Ativo = true;
        estabelecimento.UpdatedAt = DateTime.UtcNow;

        OperacaoPerfilValidation.AtualizarEndereco(
            estabelecimento.Endereco,
            endereco => estabelecimento.Endereco = endereco,
            dto.Endereco,
            mensagem => new ProfissionalAutonomoAssinaturaInvalidoException(mensagem));

        estabelecimento.Caixa ??= new Caixa();
    }

    private Profissional CriarProfissionalAutonomo(
        CriarProfissionalAutonomoAssinaturaDto dto,
        int userId)
    {
        var logo = ValidarLogoProfissionalAutonomo(dto);

        return new Profissional
        {
            UsuarioId = userId,
            NomePublico = dto.NomePublico.Trim(),
            Biografia = dto.Biografia.Trim(),
            Logo = logo,
            Telefone = TelefoneHelper.NormalizarParaArmazenamento(dto.Telefone),
            Email = dto.Email.Trim(),
            TipoProfissional = ProfessionalType.Autonomo,
            Ativo = true
        };
    }

    private void AtualizarProfissionalAutonomo(
        Profissional profissional,
        CriarProfissionalAutonomoAssinaturaDto dto)
    {
        var logo = ValidarLogoProfissionalAutonomo(dto);

        profissional.NomePublico = dto.NomePublico.Trim();
        profissional.Biografia = dto.Biografia.Trim();
        profissional.Logo = logo;
        profissional.Telefone = TelefoneHelper.NormalizarParaArmazenamento(dto.Telefone);
        profissional.Email = dto.Email.Trim();
        profissional.TipoProfissional = ProfessionalType.Autonomo;
        profissional.Ativo = true;
        profissional.UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidarPerfilProfissionalAutonomoExistente(Profissional profissional)
    {
        if (profissional.TipoProfissional != ProfessionalType.Autonomo)
        {
            throw new ProfissionalAutonomoAssinaturaInvalidoException(
                "Usuario ja possui um perfil profissional que nao e autonomo.");
        }
    }

    private string ValidarLogoProfissionalAutonomo(CriarProfissionalAutonomoAssinaturaDto dto)
    {
        ValidarProfissionalAutonomo(dto);

        return OperacaoPerfilValidation.ValidarLogoBase64(
            dto.Logo,
            "Logo do profissional",
            _avatarBase64Decoder,
            mensagem => new ProfissionalAutonomoAssinaturaInvalidoException(mensagem));
    }

    private static void ValidarProfissionalAutonomo(CriarProfissionalAutonomoAssinaturaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NomePublico))
        {
            throw new ProfissionalAutonomoAssinaturaInvalidoException("Nome publico do profissional e obrigatorio.");
        }

        if (dto.NomePublico.Length > 150)
        {
            throw new ProfissionalAutonomoAssinaturaInvalidoException("Nome publico do profissional deve ter no maximo 150 caracteres.");
        }

        if (dto.Biografia.Length > 1000)
        {
            throw new ProfissionalAutonomoAssinaturaInvalidoException("Biografia do profissional deve ter no maximo 1000 caracteres.");
        }

        OperacaoPerfilValidation.ValidarTextoObrigatorio(
            dto.Telefone,
            "Telefone do profissional",
            20,
            mensagem => new ProfissionalAutonomoAssinaturaInvalidoException(mensagem));

        OperacaoPerfilValidation.ValidarTextoObrigatorio(
            dto.Email,
            "Email do profissional",
            255,
            mensagem => new ProfissionalAutonomoAssinaturaInvalidoException(mensagem));
    }

    private static Assinatura CriarAssinaturaBase(
        int planoId,
        GatewayPagamento gateway,
        TipoAssinatura tipoAssinatura)
    {
        var agora = DateTime.UtcNow;
        return new()
        {
            PlanoId = planoId,
            Status = AssinaturaStatus.PendentePagamento,
            TipoAssinatura = tipoAssinatura,
            Inicio = agora,
            DataReferenciaCiclo = agora.Date,
            DiaVencimento = agora.Day,
            Gateway = gateway,
            RenovacaoAutomatica = true
        };
    }

    private static void InicializarReferenciaCiclo(Assinatura assinatura)
    {
        if (assinatura.DataReferenciaCiclo == default)
        {
            var referencia = assinatura.Inicio == default ? DateTime.UtcNow : assinatura.Inicio;
            assinatura.DataReferenciaCiclo = referencia.Date;
            assinatura.DiaVencimento = referencia.Day;
        }
    }

    private async Task<bool> ElegivelPromocaoTrialAsync(
        IniciarAssinaturaRequestDto request,
        int userId,
        CancellationToken cancellationToken)
    {
        if (await _assinaturaRepository.UsuarioJaTeveAssinaturaAsync(userId, cancellationToken))
        {
            return false;
        }

        var promoStatus = await _promocaoLancamentoService.ObterStatusAsync(cancellationToken);
        if (!promoStatus.Disponivel)
        {
            return false;
        }

        if (_mercadoPagoOptions.UsarCheckoutPro)
        {
            if (request.Pagamento is not null
                && string.Equals(request.Pagamento.PaymentMethodId, "pix", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }
        else if (!PagamentoCompativelComTrial(request.Pagamento))
        {
            return false;
        }

        var estabelecimentoIdPromo = request.EstabelecimentoId ?? 0;
        if (estabelecimentoIdPromo > 0
            && await _promocaoLancamentoService.EstabelecimentoJaUsouPromocaoAsync(estabelecimentoIdPromo, cancellationToken))
        {
            return false;
        }

        if (!await _promocaoLancamentoService.TentarReservarVagaAsync(estabelecimentoIdPromo, cancellationToken))
        {
            return false;
        }

        var campanha = await _campanhaPromocionalRepository.ObterAtivaPorCodigoAsync(
            PromocaoLancamentoService.CodigoCampanhaLancamento,
            cancellationToken);

        return campanha is not null;
    }

    private async Task<int?> TentarIniciarComTrialAsync(
        Assinatura assinatura,
        Plano plano,
        IniciarAssinaturaRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!await ElegivelPromocaoTrialAsync(request, ObterUserIdAutenticado(), cancellationToken))
        {
            return null;
        }

        var campanha = await _campanhaPromocionalRepository.ObterAtivaPorCodigoAsync(
            PromocaoLancamentoService.CodigoCampanhaLancamento,
            cancellationToken);

        if (campanha is null)
        {
            return null;
        }

        var inicio = DateTime.UtcNow;
        var fimTrial = _cicloCobrancaService.CalcularFimTrial(inicio, campanha.DiasTrial);
        var ciclo = _cicloCobrancaService.CalcularPrimeiroCiclo(
            assinatura.DataReferenciaCiclo,
            fimTrial,
            plano.Periodo);

        var gateway = _gatewayPagamentoResolver.Resolver(assinatura.Gateway);
        var referenciaInterna = $"trial-{Guid.NewGuid():N}";
        CriarAssinaturaRecorrenteGatewayResponse? response = null;
        var trialSemRecorrenciaNoGateway = _mercadoPagoOptions.UsarCheckoutPro
            || _mercadoPagoOptions.PermitirTrialSemRecorrenciaNoGateway;

        var valorMensalidade = AssinaturaValorCobranca.CalcularMensalidade(
            PlanoComercialCatalogo.ResolverPreco(plano, assinatura.TipoAssinatura),
            campanha.PercentualDescontoMensalidade);

        if (!trialSemRecorrenciaNoGateway)
        {
            response = await gateway.CriarAssinaturaRecorrenteAsync(new CriarAssinaturaRecorrenteGatewayRequest(
                Gateway: assinatura.Gateway,
                ReferenciaInterna: referenciaInterna,
                Descricao: $"Assinatura {plano.Nome} - trial {campanha.DiasTrial} dias",
                Valor: valorMensalidade,
                Moeda: "BRL",
                PagadorNome: _currentUser.Email ?? "Usuario Glow",
                PagadorEmail: _currentUser.Email ?? string.Empty,
                DiasTrial: campanha.DiasTrial,
                PrimeiraCobrancaEm: ciclo.Vencimento,
                PagamentoTransparente: CriarPagamentoTransparenteRequest(assinatura.Gateway, request.Pagamento),
                Metadados: new Dictionary<string, string>
                {
                    ["planoId"] = plano.Id.ToString(),
                    ["campanha"] = campanha.Codigo,
                    ["dataReferenciaCiclo"] = assinatura.DataReferenciaCiclo.ToString("O")
                }),
                cancellationToken);

            if (!response.Sucesso && DeveAtivarTrialSemRecorrenciaNoGateway(response))
            {
                trialSemRecorrenciaNoGateway = true;
            }
            else if (!response.Sucesso)
            {
                throw new GatewayPagamentoException(
                    response.MensagemErro ?? "Nao foi possivel criar assinatura recorrente no gateway.",
                    GatewayPagamentoErrorDetails.FromAssinaturaRecorrente(response));
            }
        }

        assinatura.Status = AssinaturaStatus.Trial;
        assinatura.CampanhaPromocionalId = campanha.Id;
        assinatura.PercentualDescontoPermanente = campanha.PercentualDescontoMensalidade;
        assinatura.Inicio = inicio;
        assinatura.Fim = ciclo.Vencimento;
        assinatura.GatewaySubscriptionId = trialSemRecorrenciaNoGateway
            ? string.Empty
            : response!.GatewaySubscriptionId;
        assinatura.GatewayCustomerId = trialSemRecorrenciaNoGateway
            ? string.Empty
            : response!.GatewayCustomerId ?? string.Empty;
        _cicloCobrancaService.AplicarCicloNaAssinatura(assinatura, ciclo);

        var observacaoTrial = trialSemRecorrenciaNoGateway
            ? _mercadoPagoOptions.UsarCheckoutPro
                ? $"Trial de {campanha.DiasTrial} dias iniciado via campanha {campanha.Codigo} com Checkout Pro (sem cobranca inicial)."
                : $"Trial de {campanha.DiasTrial} dias iniciado via campanha {campanha.Codigo} sem recorrencia no gateway (sandbox/flag ativa)."
            : $"Trial de {campanha.DiasTrial} dias iniciado via campanha {campanha.Codigo}.";
        var payloadHistorico = trialSemRecorrenciaNoGateway
            ? response?.ResponsePayload ?? "{}"
            : response!.ResponsePayload;

        await _assinaturaHistoricoService.RegistrarAssinaturaAsync(
            assinatura,
            "TrialIniciado",
            null,
            assinatura.Status,
            observacao: observacaoTrial,
            payloadJson: payloadHistorico,
            cancellationToken: cancellationToken);
        await _assinaturaHistoricoService.RegistrarRecorrenciaAsync(
            assinatura,
            "TrialAtivo",
            "Trial",
            cicloInicio: inicio,
            cicloFim: ciclo.Vencimento,
            observacao: trialSemRecorrenciaNoGateway
                ? "Periodo de trial ativo com modulos liberados. Cobranca recorrente sera criada no fim do trial."
                : "Periodo de trial ativo com modulos liberados.",
            payloadJson: payloadHistorico,
            cancellationToken: cancellationToken);

        return campanha.DiasTrial;
    }

    private bool DeveAtivarTrialSemRecorrenciaNoGateway(CriarAssinaturaRecorrenteGatewayResponse response)
    {
        if (_mercadoPagoOptions.PermitirTrialSemRecorrenciaNoGateway)
        {
            return true;
        }

        if (!TokenMercadoPagoSandboxAtivo())
        {
            return false;
        }

        var status = response.FailureInfo?.HttpStatusCode;
        if (status is not (500 or 503))
        {
            return false;
        }

        return response.FailureInfo?.RequestUri?.Contains("preapproval", StringComparison.OrdinalIgnoreCase) == true;
    }

    private bool TokenMercadoPagoSandboxAtivo() =>
        !string.IsNullOrWhiteSpace(_mercadoPagoOptions.AccessToken)
        && _mercadoPagoOptions.AccessToken.TrimStart().StartsWith("TEST-", StringComparison.OrdinalIgnoreCase);

    private static bool PagamentoCompativelComTrial(PagamentoTransparenteMercadoPagoDto? pagamento) =>
        pagamento is not null
        && !string.IsNullOrWhiteSpace(pagamento.Token)
        && !string.Equals(pagamento.PaymentMethodId, "pix", StringComparison.OrdinalIgnoreCase);

    private static PagamentoTransparenteGatewayRequest? CriarPagamentoTransparenteRequest(
        GatewayPagamento gateway,
        PagamentoTransparenteMercadoPagoDto? pagamento)
    {
        if (gateway != GatewayPagamento.MercadoPago)
        {
            return null;
        }

        if (pagamento is null)
        {
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

    private async Task ValidarPermissaoGerenciarAssinaturaAsync(
        Assinatura assinatura,
        int userId,
        CancellationToken cancellationToken)
    {
        if (assinatura.EstabelecimentoId.HasValue)
        {
            var vinculo = await _estabelecimentoUsuarioRepository.ObterAtivoAsync(
                assinatura.EstabelecimentoId.Value,
                userId,
                cancellationToken);

            if (vinculo is null)
            {
                throw new UsuarioSemPermissaoAssinaturaException();
            }

            return;
        }

        throw new AssinaturaTitularInvalidoException();
    }

    /// <summary>
    /// Adicionar unidade é exclusivo do proprietário (Owner) da assinatura titular.
    /// </summary>
    private async Task ValidarPermissaoOwnerAssinaturaAsync(
        Assinatura assinatura,
        int userId,
        CancellationToken cancellationToken)
    {
        if (!assinatura.EstabelecimentoId.HasValue)
        {
            throw new AssinaturaTitularInvalidoException();
        }

        var vinculo = await _estabelecimentoUsuarioRepository.ObterAtivoAsync(
            assinatura.EstabelecimentoId.Value,
            userId,
            cancellationToken);

        if (vinculo is null || vinculo.RoleNoEstabelecimento != EstablishmentUserRole.Owner)
        {
            throw new UsuarioSemPermissaoAssinaturaException();
        }
    }

    private static void ValidarAssinaturaPermiteTroca(Assinatura assinatura)
    {
        if (assinatura.Status != AssinaturaStatus.Ativa)
        {
            throw new TrocaPlanoAssinaturaInvalidaException("Somente assinatura ativa pode trocar de plano.");
        }

        if (assinatura.PlanoAlteracaoPendenteId.HasValue)
        {
            throw new TrocaPlanoAssinaturaInvalidaException("Assinatura ja possui troca de plano pendente.");
        }
    }

    private static bool TrocaExigeCobranca(
        Plano? planoAtual,
        Plano novoPlano,
        TipoAssinatura tipoAssinatura)
    {
        if (planoAtual is null)
        {
            return true;
        }

        var precoAtual = PlanoComercialCatalogo.ResolverPreco(planoAtual, tipoAssinatura);
        var precoNovo = PlanoComercialCatalogo.ResolverPreco(novoPlano, tipoAssinatura);
        return precoAtual != precoNovo || planoAtual.Periodo != novoPlano.Periodo;
    }

    private async Task TentarGeocodificarEstabelecimentoAsync(
        Estabelecimento estabelecimento,
        CancellationToken cancellationToken)
    {
        if (estabelecimento.Endereco is null)
        {
            return;
        }

        await _enderecoGeocodificacaoService.TentarGeocodificarAsync(estabelecimento.Endereco, cancellationToken);
    }

    private async Task PromoverRoleOnboardingAsync(
        int userId,
        TipoAssinatura tipoAssinatura,
        CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(userId, cancellationToken);
        if (usuario is null || usuario.Role != UserRole.Cliente)
        {
            return;
        }

        usuario.Role = tipoAssinatura switch
        {
            TipoAssinatura.Estabelecimento => UserRole.DonoEstabelecimento,
            TipoAssinatura.ProfissionalAutonomo => UserRole.ProfissionalAutonomo,
            _ => usuario.Role
        };
        usuario.UpdatedAt = DateTime.UtcNow;

        _usuarioRepository.Atualizar(usuario);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task<AssinaturaResponseDto> MontarRespostaInicioAsync(
        Assinatura assinatura,
        CancellationToken cancellationToken,
        PagamentoAssinaturaResponseDto? pagamentoInicial = null,
        int? diasTrial = null)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(ObterUserIdAutenticado(), cancellationToken);
        var requerConfirmacaoEmail = usuario is not null
            && !usuario.Ativo
            && usuario.PendenteConfirmacaoEmail();

        return AssinaturaResponseDto.From(
            assinatura,
            pagamentoInicial,
            diasTrial,
            requerConfirmacaoEmail);
    }

    private int ObterUserIdAutenticado()
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedException();
        }

        return _currentUser.UserId.Value;
    }

}
