using System.Text.Json;
using GLOWAPI.Application.DTOs.Assinaturas;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Assinaturas;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;

namespace GLOWAPI.Application.Services;

public class AssinaturaOnboardingFinalizacaoService : IAssinaturaOnboardingFinalizacaoService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IAssinaturaRepository _assinaturaRepository;
    private readonly IAssinaturaEstabelecimentoRepository _assinaturaEstabelecimentoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IEnderecoGeocodificacaoService _enderecoGeocodificacaoService;
    private readonly IAvatarBase64Decoder _avatarBase64Decoder;

    public AssinaturaOnboardingFinalizacaoService(
        IEstabelecimentoRepository estabelecimentoRepository,
        IEstabelecimentoUsuarioRepository estabelecimentoUsuarioRepository,
        IProfissionalRepository profissionalRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IAssinaturaRepository assinaturaRepository,
        IAssinaturaEstabelecimentoRepository assinaturaEstabelecimentoRepository,
        IUsuarioRepository usuarioRepository,
        IEnderecoGeocodificacaoService enderecoGeocodificacaoService,
        IAvatarBase64Decoder avatarBase64Decoder)
    {
        _estabelecimentoRepository = estabelecimentoRepository;
        _estabelecimentoUsuarioRepository = estabelecimentoUsuarioRepository;
        _profissionalRepository = profissionalRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _assinaturaRepository = assinaturaRepository;
        _assinaturaEstabelecimentoRepository = assinaturaEstabelecimentoRepository;
        _usuarioRepository = usuarioRepository;
        _enderecoGeocodificacaoService = enderecoGeocodificacaoService;
        _avatarBase64Decoder = avatarBase64Decoder;
    }

    public async Task FinalizarSePendenteAsync(Assinatura assinatura, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assinatura.OnboardingPendenteJson))
        {
            return;
        }

        var payload = JsonSerializer.Deserialize<AssinaturaOnboardingPendentePayload>(
            assinatura.OnboardingPendenteJson,
            JsonOptions);

        if (payload is null)
        {
            throw new AssinaturaTitularInvalidoException();
        }

        if (assinatura.EstabelecimentoId.HasValue)
        {
            var estabelecimentoExistente = await _estabelecimentoRepository.ObterPorIdComEnderecoAsync(
                assinatura.EstabelecimentoId.Value,
                cancellationToken);
            if (estabelecimentoExistente?.Endereco is not null
                && !OperacaoPerfilValidation.EnderecoPossuiCoordenadas(estabelecimentoExistente.Endereco))
            {
                await TentarGeocodificarAsync(estabelecimentoExistente, cancellationToken);
                _estabelecimentoRepository.Atualizar(estabelecimentoExistente);
                await _estabelecimentoRepository.SalvarAlteracoesAsync(cancellationToken);
            }

            assinatura.OnboardingPendenteJson = null;
            await PromoverRoleSeNecessarioAsync(payload.UsuarioId, payload.TipoAssinatura, cancellationToken);
            return;
        }

        Estabelecimento estabelecimento;
        if (payload.TipoAssinatura == TipoAssinatura.Estabelecimento)
        {
            if (payload.Estabelecimento is null)
            {
                throw new AssinaturaTitularInvalidoException();
            }

            estabelecimento = CriarEstabelecimento(payload.Estabelecimento);
            await TentarGeocodificarAsync(estabelecimento, cancellationToken);
            await _estabelecimentoRepository.AdicionarAsync(estabelecimento, cancellationToken);
            await _estabelecimentoUsuarioRepository.AdicionarAsync(new EstabelecimentoUsuario
            {
                Estabelecimento = estabelecimento,
                UsuarioId = payload.UsuarioId,
                RoleNoEstabelecimento = EstablishmentUserRole.Owner,
                Ativo = true
            }, cancellationToken);
        }
        else if (payload.TipoAssinatura == TipoAssinatura.ProfissionalAutonomo)
        {
            if (payload.ProfissionalAutonomo is null)
            {
                throw new AssinaturaTitularInvalidoException();
            }

            var profissional = await _profissionalRepository.ObterPorUsuarioIdAsync(payload.UsuarioId, cancellationToken)
                ?? CriarProfissionalAutonomo(payload.ProfissionalAutonomo, payload.UsuarioId);

            if (profissional.Id == 0)
            {
                await _profissionalRepository.AdicionarAsync(profissional, cancellationToken);
            }

            estabelecimento = CriarEstabelecimentoAutonomo(payload.ProfissionalAutonomo);
            await TentarGeocodificarAsync(estabelecimento, cancellationToken);
            await _estabelecimentoRepository.AdicionarAsync(estabelecimento, cancellationToken);

            await _estabelecimentoUsuarioRepository.AdicionarAsync(new EstabelecimentoUsuario
            {
                Estabelecimento = estabelecimento,
                UsuarioId = payload.UsuarioId,
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
        else
        {
            throw new AssinaturaTitularInvalidoException();
        }

        assinatura.Estabelecimento = estabelecimento;
        assinatura.EstabelecimentoId = estabelecimento.Id > 0 ? estabelecimento.Id : null;
        assinatura.OnboardingPendenteJson = null;
        _assinaturaRepository.Atualizar(assinatura);

        await PromoverRoleSeNecessarioAsync(payload.UsuarioId, payload.TipoAssinatura, cancellationToken);
        await _assinaturaRepository.SalvarAlteracoesAsync(cancellationToken);

        var assinaturaComPlano = await _assinaturaRepository.ObterPorIdComPlanoAsync(assinatura.Id, cancellationToken);
        if (assinaturaComPlano is not null)
        {
            await GarantirVinculoMatrizAsync(assinaturaComPlano, cancellationToken);
        }
    }

    private async Task PromoverRoleSeNecessarioAsync(
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

    private async Task TentarGeocodificarAsync(Estabelecimento estabelecimento, CancellationToken cancellationToken)
    {
        if (estabelecimento.Endereco is null)
        {
            return;
        }

        await _enderecoGeocodificacaoService.TentarGeocodificarAsync(estabelecimento.Endereco, cancellationToken);
    }

    private Estabelecimento CriarEstabelecimento(CriarEstabelecimentoAssinaturaDto dto)
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
            Logo = OperacaoPerfilValidation.ValidarLogoBase64(dto.Logo, "Logo do estabelecimento", _avatarBase64Decoder, CriarExcecao),
            Telefone = TelefoneHelper.NormalizarParaArmazenamento(
                OperacaoPerfilValidation.ValidarTextoObrigatorio(dto.Telefone, "Telefone do estabelecimento", 20, CriarExcecao)),
            Email = OperacaoPerfilValidation.ValidarTextoObrigatorio(dto.Email, "Email do estabelecimento", 255, CriarExcecao),
            Ativo = true,
            Endereco = OperacaoPerfilValidation.CriarEndereco(dto.Endereco, CriarExcecao)
        };
    }

    private static Estabelecimento CriarEstabelecimentoAutonomo(CriarProfissionalAutonomoAssinaturaDto dto) =>
        new()
        {
            Nome = dto.NomePublico.Trim(),
            Descricao = dto.Biografia.Trim(),
            Logo = dto.Logo?.Trim(),
            Telefone = TelefoneHelper.NormalizarParaArmazenamento(dto.Telefone),
            Email = dto.Email.Trim(),
            Ativo = true,
            Endereco = OperacaoPerfilValidation.CriarEndereco(
                dto.Endereco,
                mensagem => new ProfissionalAutonomoAssinaturaInvalidoException(mensagem)),
            Caixa = new Caixa()
        };

    private Profissional CriarProfissionalAutonomo(CriarProfissionalAutonomoAssinaturaDto dto, int userId)
    {
        ValidarProfissionalAutonomo(dto);
        var logo = OperacaoPerfilValidation.ValidarLogoBase64(
            dto.Logo,
            "Logo do profissional",
            _avatarBase64Decoder,
            mensagem => new ProfissionalAutonomoAssinaturaInvalidoException(mensagem));

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

    private static void ValidarProfissionalAutonomo(CriarProfissionalAutonomoAssinaturaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NomePublico))
        {
            throw new ProfissionalAutonomoAssinaturaInvalidoException("Nome publico do profissional e obrigatorio.");
        }

        if (dto.Biografia.Length > 1000)
        {
            throw new ProfissionalAutonomoAssinaturaInvalidoException("Biografia do profissional deve ter no maximo 1000 caracteres.");
        }
    }

    private async Task GarantirVinculoMatrizAsync(
        Assinatura assinatura,
        CancellationToken cancellationToken)
    {
        if (!assinatura.EstabelecimentoId.HasValue
            || !PlanoComercialCatalogo.PermiteMultiLoja(assinatura.Plano))
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
}
