using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.DTOs.Pagamentos;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Pagamentos;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Domain.Exceptions.Auth;

namespace GLOWAPI.Application.Services;

public class AssinaturaService : IAssinaturaService
{
    private readonly IAssinaturaRepository _assinaturaRepository;
    private readonly IPlanoRepository _planoRepository;
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IPagamentoRepository _pagamentoRepository;
    private readonly IGatewayPagamentoResolver _gatewayPagamentoResolver;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAssinaturaNotificacaoService _assinaturaNotificacaoService;

    public AssinaturaService(
        IAssinaturaRepository assinaturaRepository,
        IPlanoRepository planoRepository,
        IEstabelecimentoRepository estabelecimentoRepository,
        IEstabelecimentoUsuarioRepository estabelecimentoUsuarioRepository,
        IProfissionalRepository profissionalRepository,
        IPagamentoRepository pagamentoRepository,
        IGatewayPagamentoResolver gatewayPagamentoResolver,
        ICurrentUserContext currentUser,
        IAssinaturaNotificacaoService assinaturaNotificacaoService)
    {
        _assinaturaRepository = assinaturaRepository;
        _planoRepository = planoRepository;
        _estabelecimentoRepository = estabelecimentoRepository;
        _estabelecimentoUsuarioRepository = estabelecimentoUsuarioRepository;
        _profissionalRepository = profissionalRepository;
        _pagamentoRepository = pagamentoRepository;
        _gatewayPagamentoResolver = gatewayPagamentoResolver;
        _currentUser = currentUser;
        _assinaturaNotificacaoService = assinaturaNotificacaoService;
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

        var assinatura = request.TipoAssinatura switch
        {
            TipoAssinatura.Estabelecimento => await CriarParaEstabelecimentoAsync(request, userId, cancellationToken),
            TipoAssinatura.ProfissionalAutonomo => await CriarParaProfissionalAutonomoAsync(request, userId, cancellationToken),
            _ => throw new AssinaturaTitularInvalidoException()
        };

        var pagamentoInicial = await CriarPagamentoInicialAsync(
            assinatura,
            plano,
            request.Pagamento,
            cancellationToken);

        await _assinaturaRepository.AdicionarAsync(assinatura, cancellationToken);
        await _pagamentoRepository.AdicionarAsync(pagamentoInicial.Pagamento, cancellationToken);
        await _assinaturaRepository.SalvarAlteracoesAsync(cancellationToken);

        if (assinatura.Estabelecimento is not null && !assinatura.EstabelecimentoId.HasValue)
        {
            assinatura.EstabelecimentoId = assinatura.Estabelecimento.Id;
        }

        if (assinatura.ProfissionalAutonomo is not null && !assinatura.ProfissionalAutonomoId.HasValue)
        {
            assinatura.ProfissionalAutonomoId = assinatura.ProfissionalAutonomo.Id;
        }

        if (!pagamentoInicial.Pagamento.AssinaturaId.HasValue)
        {
            pagamentoInicial.Pagamento.AssinaturaId = assinatura.Id;
        }

        await _assinaturaNotificacaoService.AssinaturaIniciadaAsync(
            assinatura,
            plano,
            _currentUser.Email,
            cancellationToken);

        return AssinaturaResponseDto.From(
            assinatura,
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

        if (TrocaExigeCobranca(assinatura.Plano, novoPlano))
        {
            assinatura.PlanoAlteracaoPendenteId = novoPlano.Id;
            assinatura.PlanoAlteracaoPendente = novoPlano;
            assinatura.UpdatedAt = DateTime.UtcNow;

            var pagamentoTroca = await CriarPagamentoTrocaPlanoAsync(
                assinatura,
                novoPlano,
                request.Gateway ?? assinatura.Gateway,
                request.Pagamento,
                cancellationToken);

            await _pagamentoRepository.AdicionarAsync(pagamentoTroca.Pagamento, cancellationToken);
            _assinaturaRepository.Atualizar(assinatura);
            await _assinaturaRepository.SalvarAlteracoesAsync(cancellationToken);

            return AssinaturaResponseDto.From(
                assinatura,
                PagamentoAssinaturaResponseDto.From(
                    pagamentoTroca.Pagamento,
                    pagamentoTroca.CheckoutUrl,
                    pagamentoTroca.QrCode));
        }

        assinatura.PlanoId = novoPlano.Id;
        assinatura.Plano = novoPlano;
        assinatura.PlanoAlteracaoPendenteId = null;
        assinatura.PlanoAlteracaoPendente = null;
        assinatura.UpdatedAt = DateTime.UtcNow;

        _assinaturaRepository.Atualizar(assinatura);
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

        if (assinatura.Status != AssinaturaStatus.Ativa)
        {
            throw new CancelamentoAssinaturaInvalidoException("Somente assinatura ativa pode ser cancelada pelo usuario.");
        }

        assinatura.Status = AssinaturaStatus.Cancelada;
        assinatura.CanceladoEm = DateTime.UtcNow;
        assinatura.RenovacaoAutomatica = false;
        assinatura.PlanoAlteracaoPendenteId = null;
        assinatura.PlanoAlteracaoPendente = null;
        assinatura.UpdatedAt = DateTime.UtcNow;

        _assinaturaRepository.Atualizar(assinatura);
        await _assinaturaRepository.SalvarAlteracoesAsync(cancellationToken);

        await _assinaturaNotificacaoService.AssinaturaCanceladaAsync(
            assinatura,
            _currentUser.Email,
            cancellationToken);

        return AssinaturaResponseDto.From(assinatura);
    }

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

        var assinatura = CriarAssinaturaBase(request.PlanoId, request.Gateway);
        assinatura.EstabelecimentoId = estabelecimentoId;

        return assinatura;
    }

