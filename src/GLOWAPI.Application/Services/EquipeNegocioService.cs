using GLOWAPI.Application.DTOs.Equipe;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Services;

public class EquipeNegocioService : IEquipeNegocioService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IModulosAssinaturaService _modulosAssinaturaService;
    private readonly IAuditoriaNegocioService _auditoriaNegocioService;
    private readonly IEquipeNotificacaoService _equipeNotificacaoService;

    public EquipeNegocioService(
        IUsuarioRepository usuarioRepository,
        IProfissionalRepository profissionalRepository,
        IEstabelecimentoUsuarioRepository estabelecimentoUsuarioRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IModulosAssinaturaService modulosAssinaturaService,
        IAuditoriaNegocioService auditoriaNegocioService,
        IEquipeNotificacaoService equipeNotificacaoService)
    {
        _usuarioRepository = usuarioRepository;
        _profissionalRepository = profissionalRepository;
        _estabelecimentoUsuarioRepository = estabelecimentoUsuarioRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _modulosAssinaturaService = modulosAssinaturaService;
        _auditoriaNegocioService = auditoriaNegocioService;
        _equipeNotificacaoService = equipeNotificacaoService;
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
        var profissional = await _profissionalRepository.ObterPorUsuarioIdAsync(usuario.Id, cancellationToken);
        if (profissional is not null)
        {
            if (!profissional.Ativo)
            {
                profissional.Ativo = true;
            }

            profissional.TipoProfissional = ProfessionalType.VinculadoEstabelecimento;
            profissional.UpdatedAt = DateTime.UtcNow;
            _profissionalRepository.Atualizar(profissional);

            return profissional;
        }

        profissional = new Profissional
        {
            UsuarioId = usuario.Id,
            NomePublico = NormalizarTexto(request.NomePublico, usuario.Nome),
            Biografia = request.Biografia?.Trim() ?? string.Empty,
            Logo = request.Logo?.Trim() ?? usuario.AvatarBase64 ?? string.Empty,
            Telefone = usuario.Telefone,
            Email = usuario.Email,
            TipoProfissional = ProfessionalType.VinculadoEstabelecimento,
            Ativo = true
        };

        await _profissionalRepository.AdicionarAsync(profissional, cancellationToken);
        await _profissionalRepository.SalvarAlteracoesAsync(cancellationToken);

        return profissional;
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

    private static string NormalizarTexto(string? valor, string fallback)
    {
        var texto = valor?.Trim();
        return string.IsNullOrWhiteSpace(texto) ? fallback.Trim() : texto;
    }
}
