using GLOWAPI.Application.DTOs.Profissionais;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Domain.Exceptions.Auth;

namespace GLOWAPI.Application.Services;

public class ProfissionalAutonomoPerfilService : IProfissionalAutonomoPerfilService
{
    private readonly IProfissionalRepository _profissionalRepository;
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IProfissionalEstabelecimentoRepository _profissionalEstabelecimentoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ICurrentUserContext _currentUser;
    private readonly IEnderecoGeocodificacaoService _enderecoGeocodificacaoService;
    private readonly IConfirmacaoWhatsAppService _confirmacaoWhatsAppService;
    private readonly IConfirmacaoWhatsAppEstabelecimentoService _confirmacaoWhatsAppEstabelecimentoService;
    private readonly IAvatarBase64Decoder _avatarBase64Decoder;

    public ProfissionalAutonomoPerfilService(
        IProfissionalRepository profissionalRepository,
        IEstabelecimentoRepository estabelecimentoRepository,
        IProfissionalEstabelecimentoRepository profissionalEstabelecimentoRepository,
        IUsuarioRepository usuarioRepository,
        ICurrentUserContext currentUser,
        IEnderecoGeocodificacaoService enderecoGeocodificacaoService,
        IConfirmacaoWhatsAppService confirmacaoWhatsAppService,
        IConfirmacaoWhatsAppEstabelecimentoService confirmacaoWhatsAppEstabelecimentoService,
        IAvatarBase64Decoder avatarBase64Decoder)
    {
        _profissionalRepository = profissionalRepository;
        _estabelecimentoRepository = estabelecimentoRepository;
        _profissionalEstabelecimentoRepository = profissionalEstabelecimentoRepository;
        _usuarioRepository = usuarioRepository;
        _currentUser = currentUser;
        _enderecoGeocodificacaoService = enderecoGeocodificacaoService;
        _confirmacaoWhatsAppService = confirmacaoWhatsAppService;
        _confirmacaoWhatsAppEstabelecimentoService = confirmacaoWhatsAppEstabelecimentoService;
        _avatarBase64Decoder = avatarBase64Decoder;
    }

    public async Task<ProfissionalAutonomoPerfilResponseDto> AtualizarAsync(
        int profissionalId,
        AtualizarProfissionalAutonomoPerfilDto request,
        CancellationToken cancellationToken = default)
    {
        var userId = ObterUserIdAutenticado();
        var profissional = await _profissionalRepository.ObterPorIdAsync(profissionalId, cancellationToken);
        if (profissional is null || !profissional.Ativo || profissional.TipoProfissional != ProfessionalType.Autonomo)
        {
            throw new TitularAssinaturaNaoEncontradoException();
        }

        if (profissional.UsuarioId != userId)
        {
            throw new UsuarioSemPermissaoAssinaturaException();
        }

        static Exception CriarExcecao(string mensagem) => new ProfissionalAutonomoAssinaturaInvalidoException(mensagem);

        var telefoneAnterior = profissional.Telefone;

        profissional.NomePublico = OperacaoPerfilValidation.ValidarTextoObrigatorio(request.NomePublico, "Nome publico do profissional", 150, CriarExcecao);
        profissional.Logo = OperacaoPerfilValidation.ValidarLogoBase64(
            request.Logo,
            "Logo do profissional",
            _avatarBase64Decoder,
            CriarExcecao);
        profissional.Telefone = OperacaoPerfilValidation.ValidarTextoObrigatorio(request.Telefone, "Telefone do profissional", 20, CriarExcecao);
        profissional.Email = OperacaoPerfilValidation.ValidarTextoObrigatorio(request.Email, "Email do profissional", 255, CriarExcecao);
        profissional.UpdatedAt = DateTime.UtcNow;

        var usuario = await _usuarioRepository.ObterPorIdAsync(userId, cancellationToken);
        if (usuario is not null)
        {
            WhatsAppConfirmacaoEntidade.ResetarAoAlterarTelefone(
                usuario.Telefone,
                profissional.Telefone,
                () =>
                {
                    usuario.WhatsAppConfirmadoEm = null;
                    usuario.WhatsAppOptIn = false;
                    usuario.LimparConfirmacaoWhatsApp();
                });

            usuario.Telefone = profissional.Telefone;
            usuario.UpdatedAt = DateTime.UtcNow;
            _usuarioRepository.Atualizar(usuario);
        }

        var vinculo = await _profissionalEstabelecimentoRepository.ObterAtivoPorProfissionalAsync(
            profissional.Id,
            cancellationToken);

        if (vinculo?.Estabelecimento is not null)
        {
            var estabelecimento = vinculo.Estabelecimento;
            WhatsAppConfirmacaoEntidade.ResetarAoAlterarTelefone(
                telefoneAnterior,
                profissional.Telefone,
                () =>
                {
                    estabelecimento.WhatsAppConfirmadoEm = null;
                    estabelecimento.WhatsAppOptIn = false;
                    estabelecimento.LimparConfirmacaoWhatsApp();
                });

            estabelecimento.Nome = profissional.NomePublico;
            estabelecimento.Logo = profissional.Logo;
            estabelecimento.Telefone = profissional.Telefone;
            estabelecimento.Email = profissional.Email;
            estabelecimento.UpdatedAt = DateTime.UtcNow;

            OperacaoPerfilValidation.AtualizarEndereco(
                estabelecimento.Endereco,
                endereco => estabelecimento.Endereco = endereco,
                request.Endereco,
                CriarExcecao);

            if (estabelecimento.Endereco is not null)
            {
                await _enderecoGeocodificacaoService.TentarGeocodificarAsync(estabelecimento.Endereco, cancellationToken);
            }

            _estabelecimentoRepository.Atualizar(estabelecimento);
        }

        _profissionalRepository.Atualizar(profissional);
        await _profissionalRepository.SalvarAlteracoesAsync(cancellationToken);

        if (!string.Equals(telefoneAnterior, profissional.Telefone, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(profissional.Telefone))
        {
            if (usuario is not null)
            {
                await _confirmacaoWhatsAppService.IniciarConfirmacaoAsync(usuario, cancellationToken);
            }

            if (vinculo?.Estabelecimento is not null)
            {
                var estabelecimento = vinculo.Estabelecimento;
                var emailsDestino = EmailDestinoHelper.Deduplicar([
                    usuario?.Email ?? string.Empty,
                    estabelecimento.Email
                ]);

                await _confirmacaoWhatsAppEstabelecimentoService.IniciarConfirmacaoAsync(
                    estabelecimento,
                    emailsDestino,
                    cancellationToken);
            }
        }

        return ProfissionalAutonomoPerfilResponseDto.From(profissional, vinculo?.Estabelecimento);
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
