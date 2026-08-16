using GLOWAPI.Application.DTOs.Convites;
using GLOWAPI.Application.DTOs.Usuario;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Domain.Exceptions.Negocios;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class ConviteNegocioService : IConviteNegocioService
{
    private static readonly HashSet<EstablishmentUserRole> RolesPermitidas =
    [
        EstablishmentUserRole.Admin,
        EstablishmentUserRole.Manager,
        EstablishmentUserRole.Receptionist,
        EstablishmentUserRole.Profissional
    ];

    private static readonly HashSet<int> LimitesPermitidos = [1, 2, 5, 10];

    private readonly IConviteNegocioRepository _conviteRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IModulosAssinaturaService _modulosAssinaturaService;
    private readonly IAuditoriaNegocioService _auditoriaNegocioService;
    private readonly IUsuarioService _usuarioService;
    private readonly IGlowTokenService _tokenService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly AuthOptions _authOptions;

    public ConviteNegocioService(
        IConviteNegocioRepository conviteRepository,
        IUsuarioRepository usuarioRepository,
        IProfissionalRepository profissionalRepository,
        IEstabelecimentoUsuarioRepository estabelecimentoUsuarioRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IModulosAssinaturaService modulosAssinaturaService,
        IAuditoriaNegocioService auditoriaNegocioService,
        IUsuarioService usuarioService,
        IGlowTokenService tokenService,
        ICurrentUserContext currentUserContext,
        IOptions<AuthOptions> authOptions)
    {
        _conviteRepository = conviteRepository;
        _usuarioRepository = usuarioRepository;
        _profissionalRepository = profissionalRepository;
        _estabelecimentoUsuarioRepository = estabelecimentoUsuarioRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _modulosAssinaturaService = modulosAssinaturaService;
        _auditoriaNegocioService = auditoriaNegocioService;
        _usuarioService = usuarioService;
        _tokenService = tokenService;
        _currentUserContext = currentUserContext;
        _authOptions = authOptions.Value;
    }

    public async Task<ConviteNegocioCriadoResponseDto> CriarLinkAsync(
        int estabelecimentoId,
        CriarConviteLinkRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.EquipeGerenciar,
            cancellationToken);

        if (!RolesPermitidas.Contains(request.Role))
        {
            throw new ConviteNegocioInvalidoException("Role invalida para o convite.");
        }

        if (request.Role == EstablishmentUserRole.Profissional)
        {
            await _autorizacaoNegocioService.AutorizarAsync(
                estabelecimentoId,
                PermissaoNegocio.ProfissionalConvidar,
                cancellationToken);
        }

        if (!LimitesPermitidos.Contains(request.LimiteUsuarios))
        {
            throw new ConviteNegocioInvalidoException(
                "Quantidade maxima de usuarios deve ser 1, 2, 5 ou 10.");
        }

        var expiraEm = CalcularExpiracao(request.DuracaoValor, request.DuracaoUnidade);
        // UUID público no link; no banco persiste apenas o hash (mesmo padrão dos demais tokens).
        var token = Guid.NewGuid().ToString("D");
        var tipoConvite = request.Role == EstablishmentUserRole.Profissional
            ? TipoConviteNegocio.Profissional
            : TipoConviteNegocio.UsuarioEquipe;

        var convite = new ConviteNegocio
        {
            EstabelecimentoId = estabelecimentoId,
            Email = string.Empty,
            TipoConvite = tipoConvite,
            RoleSugerida = request.Role,
            PodeReceberAgendamento = request.Role == EstablishmentUserRole.Profissional
                && request.PodeReceberAgendamento,
            Status = StatusConviteNegocio.Ativo,
            TokenHash = _tokenService.HashToken(token),
            LimiteUsuarios = request.LimiteUsuarios,
            QuantidadeUtilizacoes = 0,
            ExpiraEm = expiraEm,
            CriadoPorUsuarioId = ObterUsuarioAutenticado()
        };

        await _conviteRepository.AdicionarAsync(convite, cancellationToken);
        await _conviteRepository.SalvarAlteracoesAsync(cancellationToken);

        var link = MontarLinkConvite(token);
        await AuditarAsync(
            convite,
            TipoAcaoAuditoriaNegocio.ConviteLinkCriado,
            cancellationToken);

        return ConviteNegocioCriadoResponseDto.From(convite, link);
    }

    public async Task<ConviteNegocioPreviewResponseDto> ObterPreviewAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var convite = await ObterConviteValidoAsync(token, cancellationToken);
        return ConviteNegocioPreviewResponseDto.From(convite);
    }

    public async Task<IReadOnlyList<ConviteNegocioResponseDto>> ListarAsync(
        int estabelecimentoId,
        ConviteNegocioFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.EquipeGerenciar,
            cancellationToken);

        var convites = await _conviteRepository.ListarPorEstabelecimentoAsync(
            estabelecimentoId,
            filtro.Status,
            cancellationToken);

        return convites.Select(ConviteNegocioResponseDto.From).ToList();
    }

    public async Task<ConviteNegocioResponseDto> AceitarAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioAtualAsync(cancellationToken);
        return await AceitarParaUsuarioAsync(token, usuario, cancellationToken);
    }

    public async Task<ConviteNegocioResponseDto> AceitarComCadastroAsync(
        string token,
        CadastrarClienteDto cadastro,
        CancellationToken cancellationToken = default)
    {
        // Valida o convite antes de criar a conta (não consome vaga no preview).
        await ObterConviteValidoAsync(token, cancellationToken);

        var usuario = await _usuarioService.CadastrarClienteAsync(cadastro, cancellationToken);
        return await AceitarParaUsuarioAsync(token, usuario, cancellationToken);
    }

    public async Task<ConviteNegocioResponseDto> CancelarAsync(
        int estabelecimentoId,
        int conviteId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.EquipeGerenciar,
            cancellationToken);

        var convite = await _conviteRepository.ObterPorIdAsync(conviteId, cancellationToken);
        if (convite is null || convite.EstabelecimentoId != estabelecimentoId)
        {
            throw new ConviteNegocioNaoEncontradoException();
        }

        if (convite.Status != StatusConviteNegocio.Ativo)
        {
            throw new ConviteNegocioInvalidoException("Somente convites ativos podem ser cancelados.");
        }

        convite.Status = StatusConviteNegocio.Cancelado;
        convite.UpdatedAt = DateTime.UtcNow;

        _conviteRepository.Atualizar(convite);
        await _conviteRepository.SalvarAlteracoesAsync(cancellationToken);
        await AuditarAsync(convite, TipoAcaoAuditoriaNegocio.ConviteLinkCancelado, cancellationToken);

        return ConviteNegocioResponseDto.From(convite);
    }

    private async Task<ConviteNegocioResponseDto> AceitarParaUsuarioAsync(
        string token,
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        var convite = await ObterConviteValidoAsync(token, cancellationToken);

        if (await _conviteRepository.UsuarioJaUtilizouAsync(convite.Id, usuario.Id, cancellationToken))
        {
            throw new ConviteNegocioInvalidoException("Voce ja utilizou este convite.");
        }

        var vinculoExistente = await _estabelecimentoUsuarioRepository.ObterPorUsuarioAsync(
            convite.EstabelecimentoId,
            usuario.Id,
            cancellationToken);

        if (vinculoExistente is { Ativo: true })
        {
            throw new UsuarioEquipeNegocioDuplicadoException();
        }

        var agora = DateTime.UtcNow;
        var reservou = await _conviteRepository.TentarRegistrarUtilizacaoAsync(
            convite.Id,
            agora,
            cancellationToken);

        if (!reservou)
        {
            throw new ConviteNegocioIndisponivelException();
        }

        convite.QuantidadeUtilizacoes += 1;
        if (convite.QuantidadeUtilizacoes >= convite.LimiteUsuarios)
        {
            convite.Status = StatusConviteNegocio.Esgotado;
        }
        convite.UpdatedAt = agora;

        try
        {
            if (convite.TipoConvite == TipoConviteNegocio.UsuarioEquipe)
            {
                await GarantirVinculosUsuarioEquipeAsync(convite, usuario, cancellationToken);
            }
            else
            {
                await GarantirVinculosProfissionalAsync(convite, usuario, cancellationToken);
            }

            await _conviteRepository.AdicionarUtilizacaoAsync(
                new ConviteNegocioUtilizacao
                {
                    ConviteNegocioId = convite.Id,
                    UsuarioId = usuario.Id,
                    UtilizadoEm = agora
                },
                cancellationToken);

            await _conviteRepository.SalvarAlteracoesAsync(cancellationToken);
        }
        catch
        {
            await _conviteRepository.CompensarUtilizacaoAsync(convite.Id, agora, cancellationToken);
            throw;
        }

        await AuditarAsync(convite, TipoAcaoAuditoriaNegocio.ConviteLinkAceito, cancellationToken);

        if (convite.TipoConvite == TipoConviteNegocio.UsuarioEquipe)
        {
            await AuditarAsync(convite, TipoAcaoAuditoriaNegocio.UsuarioEquipeConvidado, cancellationToken);
        }
        else
        {
            await AuditarAsync(convite, TipoAcaoAuditoriaNegocio.ProfissionalConvidado, cancellationToken);
        }

        return ConviteNegocioResponseDto.From(convite);
    }

    private async Task<ConviteNegocio> ObterConviteValidoAsync(
        string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)
            || !Guid.TryParse(token.Trim(), out var tokenGuid))
        {
            throw new ConviteNegocioNaoEncontradoException();
        }

        var hash = _tokenService.HashToken(tokenGuid.ToString("D"));
        var convite = await _conviteRepository.ObterPorTokenHashAsync(hash, cancellationToken);
        if (convite is null)
        {
            throw new ConviteNegocioNaoEncontradoException();
        }

        if (convite.Status is StatusConviteNegocio.Cancelado or StatusConviteNegocio.Esgotado)
        {
            throw new ConviteNegocioIndisponivelException();
        }

        if (convite.Status != StatusConviteNegocio.Ativo)
        {
            throw new ConviteNegocioIndisponivelException();
        }

        if (convite.ExpiraEm <= DateTime.UtcNow)
        {
            convite.Status = StatusConviteNegocio.Expirado;
            convite.UpdatedAt = DateTime.UtcNow;
            _conviteRepository.Atualizar(convite);
            await _conviteRepository.SalvarAlteracoesAsync(cancellationToken);
            throw new ConviteNegocioIndisponivelException();
        }

        if (convite.QuantidadeUtilizacoes >= convite.LimiteUsuarios)
        {
            convite.Status = StatusConviteNegocio.Esgotado;
            convite.UpdatedAt = DateTime.UtcNow;
            _conviteRepository.Atualizar(convite);
            await _conviteRepository.SalvarAlteracoesAsync(cancellationToken);
            throw new ConviteNegocioIndisponivelException();
        }

        return convite;
    }

    private async Task GarantirVinculosUsuarioEquipeAsync(
        ConviteNegocio convite,
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        var vinculoUsuario = await _estabelecimentoUsuarioRepository.ObterPorUsuarioAsync(
            convite.EstabelecimentoId,
            usuario.Id,
            cancellationToken);

        if (vinculoUsuario is null)
        {
            await ValidarLimiteUsuariosAsync(convite.EstabelecimentoId, cancellationToken);
            await _estabelecimentoUsuarioRepository.AdicionarAsync(new EstabelecimentoUsuario
            {
                EstabelecimentoId = convite.EstabelecimentoId,
                UsuarioId = usuario.Id,
                RoleNoEstabelecimento = convite.RoleSugerida,
                Ativo = true
            }, cancellationToken);
            return;
        }

        if (!vinculoUsuario.Ativo)
        {
            await ValidarLimiteUsuariosAsync(convite.EstabelecimentoId, cancellationToken);
        }

        vinculoUsuario.RoleNoEstabelecimento = convite.RoleSugerida;
        vinculoUsuario.Ativo = true;
        vinculoUsuario.UpdatedAt = DateTime.UtcNow;
        _estabelecimentoUsuarioRepository.Atualizar(vinculoUsuario);
    }

    private async Task GarantirVinculosProfissionalAsync(
        ConviteNegocio convite,
        Usuario usuario,
        CancellationToken cancellationToken)
    {
        var vinculoUsuario = await _estabelecimentoUsuarioRepository.ObterPorUsuarioAsync(
            convite.EstabelecimentoId,
            usuario.Id,
            cancellationToken);

        if (vinculoUsuario is null)
        {
            await ValidarLimiteUsuariosAsync(convite.EstabelecimentoId, cancellationToken);
            await _estabelecimentoUsuarioRepository.AdicionarAsync(new EstabelecimentoUsuario
            {
                EstabelecimentoId = convite.EstabelecimentoId,
                UsuarioId = usuario.Id,
                RoleNoEstabelecimento = convite.RoleSugerida,
                Ativo = true
            }, cancellationToken);
        }
        else
        {
            if (!vinculoUsuario.Ativo)
            {
                await ValidarLimiteUsuariosAsync(convite.EstabelecimentoId, cancellationToken);
            }

            vinculoUsuario.RoleNoEstabelecimento = convite.RoleSugerida;
            vinculoUsuario.Ativo = true;
            vinculoUsuario.UpdatedAt = DateTime.UtcNow;
            _estabelecimentoUsuarioRepository.Atualizar(vinculoUsuario);
        }

        var profissional = await _profissionalRepository.ObterPorUsuarioIdAsync(usuario.Id, cancellationToken);
        var limiteProfissionalValidado = false;
        if (profissional is null)
        {
            await ValidarLimiteProfissionaisAsync(convite.EstabelecimentoId, cancellationToken);
            limiteProfissionalValidado = true;

            profissional = new Profissional
            {
                UsuarioId = usuario.Id,
                NomePublico = string.IsNullOrWhiteSpace(convite.NomePublico) ? usuario.Nome : convite.NomePublico,
                Email = usuario.Email,
                Telefone = usuario.Telefone,
                TipoProfissional = ProfessionalType.VinculadoEstabelecimento,
                Ativo = true
            };

            await _profissionalRepository.AdicionarAsync(profissional, cancellationToken);
            await _profissionalRepository.SalvarAlteracoesAsync(cancellationToken);
        }
        else if (!profissional.Ativo)
        {
            profissional.Ativo = true;
            profissional.TipoProfissional = ProfessionalType.VinculadoEstabelecimento;
            profissional.UpdatedAt = DateTime.UtcNow;
            _profissionalRepository.Atualizar(profissional);
        }

        var vinculoProfissional = await _profissionalEstabelecimentoRepository.ObterPorProfissionalAsync(
            profissional.Id,
            convite.EstabelecimentoId,
            cancellationToken);
        if (vinculoProfissional is null)
        {
            if (!limiteProfissionalValidado)
            {
                await ValidarLimiteProfissionaisAsync(convite.EstabelecimentoId, cancellationToken);
            }

            await _profissionalEstabelecimentoRepository.AdicionarAsync(new ProfissionalEstabelecimento
            {
                EstabelecimentoId = convite.EstabelecimentoId,
                ProfissionalId = profissional.Id,
                Ativo = true,
                PodeReceberAgendamento = convite.PodeReceberAgendamento
            }, cancellationToken);
        }
        else
        {
            if (!vinculoProfissional.Ativo)
            {
                await ValidarLimiteProfissionaisAsync(convite.EstabelecimentoId, cancellationToken);
            }

            vinculoProfissional.Ativo = true;
            vinculoProfissional.PodeReceberAgendamento = convite.PodeReceberAgendamento;
            vinculoProfissional.DataSaida = null;
            vinculoProfissional.UpdatedAt = DateTime.UtcNow;
            _profissionalEstabelecimentoRepository.Atualizar(vinculoProfissional);
        }
    }

    private async Task ValidarLimiteUsuariosAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var modulos = await _modulosAssinaturaService.ObterPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        var limiteUsuarios = modulos.Limites.Usuarios;
        if (!limiteUsuarios.HasValue)
        {
            return;
        }

        var usuariosAtivos = await _estabelecimentoUsuarioRepository.ContarAtivosAsync(
            estabelecimentoId,
            cancellationToken);

        if (usuariosAtivos >= limiteUsuarios.Value)
        {
            throw new LimiteUsuariosNegocioExcedidoException();
        }
    }

    private async Task ValidarLimiteProfissionaisAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var modulos = await _modulosAssinaturaService.ObterPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        var limiteProfissionais = modulos.Limites.Profissionais;
        if (!limiteProfissionais.HasValue)
        {
            return;
        }

        var profissionaisAtivos = await _profissionalEstabelecimentoRepository.ContarAtivosPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        if (profissionaisAtivos >= limiteProfissionais.Value)
        {
            throw new LimiteProfissionaisNegocioExcedidoException();
        }
    }

    private static DateTime CalcularExpiracao(int? valor, UnidadeDuracaoConvite? unidade)
    {
        if (!valor.HasValue || valor.Value <= 0 || !unidade.HasValue)
        {
            return DateTime.UtcNow.AddDays(1);
        }

        return unidade.Value switch
        {
            UnidadeDuracaoConvite.Minutos => DateTime.UtcNow.AddMinutes(valor.Value),
            UnidadeDuracaoConvite.Horas => DateTime.UtcNow.AddHours(valor.Value),
            UnidadeDuracaoConvite.Dias => DateTime.UtcNow.AddDays(valor.Value),
            _ => DateTime.UtcNow.AddDays(1)
        };
    }

    private string MontarLinkConvite(string token)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_authOptions.LandingBaseUrl)
            ? _authOptions.FrontendBaseUrl
            : _authOptions.LandingBaseUrl;

        // UUID não precisa de escape, mas mantém consistência com demais rotas.
        return $"{baseUrl.TrimEnd('/')}/convite/{token}";
    }

    private Task AuditarAsync(
        ConviteNegocio convite,
        TipoAcaoAuditoriaNegocio tipoAcao,
        CancellationToken cancellationToken)
    {
        return _auditoriaNegocioService.RegistrarAsync(
            convite.EstabelecimentoId,
            tipoAcao,
            nameof(ConviteNegocio),
            convite.Id,
            new
            {
                convite.RoleSugerida,
                convite.LimiteUsuarios,
                convite.QuantidadeUtilizacoes,
                convite.Status,
                convite.ExpiraEm
            },
            cancellationToken);
    }

    private async Task<Usuario> ObterUsuarioAtualAsync(CancellationToken cancellationToken)
    {
        var usuarioId = ObterUsuarioAutenticado();
        return await _usuarioRepository.ObterPorIdAsync(usuarioId, cancellationToken)
            ?? throw new UnauthorizedException();
    }

    private int ObterUsuarioAutenticado()
    {
        if (!_currentUserContext.IsAuthenticated || !_currentUserContext.UserId.HasValue)
        {
            throw new UnauthorizedException();
        }

        return _currentUserContext.UserId.Value;
    }
}