    private async Task<Assinatura> CriarParaNovoEstabelecimentoAsync(
        IniciarAssinaturaRequestDto request,
        int userId,
        CancellationToken cancellationToken)
    {
        var estabelecimento = CriarEstabelecimento(request.Estabelecimento!);
        await _estabelecimentoRepository.AdicionarAsync(estabelecimento, cancellationToken);

        await _estabelecimentoUsuarioRepository.AdicionarAsync(new EstabelecimentoUsuario
        {
            Estabelecimento = estabelecimento,
            UsuarioId = userId,
            RoleNoEstabelecimento = EstablishmentUserRole.Owner,
            Ativo = true
        }, cancellationToken);

        var assinatura = CriarAssinaturaBase(request.PlanoId, request.Gateway);
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
            return await CriarParaNovoOuExistenteProfissionalAutonomoAsync(request, userId, cancellationToken);
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

        if (await _assinaturaRepository.ExisteAtivaOuPendentePorProfissionalAutonomoAsync(profissionalId, cancellationToken))
        {
            throw new AssinaturaDuplicadaException();
        }

        var assinatura = CriarAssinaturaBase(request.PlanoId, request.Gateway);
        assinatura.ProfissionalAutonomoId = profissionalId;

        return assinatura;
    }

    private async Task<Assinatura> CriarParaNovoOuExistenteProfissionalAutonomoAsync(
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
            if (await _assinaturaRepository.ExisteAtivaOuPendentePorProfissionalAutonomoAsync(profissional.Id, cancellationToken))
            {
                throw new AssinaturaDuplicadaException();
            }

            AtualizarProfissionalAutonomo(profissional, request.ProfissionalAutonomo!);
            _profissionalRepository.Atualizar(profissional);
        }

        var assinatura = CriarAssinaturaBase(request.PlanoId, request.Gateway);
        assinatura.ProfissionalAutonomo = profissional;

        return assinatura;
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

    private static Estabelecimento CriarEstabelecimento(CriarEstabelecimentoAssinaturaDto dto)
    {
        static Exception CriarExcecao(string mensagem) => new EstabelecimentoAssinaturaInvalidoException(mensagem);

        if (dto.Descricao.Length > 500)
        {
            throw new EstabelecimentoAssinaturaInvalidoException("Descricao do estabelecimento deve ter no maximo 500 caracteres.");
        }

        return new Estabelecimento
        {
            Nome = OperacaoPerfilValidation.ValidarTextoObrigatorio(dto.Nome, "Nome do estabelecimento", 150, CriarExcecao),
            Descricao = dto.Descricao.Trim(),
            Logo = OperacaoPerfilValidation.ValidarTextoObrigatorio(dto.Logo, "Logo do estabelecimento", 500, CriarExcecao),
            Telefone = OperacaoPerfilValidation.ValidarTextoObrigatorio(dto.Telefone, "Telefone do estabelecimento", 20, CriarExcecao),
            Email = OperacaoPerfilValidation.ValidarTextoObrigatorio(dto.Email, "Email do estabelecimento", 255, CriarExcecao),
            Ativo = true,
            Endereco = OperacaoPerfilValidation.CriarEndereco(dto.Endereco, CriarExcecao)
        };
    }

