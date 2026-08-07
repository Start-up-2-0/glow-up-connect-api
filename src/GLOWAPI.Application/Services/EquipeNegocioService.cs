using GLOWAPI.Application.DTOs.Equipe;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;
using GLOWAPI.Domain.Exceptions.Usuario;

namespace GLOWAPI.Application.Services;

public class EquipeNegocioService : IEquipeNegocioService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IAgendamentoItemRepository _agendamentoItemRepository;
    private readonly IAgendamentoNegocioService _agendamentoNegocioService;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IModulosAssinaturaService _modulosAssinaturaService;
    private readonly IAuditoriaNegocioService _auditoriaNegocioService;
    private readonly IEquipeNotificacaoService _equipeNotificacaoService;
    private readonly IAvatarBase64Decoder _avatarBase64Decoder;
    private readonly IBase64ImageThumbnailer _thumbnailer;
    private readonly IConviteNegocioRepository _conviteRepository;

    public EquipeNegocioService(
        IUsuarioRepository usuarioRepository,
        IProfissionalRepository profissionalRepository,
        IEstabelecimentoUsuarioRepository estabelecimentoUsuarioRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IAgendamentoItemRepository agendamentoItemRepository,
        IAgendamentoNegocioService agendamentoNegocioService,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IModulosAssinaturaService modulosAssinaturaService,
        IAuditoriaNegocioService auditoriaNegocioService,
        IEquipeNotificacaoService equipeNotificacaoService,
        IAvatarBase64Decoder avatarBase64Decoder,
        IBase64ImageThumbnailer thumbnailer,
        IConviteNegocioRepository conviteRepository)
    {
        _usuarioRepository = usuarioRepository;
        _profissionalRepository = profissionalRepository;
        _estabelecimentoUsuarioRepository = estabelecimentoUsuarioRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _agendamentoItemRepository = agendamentoItemRepository;
        _agendamentoNegocioService = agendamentoNegocioService;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _modulosAssinaturaService = modulosAssinaturaService;
        _auditoriaNegocioService = auditoriaNegocioService;
        _equipeNotificacaoService = equipeNotificacaoService;
        _avatarBase64Decoder = avatarBase64Decoder;
        _thumbnailer = thumbnailer;
        _conviteRepository = conviteRepository;
    }

    public async Task<UsuarioEquipeResponseDto> CadastrarUsuarioAsync(
        int estabelecimentoId,
        CadastrarUsuarioEquipeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.EquipeGerenciar,
            cancellationToken);

        var usuario = await ObterUsuarioAsync(request, cancellationToken);
        if (usuario is null || !usuario.Ativo)
        {
            throw new UsuarioEquipeNegocioNaoEncontradoException();
        }

        var vinculoExistente = await _estabelecimentoUsuarioRepository.ObterPorUsuarioAsync(
            estabelecimentoId,
            usuario.Id,
            cancellationToken);

        if (vinculoExistente?.Ativo == true)
        {
            throw new UsuarioEquipeNegocioDuplicadoException();
        }

        await ValidarLimiteUsuariosAsync(estabelecimentoId, cancellationToken);

        if (vinculoExistente is not null)
        {
            vinculoExistente.RoleNoEstabelecimento = request.Role;
            vinculoExistente.Ativo = true;
            vinculoExistente.UpdatedAt = DateTime.UtcNow;

            _estabelecimentoUsuarioRepository.Atualizar(vinculoExistente);
            await _estabelecimentoUsuarioRepository.SalvarAlteracoesAsync(cancellationToken);
            await AuditarUsuarioEquipeAsync(
                estabelecimentoId,
                TipoAcaoAuditoriaNegocio.UsuarioEquipeConvidado,
                vinculoExistente,
                request.Role,
                "reativado",
                cancellationToken);
            await _equipeNotificacaoService.UsuarioEquipeConvidadoAsync(
                vinculoExistente,
                usuario,
                cancellationToken);

            return UsuarioEquipeResponseDto.From(vinculoExistente, usuario);
        }

        var vinculo = new EstabelecimentoUsuario
        {
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = usuario.Id,
            RoleNoEstabelecimento = request.Role,
            Ativo = true
        };

        await _estabelecimentoUsuarioRepository.AdicionarAsync(vinculo, cancellationToken);
        await _estabelecimentoUsuarioRepository.SalvarAlteracoesAsync(cancellationToken);
        await AuditarUsuarioEquipeAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.UsuarioEquipeConvidado,
            vinculo,
            request.Role,
            "criado",
            cancellationToken);
        await _equipeNotificacaoService.UsuarioEquipeConvidadoAsync(
            vinculo,
            usuario,
            cancellationToken);

        return UsuarioEquipeResponseDto.From(vinculo, usuario);
    }

    public async Task<ProfissionalEquipeResponseDto> ConvidarProfissionalAsync(
        int estabelecimentoId,
        ConvidarProfissionalEquipeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ProfissionalConvidar,
            cancellationToken);

        var usuario = await ObterUsuarioAsync(request.Email, request.Telefone, cancellationToken);
        if (usuario is null || !usuario.Ativo)
        {
            throw new UsuarioEquipeNegocioNaoEncontradoException();
        }

        var profissional = await ObterOuCriarProfissionalAsync(usuario, request, cancellationToken);
        var vinculoExistente = await _profissionalEstabelecimentoRepository.ObterPorProfissionalAsync(
            profissional.Id,
            estabelecimentoId,
            cancellationToken);

        if (vinculoExistente?.Ativo == true)
        {
            throw new ProfissionalNegocioDuplicadoException();
        }

        await ValidarLimiteProfissionaisAsync(estabelecimentoId, cancellationToken);
        await GarantirAcessoProfissionalAsync(estabelecimentoId, usuario.Id, cancellationToken);

        if (vinculoExistente is not null)
        {
            vinculoExistente.Ativo = true;
            vinculoExistente.PodeReceberAgendamento = request.PodeReceberAgendamento;
            vinculoExistente.DataEntrada = DateTime.UtcNow;
            vinculoExistente.DataSaida = null;
            vinculoExistente.UpdatedAt = DateTime.UtcNow;

            _profissionalEstabelecimentoRepository.Atualizar(vinculoExistente);
            await _profissionalEstabelecimentoRepository.SalvarAlteracoesAsync(cancellationToken);
            await AuditarProfissionalAsync(
                estabelecimentoId,
                TipoAcaoAuditoriaNegocio.ProfissionalConvidado,
                vinculoExistente,
                "reativado",
                cancellationToken);
            await _equipeNotificacaoService.ProfissionalConvidadoAsync(
                vinculoExistente,
                profissional,
                cancellationToken);

            return ProfissionalEquipeResponseDto.From(vinculoExistente, profissional);
        }

        var vinculo = new ProfissionalEstabelecimento
        {
            EstabelecimentoId = estabelecimentoId,
            ProfissionalId = profissional.Id,
            Ativo = true,
            PodeReceberAgendamento = request.PodeReceberAgendamento
        };

        await _profissionalEstabelecimentoRepository.AdicionarAsync(vinculo, cancellationToken);
        await _profissionalEstabelecimentoRepository.SalvarAlteracoesAsync(cancellationToken);
        await AuditarProfissionalAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ProfissionalConvidado,
            vinculo,
            "criado",
            cancellationToken);
        await _equipeNotificacaoService.ProfissionalConvidadoAsync(
            vinculo,
            profissional,
            cancellationToken);

        return ProfissionalEquipeResponseDto.From(vinculo, profissional);
    }

    public async Task<ProfissionalEquipeResponseDto> AtualizarProfissionalAsync(
        int estabelecimentoId,
        int profissionalId,
        AtualizarProfissionalEquipeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ProfissionalGerenciar,
            cancellationToken);

        var vinculo = await _profissionalEstabelecimentoRepository.ObterPorProfissionalAsync(
            profissionalId,
            estabelecimentoId,
            cancellationToken);

        if (vinculo is null)
        {
            throw new ProfissionalNegocioNaoEncontradoException();
        }

        var profissional = await _profissionalRepository.ObterPorIdAsync(profissionalId, cancellationToken)
            ?? throw new ProfissionalNegocioNaoEncontradoException();

        if (!string.IsNullOrWhiteSpace(request.NomePublico))
        {
            profissional.NomePublico = NormalizarTexto(request.NomePublico, profissional.NomePublico);
        }

        if (request.Biografia is not null)
        {
            profissional.Biografia = request.Biografia.Trim();
        }

        if (request.RemoverFoto)
        {
            profissional.Logo = string.Empty;
        }
        else if (!string.IsNullOrWhiteSpace(request.Foto))
        {
            profissional.Logo = NormalizarFotoProfissional(request.Foto, request.FotoContentType);
        }

        profissional.UpdatedAt = DateTime.UtcNow;
        _profissionalRepository.Atualizar(profissional);
        await _profissionalRepository.SalvarAlteracoesAsync(cancellationToken);

        return ProfissionalEquipeResponseDto.From(vinculo, profissional);
    }

    public async Task<UsuarioEquipeResponseDto> AtualizarRoleUsuarioAsync(
        int estabelecimentoId,
        int usuarioId,
        AtualizarRoleUsuarioEquipeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.EquipeGerenciar,
            cancellationToken);

        var vinculo = await ObterVinculoUsuarioEquipeAsync(estabelecimentoId, usuarioId, cancellationToken);
        var roleAnterior = vinculo.RoleNoEstabelecimento;
        if (vinculo.RoleNoEstabelecimento == EstablishmentUserRole.Owner
            && request.Role != EstablishmentUserRole.Owner)
        {
            await ValidarNaoRemoveUltimoOwnerAsync(estabelecimentoId, usuarioId, cancellationToken);
        }

        vinculo.RoleNoEstabelecimento = request.Role;
        vinculo.UpdatedAt = DateTime.UtcNow;

        _estabelecimentoUsuarioRepository.Atualizar(vinculo);
        await _estabelecimentoUsuarioRepository.SalvarAlteracoesAsync(cancellationToken);
        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.UsuarioEquipeRoleAlterada,
            nameof(EstabelecimentoUsuario),
            vinculo.Id,
            new
            {
                vinculo.UsuarioId,
                roleAnterior,
                roleNova = request.Role
            },
            cancellationToken);

        var usuario = await ObterUsuarioPorIdOuFalharAsync(usuarioId, cancellationToken);
        await _equipeNotificacaoService.RoleUsuarioAlteradaAsync(
            vinculo,
            usuario,
            roleAnterior,
            request.Role,
            cancellationToken);

        return UsuarioEquipeResponseDto.From(vinculo, usuario);
    }

    public async Task<UsuarioEquipeResponseDto> AtualizarStatusUsuarioAsync(
        int estabelecimentoId,
        int usuarioId,
        AtualizarStatusUsuarioEquipeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.EquipeGerenciar,
            cancellationToken);

        var vinculo = await ObterVinculoUsuarioEquipeAsync(estabelecimentoId, usuarioId, cancellationToken);
        var statusAnterior = vinculo.Ativo;
        if (vinculo.Ativo
            && !request.Ativo
            && vinculo.RoleNoEstabelecimento == EstablishmentUserRole.Owner)
        {
            await ValidarNaoRemoveUltimoOwnerAsync(estabelecimentoId, usuarioId, cancellationToken);
        }

        if (!vinculo.Ativo && request.Ativo)
        {
            await ValidarLimiteUsuariosAsync(estabelecimentoId, cancellationToken);
        }

        vinculo.Ativo = request.Ativo;
        vinculo.UpdatedAt = DateTime.UtcNow;

        _estabelecimentoUsuarioRepository.Atualizar(vinculo);
        await _estabelecimentoUsuarioRepository.SalvarAlteracoesAsync(cancellationToken);
        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.UsuarioEquipeStatusAlterado,
            nameof(EstabelecimentoUsuario),
            vinculo.Id,
            new
            {
                vinculo.UsuarioId,
                ativoAnterior = statusAnterior,
                ativoNovo = request.Ativo,
                vinculo.RoleNoEstabelecimento
            },
            cancellationToken);

        var usuario = await ObterUsuarioPorIdOuFalharAsync(usuarioId, cancellationToken);
        await _equipeNotificacaoService.StatusUsuarioAlteradoAsync(
            vinculo,
            usuario,
            statusAnterior,
            request.Ativo,
            cancellationToken);

        return UsuarioEquipeResponseDto.From(vinculo, usuario);
    }

    public async Task<ProfissionalEquipeResponseDto> AtualizarStatusProfissionalAsync(
        int estabelecimentoId,
        int profissionalId,
        AtualizarStatusProfissionalEquipeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ProfissionalGerenciar,
            cancellationToken);

        var vinculo = await _profissionalEstabelecimentoRepository.ObterPorProfissionalAsync(
            profissionalId,
            estabelecimentoId,
            cancellationToken);

        if (vinculo is null)
        {
            throw new ProfissionalNegocioNaoEncontradoException();
        }

        var statusAnterior = vinculo.Ativo;
        var podeReceberAgendamentoAnterior = vinculo.PodeReceberAgendamento;

        if (!vinculo.Ativo && request.Ativo)
        {
            await ValidarLimiteProfissionaisAsync(estabelecimentoId, cancellationToken);
            vinculo.DataEntrada = DateTime.UtcNow;
            vinculo.DataSaida = null;
        }

        if (!request.Ativo)
        {
            await ValidarOuCancelarAgendamentosFuturosAsync(
                estabelecimentoId,
                profissionalId,
                request.CancelarAgendamentosFuturos,
                request.MotivoCancelamento,
                cancellationToken);
            vinculo.DataSaida = DateTime.UtcNow;
        }

        vinculo.Ativo = request.Ativo;
        vinculo.PodeReceberAgendamento = request.Ativo && request.PodeReceberAgendamento;
        vinculo.UpdatedAt = DateTime.UtcNow;

        _profissionalEstabelecimentoRepository.Atualizar(vinculo);
        await _profissionalEstabelecimentoRepository.SalvarAlteracoesAsync(cancellationToken);
        await _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ProfissionalStatusAlterado,
            nameof(ProfissionalEstabelecimento),
            vinculo.Id,
            new
            {
                vinculo.ProfissionalId,
                ativoAnterior = statusAnterior,
                ativoNovo = request.Ativo,
                podeReceberAgendamentoAnterior,
                podeReceberAgendamentoNovo = vinculo.PodeReceberAgendamento
            },
            cancellationToken);

        var profissional = await _profissionalRepository.ObterPorIdAsync(profissionalId, cancellationToken)
            ?? throw new ProfissionalNegocioNaoEncontradoException();
        await _equipeNotificacaoService.StatusProfissionalAlteradoAsync(
            vinculo,
            profissional,
            statusAnterior,
            request.Ativo,
            cancellationToken);

        return ProfissionalEquipeResponseDto.From(vinculo, profissional);
    }

    public async Task<IReadOnlyList<AgendamentoFuturoEquipeResponseDto>> ListarAgendamentosFuturosProfissionalAsync(
        int estabelecimentoId,
        int profissionalId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ProfissionalGerenciar,
            cancellationToken);

        await GarantirProfissionalNoEstabelecimentoAsync(
            estabelecimentoId,
            profissionalId,
            cancellationToken);

        return await _agendamentoItemRepository.ListarAgendamentosFuturosAtivosPorProfissionalAsync(
            estabelecimentoId,
            profissionalId,
            cancellationToken);
    }

    public async Task<CancelarAgendamentosFuturosProfissionalEquipeResponseDto> CancelarAgendamentosFuturosProfissionalAsync(
        int estabelecimentoId,
        int profissionalId,
        CancelarAgendamentosFuturosProfissionalEquipeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ProfissionalGerenciar,
            cancellationToken);

        await GarantirProfissionalNoEstabelecimentoAsync(
            estabelecimentoId,
            profissionalId,
            cancellationToken);

        var quantidadeCancelada = await _agendamentoNegocioService.CancelarAgendamentosFuturosDoProfissionalAsync(
            estabelecimentoId,
            profissionalId,
            request.Motivo,
            autorizarAgendaCancelar: true,
            cancellationToken);

        return new CancelarAgendamentosFuturosProfissionalEquipeResponseDto
        {
            QuantidadeCancelada = quantidadeCancelada
        };
    }

    private async Task ValidarOuCancelarAgendamentosFuturosAsync(
        int estabelecimentoId,
        int profissionalId,
        bool cancelarAgendamentosFuturos,
        string? motivoCancelamento,
        CancellationToken cancellationToken)
    {
        var agendamentosFuturos = await _agendamentoItemRepository.ListarAgendamentosFuturosAtivosPorProfissionalAsync(
            estabelecimentoId,
            profissionalId,
            cancellationToken);

        if (agendamentosFuturos.Count == 0)
        {
            return;
        }

        if (!cancelarAgendamentosFuturos)
        {
            throw new ProfissionalEquipeComAgendamentoFuturoException(
                "Este profissional possui agendamentos futuros. Cancele ou reagende antes de remover.",
                agendamentosFuturos.Select(MapearAgendamentoFuturoResumo).ToList());
        }

        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.AgendaCancelar,
            cancellationToken);

        await _agendamentoNegocioService.CancelarAgendamentosFuturosDoProfissionalAsync(
            estabelecimentoId,
            profissionalId,
            motivoCancelamento ?? string.Empty,
            autorizarAgendaCancelar: false,
            cancellationToken);
    }

    private static AgendamentoFuturoEquipeResumo MapearAgendamentoFuturoResumo(
        AgendamentoFuturoEquipeResponseDto agendamento) =>
        new(
            agendamento.AgendamentoId,
            agendamento.AgendamentoItemId,
            agendamento.ClienteNome,
            agendamento.ServicoNome,
            agendamento.Inicio,
            agendamento.Fim,
            agendamento.Status);

    private async Task GarantirProfissionalNoEstabelecimentoAsync(
        int estabelecimentoId,
        int profissionalId,
        CancellationToken cancellationToken)
    {
        var vinculo = await _profissionalEstabelecimentoRepository.ObterPorProfissionalAsync(
            profissionalId,
            estabelecimentoId,
            cancellationToken);

        if (vinculo is null)
        {
            throw new ProfissionalNegocioNaoEncontradoException();
        }
    }

    private async Task<Usuario?> ObterUsuarioAsync(
        CadastrarUsuarioEquipeRequestDto request,
        CancellationToken cancellationToken)
    {
        return await ObterUsuarioAsync(request.Email, request.Telefone, cancellationToken)
            ?? throw new UsuarioEquipeNegocioNaoEncontradoException();
    }

    private async Task<Usuario?> ObterUsuarioAsync(
        string? emailEntrada,
        string? telefoneEntrada,
        CancellationToken cancellationToken)
    {
        var email = emailEntrada?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(email))
        {
            return await _usuarioRepository.ObterPorEmailAsync(email, cancellationToken);
        }

        var telefone = telefoneEntrada?.Trim();
        if (!string.IsNullOrWhiteSpace(telefone))
        {
            return await _usuarioRepository.ObterPorTelefoneAsync(telefone, cancellationToken);
        }

        return null;
    }

    private async Task<EstabelecimentoUsuario> ObterVinculoUsuarioEquipeAsync(
        int estabelecimentoId,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        return await _estabelecimentoUsuarioRepository.ObterPorUsuarioAsync(
            estabelecimentoId,
            usuarioId,
            cancellationToken)
            ?? throw new UsuarioEquipeNegocioNaoEncontradoException();
    }

    private async Task<Usuario> ObterUsuarioPorIdOuFalharAsync(
        int usuarioId,
        CancellationToken cancellationToken)
    {
        return await _usuarioRepository.ObterPorIdAsync(usuarioId, cancellationToken)
            ?? throw new UsuarioEquipeNegocioNaoEncontradoException();
    }

    private async Task ValidarNaoRemoveUltimoOwnerAsync(
        int estabelecimentoId,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var outrosOwnersAtivos = await _estabelecimentoUsuarioRepository.ContarOwnersAtivosAsync(
            estabelecimentoId,
            usuarioId,
            cancellationToken);

        if (outrosOwnersAtivos == 0)
        {
            throw new UltimoOwnerNegocioException();
        }
    }

    private Task AuditarUsuarioEquipeAsync(
        int estabelecimentoId,
        TipoAcaoAuditoriaNegocio tipoAcao,
        EstabelecimentoUsuario vinculo,
        EstablishmentUserRole role,
        string origem,
        CancellationToken cancellationToken)
    {
        return _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            tipoAcao,
            nameof(EstabelecimentoUsuario),
            vinculo.Id,
            new
            {
                vinculo.UsuarioId,
                role,
                vinculo.Ativo,
                origem
            },
            cancellationToken);
    }

    private Task AuditarProfissionalAsync(
        int estabelecimentoId,
        TipoAcaoAuditoriaNegocio tipoAcao,
        ProfissionalEstabelecimento vinculo,
        string origem,
        CancellationToken cancellationToken)
    {
        return _auditoriaNegocioService.RegistrarAsync(
            estabelecimentoId,
            tipoAcao,
            nameof(ProfissionalEstabelecimento),
            vinculo.Id,
            new
            {
                vinculo.ProfissionalId,
                vinculo.Ativo,
                vinculo.PodeReceberAgendamento,
                origem
            },
            cancellationToken);
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

    private async Task<Profissional> ObterOuCriarProfissionalAsync(
        Usuario usuario,
        ConvidarProfissionalEquipeRequestDto request,
        CancellationToken cancellationToken)
    {
        var fotoInformada = !string.IsNullOrWhiteSpace(request.ResolverFoto());
        var fotoNormalizada = fotoInformada
            ? NormalizarFotoProfissional(request.ResolverFoto(), request.FotoContentType)
            : null;

        var profissional = await _profissionalRepository.ObterPorUsuarioIdAsync(usuario.Id, cancellationToken);
        if (profissional is not null)
        {
            if (!profissional.Ativo)
            {
                profissional.Ativo = true;
            }

            profissional.TipoProfissional = ProfessionalType.VinculadoEstabelecimento;

            if (!string.IsNullOrWhiteSpace(request.NomePublico))
            {
                profissional.NomePublico = NormalizarTexto(request.NomePublico, profissional.NomePublico);
            }

            if (request.Biografia is not null)
            {
                profissional.Biografia = request.Biografia.Trim();
            }

            // Atualiza a foto do profissional apenas quando enviada; nunca copia o avatar da conta.
            if (fotoNormalizada is not null)
            {
                profissional.Logo = fotoNormalizada;
            }

            profissional.UpdatedAt = DateTime.UtcNow;
            _profissionalRepository.Atualizar(profissional);
            await _profissionalRepository.SalvarAlteracoesAsync(cancellationToken);

            return profissional;
        }

        profissional = new Profissional
        {
            UsuarioId = usuario.Id,
            NomePublico = NormalizarTexto(request.NomePublico, usuario.Nome),
            Biografia = request.Biografia?.Trim() ?? string.Empty,
            Logo = fotoNormalizada ?? string.Empty,
            Telefone = usuario.Telefone,
            Email = usuario.Email,
            TipoProfissional = ProfessionalType.VinculadoEstabelecimento,
            Ativo = true
        };

        await _profissionalRepository.AdicionarAsync(profissional, cancellationToken);
        await _profissionalRepository.SalvarAlteracoesAsync(cancellationToken);

        return profissional;
    }

    /// <summary>
    /// Valida e normaliza a foto de apresentação do profissional.
    /// Independente do avatar da conta (<see cref="Usuario.AvatarBase64"/>).
    /// </summary>
    private string NormalizarFotoProfissional(string? foto, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(foto))
        {
            throw new AvatarInvalidoException("Foto do profissional e obrigatoria quando enviada.");
        }

        try
        {
            return _thumbnailer.ParaPersistencia(
                _avatarBase64Decoder.ValidarENormalizar(foto.Trim(), contentType));
        }
        catch (AvatarInvalidoException ex)
        {
            throw new AvatarInvalidoException(
                ex.Message.Replace("Avatar", "Foto do profissional", StringComparison.Ordinal));
        }
    }

    private async Task GarantirAcessoProfissionalAsync(
        int estabelecimentoId,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var vinculoUsuario = await _estabelecimentoUsuarioRepository.ObterPorUsuarioAsync(
            estabelecimentoId,
            usuarioId,
            cancellationToken);

        if (vinculoUsuario?.Ativo == true)
        {
            return;
        }

        if (vinculoUsuario is not null)
        {
            vinculoUsuario.RoleNoEstabelecimento = EstablishmentUserRole.Profissional;
            vinculoUsuario.Ativo = true;
            vinculoUsuario.UpdatedAt = DateTime.UtcNow;

            _estabelecimentoUsuarioRepository.Atualizar(vinculoUsuario);
            await _estabelecimentoUsuarioRepository.SalvarAlteracoesAsync(cancellationToken);
            return;
        }

        await ValidarLimiteUsuariosAsync(estabelecimentoId, cancellationToken);

        await _estabelecimentoUsuarioRepository.AdicionarAsync(new EstabelecimentoUsuario
        {
            EstabelecimentoId = estabelecimentoId,
            UsuarioId = usuarioId,
            RoleNoEstabelecimento = EstablishmentUserRole.Profissional,
            Ativo = true
        }, cancellationToken);
        await _estabelecimentoUsuarioRepository.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UsuarioEquipeResponseDto>> ListarUsuariosAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.EquipeGerenciar,
            cancellationToken);

        var vinculos = await _estabelecimentoUsuarioRepository.ListarAtivosPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        return vinculos
            .Where(vinculo => vinculo.Usuario is not null)
            .Select(vinculo => UsuarioEquipeResponseDto.From(vinculo, vinculo.Usuario!))
            .ToList();
    }

    public async Task<IReadOnlyList<ProfissionalEquipeResponseDto>> ListarProfissionaisAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ProfissionalGerenciar,
            cancellationToken);

        var vinculos = await _profissionalEstabelecimentoRepository.ListarAtivosPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        return vinculos
            .Where(vinculo => vinculo.Profissional is not null)
            .Select(vinculo =>
            {
                var profissional = vinculo.Profissional!;
                var foto = string.IsNullOrWhiteSpace(profissional.Logo)
                    ? null
                    : _thumbnailer.ParaListagem(profissional.Logo, maxLadoPx: 96, qualidadeJpeg: 72);
                return new ProfissionalEquipeResponseDto(
                    vinculo.Id,
                    vinculo.EstabelecimentoId,
                    profissional.Id,
                    profissional.UsuarioId,
                    profissional.NomePublico,
                    profissional.Email,
                    profissional.Telefone,
                    vinculo.PodeReceberAgendamento,
                    vinculo.Ativo,
                    profissional.NotaMedia,
                    profissional.TotalAvaliacoes,
                    foto);
            })
            .ToList();
    }

    public async Task<EquipeMembrosPaginadoResponseDto> ListarMembrosPaginadoAsync(
        int estabelecimentoId,
        EquipeMembrosFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.EquipeGerenciar,
            cancellationToken);

        var pagina = Math.Max(1, filtro.Pagina);
        var tamanhoPagina = Math.Clamp(filtro.TamanhoPagina <= 0 ? 6 : filtro.TamanhoPagina, 1, 50);

        var usuarios = await _estabelecimentoUsuarioRepository.ListarAtivosPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);
        var profissionais = await _profissionalEstabelecimentoRepository.ListarAtivosPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);
        var convites = await _conviteRepository.ListarPorEstabelecimentoAsync(
            estabelecimentoId,
            StatusConviteNegocio.Pendente,
            cancellationToken);

        var profissionalPorUsuarioId = profissionais
            .Where(v => v.Profissional?.UsuarioId is int uid)
            .ToDictionary(v => v.Profissional!.UsuarioId!.Value, v => v);

        var usuarioIds = new HashSet<int>();
        var membros = new List<MembroEquipeResponseDto>();

        foreach (var vinculo in usuarios.Where(v => v.Usuario is not null))
        {
            var usuario = vinculo.Usuario!;
            usuarioIds.Add(usuario.Id);
            profissionalPorUsuarioId.TryGetValue(usuario.Id, out var vinculoProf);
            var profissional = vinculoProf?.Profissional;

            membros.Add(new MembroEquipeResponseDto(
                Id: $"usuario-{vinculo.Id}",
                Tipo: "usuario",
                Nome: usuario.Nome,
                Cargo: RotuloCargo(vinculo.RoleNoEstabelecimento.ToString()),
                Role: vinculo.RoleNoEstabelecimento.ToString(),
                Email: string.IsNullOrWhiteSpace(usuario.Email) ? null : usuario.Email,
                Telefone: string.IsNullOrWhiteSpace(usuario.Telefone) ? null : usuario.Telefone,
                Ativo: vinculo.Ativo,
                UsuarioId: usuario.Id,
                ProfissionalId: profissional?.Id,
                PodeReceberAgendamento: vinculoProf?.PodeReceberAgendamento,
                Foto: string.IsNullOrWhiteSpace(profissional?.Logo)
                    ? null
                    : _thumbnailer.ParaListagem(profissional!.Logo, maxLadoPx: 96, qualidadeJpeg: 72),
                ConviteEm: null));
        }

        foreach (var vinculo in profissionais.Where(v => v.Profissional is not null))
        {
            var profissional = vinculo.Profissional!;
            if (profissional.UsuarioId is int uid && usuarioIds.Contains(uid))
            {
                continue;
            }

            membros.Add(new MembroEquipeResponseDto(
                Id: $"profissional-{vinculo.Id}",
                Tipo: "profissional",
                Nome: profissional.NomePublico,
                Cargo: RotuloCargo(nameof(EstablishmentUserRole.Profissional)),
                Role: nameof(EstablishmentUserRole.Profissional),
                Email: string.IsNullOrWhiteSpace(profissional.Email) ? null : profissional.Email,
                Telefone: string.IsNullOrWhiteSpace(profissional.Telefone) ? null : profissional.Telefone,
                Ativo: vinculo.Ativo,
                UsuarioId: profissional.UsuarioId,
                ProfissionalId: profissional.Id,
                PodeReceberAgendamento: vinculo.PodeReceberAgendamento,
                Foto: string.IsNullOrWhiteSpace(profissional.Logo)
                    ? null
                    : _thumbnailer.ParaListagem(profissional.Logo, maxLadoPx: 96, qualidadeJpeg: 72),
                ConviteEm: null));
        }

        foreach (var convite in convites)
        {
            var nome = string.IsNullOrWhiteSpace(convite.NomePublico)
                ? (convite.Email.Contains('@') ? convite.Email.Split('@')[0] : convite.Email)
                : convite.NomePublico;

            membros.Add(new MembroEquipeResponseDto(
                Id: $"convite-{convite.Id}",
                Tipo: "convite",
                Nome: string.IsNullOrWhiteSpace(nome) ? convite.Email : nome,
                Cargo: "Convidado",
                Role: "Convidado",
                Email: convite.Email,
                Telefone: string.IsNullOrWhiteSpace(convite.Telefone) ? null : convite.Telefone,
                Ativo: false,
                UsuarioId: null,
                ProfissionalId: null,
                PodeReceberAgendamento: null,
                Foto: null,
                ConviteEm: convite.CriadoEm.ToString("dd/MM/yyyy")));
        }

        var resumo = new EquipeMembrosResumoDto(
            TotalMembros: membros.Count(m => m.Tipo != "convite" && m.Ativo),
            Administradores: membros.Count(m =>
                m.Ativo && (m.Role is nameof(EstablishmentUserRole.Owner) or nameof(EstablishmentUserRole.Admin))),
            Profissionais: membros.Count(m =>
                m.Ativo && m.Role == nameof(EstablishmentUserRole.Profissional)),
            Recepcionistas: membros.Count(m =>
                m.Ativo && m.Role == nameof(EstablishmentUserRole.Receptionist)),
            Convidados: convites.Count);

        var filtrados = FiltrarMembros(membros, filtro)
            .OrderBy(m => OrdemCargo(m.Role))
            .ThenBy(m => m.Nome, StringComparer.Create(new System.Globalization.CultureInfo("pt-BR"), ignoreCase: true))
            .ToList();

        var total = filtrados.Count;
        var itens = filtrados
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToList();

        return new EquipeMembrosPaginadoResponseDto(total, pagina, tamanhoPagina, itens, resumo);
    }

    private static IEnumerable<MembroEquipeResponseDto> FiltrarMembros(
        IEnumerable<MembroEquipeResponseDto> membros,
        EquipeMembrosFiltroDto filtro)
    {
        var cargo = filtro.Cargo?.Trim();
        var status = filtro.Status?.Trim().ToLowerInvariant();
        var busca = filtro.Busca?.Trim().ToLowerInvariant();

        foreach (var membro in membros)
        {
            if (!string.IsNullOrWhiteSpace(cargo) &&
                !string.Equals(membro.Role, cargo, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusMembro = membro.Tipo == "convite"
                    ? "pendente"
                    : membro.Ativo ? "ativo" : "inativo";
                if (!string.Equals(statusMembro, status, StringComparison.Ordinal))
                {
                    continue;
                }
            }

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var hay = string.Join(
                    ' ',
                    membro.Nome,
                    membro.Cargo,
                    membro.Email ?? string.Empty).ToLowerInvariant();
                if (!hay.Contains(busca, StringComparison.Ordinal))
                {
                    continue;
                }
            }

            yield return membro;
        }
    }

    private static int OrdemCargo(string role) => role switch
    {
        nameof(EstablishmentUserRole.Owner) => 0,
        nameof(EstablishmentUserRole.Admin) => 1,
        nameof(EstablishmentUserRole.Manager) => 2,
        nameof(EstablishmentUserRole.Receptionist) => 3,
        nameof(EstablishmentUserRole.Profissional) => 4,
        "Convidado" => 5,
        _ => 99
    };

    private static string RotuloCargo(string role) => role switch
    {
        nameof(EstablishmentUserRole.Owner) => "Dono",
        nameof(EstablishmentUserRole.Admin) => "Administrador",
        nameof(EstablishmentUserRole.Manager) => "Gerente",
        nameof(EstablishmentUserRole.Receptionist) => "Recepcionista",
        nameof(EstablishmentUserRole.Profissional) => "Profissional",
        "Convidado" => "Convidado",
        _ => role
    };

    public async Task<ProfissionalVitrineResponseDto> CadastrarProfissionalVitrineAsync(
        int estabelecimentoId,
        CadastrarProfissionalVitrineRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ProfissionalGerenciar,
            cancellationToken);

        await ValidarPlanoBasicSemModuloProfissionaisAsync(estabelecimentoId, cancellationToken);
        await ValidarLimiteProfissionaisAsync(estabelecimentoId, cancellationToken);

        var nomePublico = NormalizarTexto(request.NomePublico, string.Empty);
        if (string.IsNullOrWhiteSpace(nomePublico))
        {
            throw new ProfissionalNegocioInvalidoException("Nome publico do profissional e obrigatorio.");
        }

        var profissional = new Profissional
        {
            NomePublico = nomePublico,
            Biografia = request.Biografia?.Trim() ?? string.Empty,
            Logo = request.Logo?.Trim() ?? string.Empty,
            TipoProfissional = ProfessionalType.SomenteExibicao,
            Ativo = true
        };

        await _profissionalRepository.AdicionarAsync(profissional, cancellationToken);
        await _profissionalRepository.SalvarAlteracoesAsync(cancellationToken);

        var vinculo = new ProfissionalEstabelecimento
        {
            EstabelecimentoId = estabelecimentoId,
            ProfissionalId = profissional.Id,
            Ativo = true,
            SomenteExibicao = true,
            PodeReceberAgendamento = false
        };

        await _profissionalEstabelecimentoRepository.AdicionarAsync(vinculo, cancellationToken);
        await _profissionalEstabelecimentoRepository.SalvarAlteracoesAsync(cancellationToken);

        await AuditarProfissionalAsync(
            estabelecimentoId,
            TipoAcaoAuditoriaNegocio.ProfissionalConvidado,
            vinculo,
            "vitrine_criado",
            cancellationToken);

        return ProfissionalVitrineResponseDto.From(vinculo, profissional);
    }

    public async Task<ProfissionalVitrineResponseDto> AtualizarProfissionalVitrineAsync(
        int estabelecimentoId,
        int profissionalId,
        AtualizarProfissionalVitrineRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ProfissionalGerenciar,
            cancellationToken);

        await ValidarPlanoBasicSemModuloProfissionaisAsync(estabelecimentoId, cancellationToken);

        var (vinculo, profissional) = await ObterVinculoVitrineAsync(
            estabelecimentoId,
            profissionalId,
            cancellationToken);

        var nomePublico = NormalizarTexto(request.NomePublico, string.Empty);
        if (string.IsNullOrWhiteSpace(nomePublico))
        {
            throw new ProfissionalNegocioInvalidoException("Nome publico do profissional e obrigatorio.");
        }

        profissional.NomePublico = nomePublico;
        profissional.Biografia = request.Biografia?.Trim() ?? string.Empty;
        profissional.Logo = request.Logo?.Trim() ?? string.Empty;
        profissional.UpdatedAt = DateTime.UtcNow;

        _profissionalRepository.Atualizar(profissional);
        await _profissionalRepository.SalvarAlteracoesAsync(cancellationToken);

        return ProfissionalVitrineResponseDto.From(vinculo, profissional);
    }

    public async Task<ProfissionalVitrineResponseDto> AtualizarStatusProfissionalVitrineAsync(
        int estabelecimentoId,
        int profissionalId,
        AtualizarStatusProfissionalVitrineRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ProfissionalGerenciar,
            cancellationToken);

        await ValidarPlanoBasicSemModuloProfissionaisAsync(estabelecimentoId, cancellationToken);

        var (vinculo, profissional) = await ObterVinculoVitrineAsync(
            estabelecimentoId,
            profissionalId,
            cancellationToken);

        if (request.Ativo && !vinculo.Ativo)
        {
            await ValidarLimiteProfissionaisAsync(estabelecimentoId, cancellationToken);
        }

        vinculo.Ativo = request.Ativo;
        vinculo.PodeReceberAgendamento = false;
        vinculo.DataSaida = request.Ativo ? null : DateTime.UtcNow;
        vinculo.UpdatedAt = DateTime.UtcNow;
        profissional.Ativo = request.Ativo;
        profissional.UpdatedAt = DateTime.UtcNow;

        _profissionalEstabelecimentoRepository.Atualizar(vinculo);
        _profissionalRepository.Atualizar(profissional);
        await _profissionalEstabelecimentoRepository.SalvarAlteracoesAsync(cancellationToken);

        return ProfissionalVitrineResponseDto.From(vinculo, profissional);
    }

    public async Task<IReadOnlyList<ProfissionalVitrineResponseDto>> ListarProfissionaisVitrineAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ProfissionalGerenciar,
            cancellationToken);

        await ValidarPlanoBasicSemModuloProfissionaisAsync(estabelecimentoId, cancellationToken);

        var vinculos = await _profissionalEstabelecimentoRepository.ListarVitrinePorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        return vinculos
            .Where(vinculo => vinculo.Profissional is not null)
            .Select(vinculo => ProfissionalVitrineResponseDto.From(vinculo, vinculo.Profissional!))
            .ToList();
    }

    private async Task ValidarPlanoBasicSemModuloProfissionaisAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var possuiProfissionais = await _modulosAssinaturaService.PossuiModuloPorEstabelecimentoAsync(
            estabelecimentoId,
            ModuloAssinatura.Profissionais,
            cancellationToken);

        if (possuiProfissionais)
        {
            throw new ProfissionalVitrineNegocioIndisponivelException();
        }
    }

    private async Task<(ProfissionalEstabelecimento Vinculo, Profissional Profissional)> ObterVinculoVitrineAsync(
        int estabelecimentoId,
        int profissionalId,
        CancellationToken cancellationToken)
    {
        var vinculo = await _profissionalEstabelecimentoRepository.ObterPorProfissionalAsync(
            profissionalId,
            estabelecimentoId,
            cancellationToken);

        if (vinculo?.Profissional is null || !vinculo.SomenteExibicao)
        {
            throw new RecursoProfissionalNaoEncontradoException();
        }

        return (vinculo, vinculo.Profissional);
    }

    private static string NormalizarTexto(string? valor, string fallback)
    {
        var texto = valor?.Trim();
        return string.IsNullOrWhiteSpace(texto) ? fallback.Trim() : texto;
    }
}
