using GLOWAPI.Application.DTOs.Convites;
using GLOWAPI.Application.DTOs.Equipe;
using GLOWAPI.Application.DTOs.Mensageria;
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
    private static readonly HashSet<EstablishmentUserRole> RolesConviteEquipe =
    [
        EstablishmentUserRole.Admin,
        EstablishmentUserRole.Manager,
        EstablishmentUserRole.Receptionist
    ];

    private readonly IConviteNegocioRepository _conviteRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IModulosAssinaturaService _modulosAssinaturaService;
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;
    private readonly IEquipeNegocioService _equipeNegocioService;
    private readonly IAuditoriaNegocioService _auditoriaNegocioService;
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
        IMensagemNotificacaoService mensagemNotificacaoService,
        IEquipeNegocioService equipeNegocioService,
        IAuditoriaNegocioService auditoriaNegocioService,
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
        _mensagemNotificacaoService = mensagemNotificacaoService;
        _equipeNegocioService = equipeNegocioService;
        _auditoriaNegocioService = auditoriaNegocioService;
        _tokenService = tokenService;
        _currentUserContext = currentUserContext;
        _authOptions = authOptions.Value;
    }

    public async Task<ConviteOuVinculoResponseDto> CriarConviteProfissionalAsync(
        int estabelecimentoId,
        CriarConviteProfissionalRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ProfissionalConvidar,
            cancellationToken);

        var email = NormalizarEmail(request.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ConviteNegocioInvalidoException("E-mail do convite e obrigatorio.");
        }

        var vinculoDireto = await TentarVincularProfissionalExistenteAsync(
            estabelecimentoId,
            email,
            request,
            cancellationToken);
        if (vinculoDireto is not null)
        {
            return vinculoDireto;
        }

        await ValidarConvitePendenteDuplicadoAsync(
            estabelecimentoId,
            email,
            TipoConviteNegocio.Profissional,
            cancellationToken);

        var token = _tokenService.GerarRefreshToken();
        var convite = new ConviteNegocio
        {
            EstabelecimentoId = estabelecimentoId,
            Email = email,
            Telefone = request.Telefone?.Trim() ?? string.Empty,
            NomePublico = request.NomePublico?.Trim() ?? string.Empty,
            TipoConvite = TipoConviteNegocio.Profissional,
            RoleSugerida = EstablishmentUserRole.Profissional,
            PodeReceberAgendamento = request.PodeReceberAgendamento,
            Status = StatusConviteNegocio.Pendente,
            TokenHash = _tokenService.HashToken(token),
            ExpiraEm = DateTime.UtcNow.AddDays(7),
            CriadoPorUsuarioId = ObterUsuarioAutenticado()
        };

        var conviteCriado = await PersistirConviteAsync(
            convite,
            token,
            TipoAcaoAuditoriaNegocio.ProfissionalConvidado,
            "Voce recebeu um convite para atuar como profissional.",
            cancellationToken);

        return ConviteOuVinculoResponseDto.FromConvite(conviteCriado);
    }

    public async Task<ConviteOuVinculoResponseDto> CriarConviteUsuarioEquipeAsync(
        int estabelecimentoId,
        CriarConviteUsuarioEquipeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.EquipeGerenciar,
            cancellationToken);

        if (!RolesConviteEquipe.Contains(request.Role))
        {
            throw new ConviteNegocioInvalidoException(
                "Role invalida para convite de usuario da equipe.");
        }

        var email = NormalizarEmail(request.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ConviteNegocioInvalidoException("E-mail do convite e obrigatorio.");
        }

        var vinculoDireto = await TentarVincularUsuarioEquipeExistenteAsync(
            estabelecimentoId,
            email,
            request.Role,
            cancellationToken);
        if (vinculoDireto is not null)
        {
            return vinculoDireto;
        }

        await ValidarConvitePendenteDuplicadoAsync(
            estabelecimentoId,
            email,
            TipoConviteNegocio.UsuarioEquipe,
            cancellationToken);

        var token = _tokenService.GerarRefreshToken();
        var convite = new ConviteNegocio
        {
            EstabelecimentoId = estabelecimentoId,
            Email = email,
            TipoConvite = TipoConviteNegocio.UsuarioEquipe,
            RoleSugerida = request.Role,
            Status = StatusConviteNegocio.Pendente,
            TokenHash = _tokenService.HashToken(token),
            ExpiraEm = DateTime.UtcNow.AddDays(7),
            CriadoPorUsuarioId = ObterUsuarioAutenticado()
        };

        var conviteCriado = await PersistirConviteAsync(
            convite,
            token,
            TipoAcaoAuditoriaNegocio.UsuarioEquipeConvidado,
            $"Voce recebeu um convite para integrar a equipe como {request.Role}.",
            cancellationToken);

        return ConviteOuVinculoResponseDto.FromConvite(conviteCriado);
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

        return convites
            .Select(ConviteNegocioResponseDto.From)
            .ToList();
    }

    public async Task<ConviteNegocioResponseDto> AceitarAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioAtualAsync(cancellationToken);
        var convite = await ObterConviteValidoAsync(token, cancellationToken);
        ValidarDestinatario(convite, usuario);

        convite.Status = StatusConviteNegocio.Aceito;
        convite.AceitoPorUsuarioId = usuario.Id;
        convite.RespondidoEm = DateTime.UtcNow;
        convite.UpdatedAt = DateTime.UtcNow;

        if (convite.TipoConvite == TipoConviteNegocio.UsuarioEquipe)
        {
            await GarantirVinculosUsuarioEquipeAsync(convite, usuario, cancellationToken);
            await AuditarAsync(convite, TipoAcaoAuditoriaNegocio.UsuarioEquipeConvidado, cancellationToken);
        }
        else
        {
            await GarantirVinculosProfissionalAsync(convite, usuario, cancellationToken);
            await AuditarAsync(convite, TipoAcaoAuditoriaNegocio.ProfissionalConvidado, cancellationToken);
        }

        _conviteRepository.Atualizar(convite);
        await _conviteRepository.SalvarAlteracoesAsync(cancellationToken);

        return ConviteNegocioResponseDto.From(convite);
    }

    public async Task<ConviteNegocioResponseDto> RejeitarAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var usuario = await ObterUsuarioAtualAsync(cancellationToken);
        var convite = await ObterConviteValidoAsync(token, cancellationToken);
        ValidarDestinatario(convite, usuario);

        convite.Status = StatusConviteNegocio.Rejeitado;
        convite.AceitoPorUsuarioId = usuario.Id;
        convite.RespondidoEm = DateTime.UtcNow;
        convite.UpdatedAt = DateTime.UtcNow;

        _conviteRepository.Atualizar(convite);
        await _conviteRepository.SalvarAlteracoesAsync(cancellationToken);

        return ConviteNegocioResponseDto.From(convite);
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

        if (convite.Status != StatusConviteNegocio.Pendente)
        {
            throw new ConviteNegocioInvalidoException("Somente convites pendentes podem ser cancelados.");
        }

        convite.Status = StatusConviteNegocio.Cancelado;
        convite.UpdatedAt = DateTime.UtcNow;

        _conviteRepository.Atualizar(convite);
        await _conviteRepository.SalvarAlteracoesAsync(cancellationToken);

        return ConviteNegocioResponseDto.From(convite);
    }

    private async Task<ConviteNegocioCriadoResponseDto> PersistirConviteAsync(
        ConviteNegocio convite,
        string token,
        TipoAcaoAuditoriaNegocio tipoAuditoria,
        string mensagemEmail,
        CancellationToken cancellationToken)
    {
        await _conviteRepository.AdicionarAsync(convite, cancellationToken);
        await _conviteRepository.SalvarAlteracoesAsync(cancellationToken);

        var link = MontarLinkConvite(token);
        await EnfileirarConviteAsync(convite, link, mensagemEmail, cancellationToken);
        await AuditarAsync(convite, tipoAuditoria, cancellationToken);

        return ConviteNegocioCriadoResponseDto.From(convite, link);
    }

    private async Task ValidarConvitePendenteDuplicadoAsync(
        int estabelecimentoId,
        string email,
        TipoConviteNegocio tipoConvite,
        CancellationToken cancellationToken)
    {
        var existente = await _conviteRepository.ObterPendentePorDestinatarioAsync(
            estabelecimentoId,
            email,
            tipoConvite,
            cancellationToken);

        if (existente is not null && existente.ExpiraEm > DateTime.UtcNow)
        {
            throw new ConviteNegocioDuplicadoException();
        }
    }

    private async Task<ConviteNegocio> ObterConviteValidoAsync(
        string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ConviteNegocioNaoEncontradoException();
        }

        var hash = _tokenService.HashToken(token.Trim());
        var convite = await _conviteRepository.ObterPorTokenHashAsync(hash, cancellationToken);
        if (convite is null)
        {
            throw new ConviteNegocioNaoEncontradoException();
        }

        if (convite.Status != StatusConviteNegocio.Pendente)
        {
            throw new ConviteNegocioInvalidoException("Convite ja foi respondido ou cancelado.");
        }

        if (convite.ExpiraEm <= DateTime.UtcNow)
        {
            convite.Status = StatusConviteNegocio.Expirado;
            convite.UpdatedAt = DateTime.UtcNow;
            _conviteRepository.Atualizar(convite);
            await _conviteRepository.SalvarAlteracoesAsync(cancellationToken);
            throw new ConviteNegocioInvalidoException("Convite expirado.");
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
                vinculoUsuario.RoleNoEstabelecimento = convite.RoleSugerida;
            }

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

    private string MontarLinkConvite(string token) =>
        $"{_authOptions.FrontendBaseUrl.TrimEnd('/')}/convites/{Uri.EscapeDataString(token)}";

    private async Task EnfileirarConviteAsync(
        ConviteNegocio convite,
        string link,
        string mensagem,
        CancellationToken cancellationToken)
    {
        await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.Email,
            Destinatario = convite.Email,
            Assunto = "Convite para integrar a equipe",
            Conteudo = $"{mensagem} Acesse: {link}",
            EstabelecimentoId = convite.EstabelecimentoId,
            Prioridade = 2
        }, cancellationToken);
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
                convite.Email,
                convite.TipoConvite,
                convite.Status
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

    private static void ValidarDestinatario(ConviteNegocio convite, Usuario usuario)
    {
        if (!string.Equals(convite.Email, usuario.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConviteNegocioInvalidoException("Convite pertence a outro destinatario.");
        }
    }

    private async Task<ConviteOuVinculoResponseDto?> TentarVincularProfissionalExistenteAsync(
        int estabelecimentoId,
        string email,
        CriarConviteProfissionalRequestDto request,
        CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepository.ObterPorEmailAsync(email, cancellationToken);
        if (usuario is null)
        {
            return null;
        }

        ValidarUsuarioParaVinculoDireto(usuario);

        var vinculo = await _equipeNegocioService.ConvidarProfissionalAsync(
            estabelecimentoId,
            new ConvidarProfissionalEquipeRequestDto
            {
                Email = email,
                Telefone = request.Telefone,
                NomePublico = request.NomePublico,
                PodeReceberAgendamento = request.PodeReceberAgendamento
            },
            cancellationToken);

        return ConviteOuVinculoResponseDto.FromVinculoProfissional(vinculo);
    }

    private async Task<ConviteOuVinculoResponseDto?> TentarVincularUsuarioEquipeExistenteAsync(
        int estabelecimentoId,
        string email,
        EstablishmentUserRole role,
        CancellationToken cancellationToken)
    {
        var usuario = await _usuarioRepository.ObterPorEmailAsync(email, cancellationToken);
        if (usuario is null)
        {
            return null;
        }

        ValidarUsuarioParaVinculoDireto(usuario);

        var vinculo = await _equipeNegocioService.CadastrarUsuarioAsync(
            estabelecimentoId,
            new CadastrarUsuarioEquipeRequestDto
            {
                Email = email,
                Role = role
            },
            cancellationToken);

        return ConviteOuVinculoResponseDto.FromVinculoUsuario(vinculo);
    }

    private static void ValidarUsuarioParaVinculoDireto(Usuario usuario)
    {
        if (!usuario.Ativo || usuario.PendenteConfirmacaoEmail())
        {
            throw new ConviteUsuarioNaoConfirmadoException();
        }
    }

    private static string NormalizarEmail(string email) =>
        email.Trim().ToLowerInvariant();
}