    private static Profissional CriarProfissionalAutonomo(
        CriarProfissionalAutonomoAssinaturaDto dto,
        int userId)
    {
        ValidarProfissionalAutonomo(dto);

        return new Profissional
        {
            UsuarioId = userId,
            NomePublico = dto.NomePublico.Trim(),
            Biografia = dto.Biografia.Trim(),
            Logo = dto.Logo.Trim(),
            Telefone = dto.Telefone.Trim(),
            Email = dto.Email.Trim(),
            TipoProfissional = ProfessionalType.Autonomo,
            Ativo = true,
            Endereco = OperacaoPerfilValidation.CriarEndereco(
                dto.Endereco,
                mensagem => new ProfissionalAutonomoAssinaturaInvalidoException(mensagem))
        };
    }

    private static void AtualizarProfissionalAutonomo(
        Profissional profissional,
        CriarProfissionalAutonomoAssinaturaDto dto)
    {
        ValidarProfissionalAutonomo(dto);

        profissional.NomePublico = dto.NomePublico.Trim();
        profissional.Biografia = dto.Biografia.Trim();
        profissional.Logo = dto.Logo.Trim();
        profissional.Telefone = dto.Telefone.Trim();
        profissional.Email = dto.Email.Trim();
        profissional.TipoProfissional = ProfessionalType.Autonomo;
        profissional.Ativo = true;
        profissional.UpdatedAt = DateTime.UtcNow;
        OperacaoPerfilValidation.AtualizarEndereco(
            profissional.Endereco,
            endereco => profissional.Endereco = endereco,
            dto.Endereco,
            mensagem => new ProfissionalAutonomoAssinaturaInvalidoException(mensagem));
    }

    private static void ValidarPerfilProfissionalAutonomoExistente(Profissional profissional)
    {
        if (profissional.TipoProfissional != ProfessionalType.Autonomo)
        {
            throw new ProfissionalAutonomoAssinaturaInvalidoException(
                "Usuario ja possui um perfil profissional que nao e autonomo.");
        }
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
            dto.Logo,
            "Logo do profissional",
            500,
            mensagem => new ProfissionalAutonomoAssinaturaInvalidoException(mensagem));

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

    private static Assinatura CriarAssinaturaBase(int planoId, GatewayPagamento gateway) =>
        new()
        {
            PlanoId = planoId,
            Status = AssinaturaStatus.PendentePagamento,
            Inicio = DateTime.UtcNow,
            Gateway = gateway,
            RenovacaoAutomatica = true
        };

    private async Task<PagamentoInicial> CriarPagamentoInicialAsync(
        Assinatura assinatura,
        Plano plano,
        PagamentoTransparenteMercadoPagoDto? pagamentoTransparente,
        CancellationToken cancellationToken)
    {
        var gateway = _gatewayPagamentoResolver.Resolver(assinatura.Gateway);
        var referenciaInterna = $"assinatura-{Guid.NewGuid():N}";
        var response = await gateway.CriarCobrancaAsync(new CriarCobrancaGatewayRequest(
            Gateway: assinatura.Gateway,
            ReferenciaInterna: referenciaInterna,
            Descricao: $"Assinatura {plano.Nome}",
            Valor: plano.Preco,
            Moeda: "BRL",
            PagadorNome: _currentUser.Email ?? "Usuario Glow",
            PagadorEmail: _currentUser.Email ?? string.Empty,
            Metadados: new Dictionary<string, string>
            {
                ["planoId"] = plano.Id.ToString(),
                ["tipo"] = assinatura.EstabelecimentoId.HasValue || assinatura.Estabelecimento is not null
                    ? TipoAssinatura.Estabelecimento.ToString()
                    : TipoAssinatura.ProfissionalAutonomo.ToString()
            },
            PagamentoTransparente: CriarPagamentoTransparenteRequest(pagamentoTransparente)),
            cancellationToken);

        if (!response.Sucesso)
        {
            throw new GatewayPagamentoException(response.MensagemErro ?? "Nao foi possivel criar a cobranca no gateway.");
        }

        var pagamento = new Pagamento
        {
            Assinatura = assinatura,
            Gateway = assinatura.Gateway,
            GatewayPaymentId = response.GatewayPaymentId,
            MetodoPagamento = response.MetodoPagamento,
            Status = PagamentoStatus.Pendente,
            Valor = plano.Preco,
            Moeda = "BRL"
        };

        return new PagamentoInicial(pagamento, response.CheckoutUrl, response.QrCode);
    }

