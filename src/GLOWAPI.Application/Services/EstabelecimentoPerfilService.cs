using GLOWAPI.Application.DTOs.Estabelecimentos;
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Assinatura;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Domain.Exceptions.Usuario;

namespace GLOWAPI.Application.Services;

public class EstabelecimentoPerfilService : IEstabelecimentoPerfilService
{
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IEnderecoGeocodificacaoService _enderecoGeocodificacaoService;
    private readonly IConfirmacaoWhatsAppEstabelecimentoService _confirmacaoWhatsAppEstabelecimentoService;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAvatarBase64Decoder _avatarBase64Decoder;

    public EstabelecimentoPerfilService(
        IEstabelecimentoRepository estabelecimentoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IEnderecoGeocodificacaoService enderecoGeocodificacaoService,
        IConfirmacaoWhatsAppEstabelecimentoService confirmacaoWhatsAppEstabelecimentoService,
        IUsuarioRepository usuarioRepository,
        ICurrentUserContext currentUser,
        IAvatarBase64Decoder avatarBase64Decoder)
    {
        _estabelecimentoRepository = estabelecimentoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _enderecoGeocodificacaoService = enderecoGeocodificacaoService;
        _confirmacaoWhatsAppEstabelecimentoService = confirmacaoWhatsAppEstabelecimentoService;
        _usuarioRepository = usuarioRepository;
        _currentUser = currentUser;
        _avatarBase64Decoder = avatarBase64Decoder;
    }



    public async Task<EstabelecimentoPerfilResponseDto> ObterAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        var estabelecimento = await ObterEstabelecimentoAutorizadoAsync(estabelecimentoId, cancellationToken);
        return EstabelecimentoPerfilResponseDto.From(estabelecimento);
    }



    public async Task<EstabelecimentoPerfilResponseDto> AtualizarAsync(

        int estabelecimentoId,

        AtualizarEstabelecimentoPerfilDto request,

        CancellationToken cancellationToken = default)

    {

        var estabelecimento = await ObterEstabelecimentoAutorizadoAsync(estabelecimentoId, cancellationToken);



        static Exception CriarExcecao(string mensagem) => new EstabelecimentoAssinaturaInvalidoException(mensagem);



        var telefoneAnterior = estabelecimento.Telefone;



        estabelecimento.Nome = OperacaoPerfilValidation.ValidarTextoObrigatorio(request.Nome, "Nome do estabelecimento", 150, CriarExcecao);

        estabelecimento.Logo = OperacaoPerfilValidation.ValidarLogoBase64(
            request.Logo,
            "Logo do estabelecimento",
            _avatarBase64Decoder,
            CriarExcecao);

        estabelecimento.Telefone = TelefoneHelper.NormalizarParaArmazenamento(
            OperacaoPerfilValidation.ValidarTextoObrigatorio(request.Telefone, "Telefone do estabelecimento", 20, CriarExcecao));

        estabelecimento.Email = OperacaoPerfilValidation.ValidarTextoObrigatorio(request.Email, "Email do estabelecimento", 255, CriarExcecao);

        estabelecimento.UpdatedAt = DateTime.UtcNow;



        WhatsAppConfirmacaoEntidade.ResetarAoAlterarTelefone(

            telefoneAnterior,

            estabelecimento.Telefone,

            () =>

            {

                estabelecimento.WhatsAppConfirmadoEm = null;

                estabelecimento.WhatsAppOptIn = false;

                estabelecimento.LimparConfirmacaoWhatsApp();

            });



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

        await _estabelecimentoRepository.SalvarAlteracoesAsync(cancellationToken);



        if (!string.Equals(telefoneAnterior, estabelecimento.Telefone, StringComparison.Ordinal)

            && !string.IsNullOrWhiteSpace(estabelecimento.Telefone))

        {

            var emailsDestino = await ObterEmailsDestinoConfirmacaoWhatsAppAsync(
                estabelecimento,
                cancellationToken);

            await _confirmacaoWhatsAppEstabelecimentoService.IniciarConfirmacaoAsync(
                estabelecimento,
                emailsDestino,
                cancellationToken);

        }



        return EstabelecimentoPerfilResponseDto.From(estabelecimento);

    }



    public async Task<WhatsAppConfirmacaoInstrucoesDto> SolicitarConfirmacaoWhatsAppAsync(

        int estabelecimentoId,

        CancellationToken cancellationToken = default)

    {

        var estabelecimento = await ObterEstabelecimentoAutorizadoAsync(estabelecimentoId, cancellationToken);



        if (estabelecimento.WhatsAppConfirmadoEm.HasValue)

        {

            throw new ConfirmacaoWhatsAppInvalidaException();

        }



        return await _confirmacaoWhatsAppEstabelecimentoService.IniciarConfirmacaoAsync(
            estabelecimento,
            await ObterEmailsDestinoConfirmacaoWhatsAppAsync(estabelecimento, cancellationToken),
            cancellationToken);

    }



    public async Task AtualizarWhatsAppOptInAsync(

        int estabelecimentoId,

        bool optIn,

        CancellationToken cancellationToken = default)

    {

        var estabelecimento = await ObterEstabelecimentoAutorizadoAsync(estabelecimentoId, cancellationToken);



        if (!estabelecimento.WhatsAppConfirmadoEm.HasValue)

        {

            throw new ConfirmacaoWhatsAppInvalidaException();

        }



        estabelecimento.WhatsAppOptIn = optIn;

        estabelecimento.UpdatedAt = DateTime.UtcNow;



        _estabelecimentoRepository.Atualizar(estabelecimento);

        await _estabelecimentoRepository.SalvarAlteracoesAsync(cancellationToken);

    }



    private async Task<Estabelecimento> ObterEstabelecimentoAutorizadoAsync(

        int estabelecimentoId,

        CancellationToken cancellationToken)

    {

        await _autorizacaoNegocioService.AutorizarAsync(

            estabelecimentoId,

            PermissaoNegocio.NegocioEditar,

            cancellationToken);



        var estabelecimento = await _estabelecimentoRepository.ObterPorIdComEnderecoAsync(estabelecimentoId, cancellationToken);

        if (estabelecimento is null || !estabelecimento.Ativo)

        {

            throw new TitularAssinaturaNaoEncontradoException();

        }



        return estabelecimento;

    }

    private async Task<IReadOnlyList<string>> ObterEmailsDestinoConfirmacaoWhatsAppAsync(
        Estabelecimento estabelecimento,
        CancellationToken cancellationToken)
    {
        var emails = new List<string> { estabelecimento.Email };

        if (_currentUser.UserId.HasValue)
        {
            var usuario = await _usuarioRepository.ObterPorIdAsync(_currentUser.UserId.Value, cancellationToken);
            if (usuario is not null)
            {
                emails.Add(usuario.Email);
            }
        }

        return EmailDestinoHelper.Deduplicar(emails);
    }

}