    private async Task<PagamentoInicial> CriarPagamentoTrocaPlanoAsync(
        Assinatura assinatura,
        Plano novoPlano,
        GatewayPagamento gatewayPagamento,
        PagamentoTransparenteMercadoPagoDto? pagamentoTransparente,
        CancellationToken cancellationToken)
    {
        var gateway = _gatewayPagamentoResolver.Resolver(gatewayPagamento);
        var referenciaInterna = $"troca-plano-{assinatura.Id}-{Guid.NewGuid():N}";
        var response = await gateway.CriarCobrancaAsync(new CriarCobrancaGatewayRequest(
            Gateway: gatewayPagamento,
            ReferenciaInterna: referenciaInterna,
            Descricao: $"Troca de plano para {novoPlano.Nome}",
            Valor: novoPlano.Preco,
            Moeda: "BRL",
            PagadorNome: _currentUser.Email ?? "Usuario Glow",
            PagadorEmail: _currentUser.Email ?? string.Empty,
            Metadados: new Dictionary<string, string>
            {
                ["assinaturaId"] = assinatura.Id.ToString(),
                ["planoAtualId"] = assinatura.PlanoId.ToString(),
                ["novoPlanoId"] = novoPlano.Id.ToString(),
                ["acao"] = "TrocaPlano"
            },
            PagamentoTransparente: CriarPagamentoTransparenteRequest(pagamentoTransparente)),
            cancellationToken);

        if (!response.Sucesso)
        {
            throw new GatewayPagamentoException(response.MensagemErro ?? "Nao foi possivel criar a cobranca no gateway.");
        }

        var pagamento = new Pagamento
        {
            Assinatura = assinatura,
            AssinaturaId = assinatura.Id,
            Gateway = gatewayPagamento,
            GatewayPaymentId = response.GatewayPaymentId,
            MetodoPagamento = response.MetodoPagamento,
            Status = PagamentoStatus.Pendente,
            Valor = novoPlano.Preco,
            Moeda = "BRL"
        };

        return new PagamentoInicial(pagamento, response.CheckoutUrl, response.QrCode);
    }

    private static PagamentoTransparenteGatewayRequest? CriarPagamentoTransparenteRequest(
        PagamentoTransparenteMercadoPagoDto? pagamento)
    {
        if (pagamento is null)
        {
            return null;
        }

        return new PagamentoTransparenteGatewayRequest(
            pagamento.PaymentMethodId,
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

        if (assinatura.ProfissionalAutonomoId.HasValue)
        {
            var profissional = await _profissionalRepository.ObterPorIdAsync(
                assinatura.ProfissionalAutonomoId.Value,
                cancellationToken);

            if (profissional is null || profissional.UsuarioId != userId)
            {
                throw new UsuarioSemPermissaoAssinaturaException();
            }

            return;
        }

        throw new AssinaturaTitularInvalidoException();
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

    private static bool TrocaExigeCobranca(Plano? planoAtual, Plano novoPlano)
    {
        if (planoAtual is null)
        {
            return true;
        }

        return planoAtual.Preco != novoPlano.Preco || planoAtual.Periodo != novoPlano.Periodo;
    }

    private int ObterUserIdAutenticado()
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedException();
        }

        return _currentUser.UserId.Value;
    }

    private record PagamentoInicial(Pagamento Pagamento, string CheckoutUrl, string QrCode);
}
