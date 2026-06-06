using System.Security.Cryptography;
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Helpers;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Mensageria;
using GLOWAPI.Application.Models.Mensageria;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Usuario;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class ConfirmacaoWhatsAppEstabelecimentoService : IConfirmacaoWhatsAppEstabelecimentoService
{
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;
    private readonly IGlowTokenService _tokenService;
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;
    private readonly MensageriaWhatsAppOptions _whatsAppOptions;
    private readonly AuthOptions _authOptions;

    public ConfirmacaoWhatsAppEstabelecimentoService(
        IEstabelecimentoRepository estabelecimentoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService,
        IGlowTokenService tokenService,
        IMensagemNotificacaoService mensagemNotificacaoService,
        IOptions<MensageriaWhatsAppOptions> whatsAppOptions,
        IOptions<AuthOptions> authOptions)
    {
        _estabelecimentoRepository = estabelecimentoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
        _tokenService = tokenService;
        _mensagemNotificacaoService = mensagemNotificacaoService;
        _whatsAppOptions = whatsAppOptions.Value;
        _authOptions = authOptions.Value;
    }

    public async Task<WhatsAppConfirmacaoInstrucoesDto> IniciarConfirmacaoAsync(
        Estabelecimento estabelecimento,
        IReadOnlyList<string> emailsDestino,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(estabelecimento.Telefone))
        {
            throw new ConfirmacaoWhatsAppInvalidaException();
        }

        var tokenPlano = _tokenService.GerarRefreshToken();
        var codigoPlano = GerarCodigoNumerico(_authOptions.ConfirmacaoCodigoDigitos);

        estabelecimento.WhatsAppConfirmacaoTokenHash = _tokenService.HashToken(tokenPlano);
        estabelecimento.WhatsAppConfirmacaoCodigoHash = _tokenService.HashToken(codigoPlano);
        estabelecimento.WhatsAppConfirmacaoExpiraEm = DateTime.UtcNow.AddHours(_authOptions.ConfirmacaoWhatsAppHoras);
        estabelecimento.UpdatedAt = DateTime.UtcNow;

        _estabelecimentoRepository.Atualizar(estabelecimento);
        await _estabelecimentoRepository.SalvarAlteracoesAsync(cancellationToken);

        var instrucoes = ConfirmacaoWhatsAppInstrucoesBuilder.Criar(
            _whatsAppOptions,
            codigoPlano,
            estabelecimento.WhatsAppConfirmacaoExpiraEm.Value,
            emailEnviado: false);

        var emailEnviado = await EnviarEmailsConfirmacaoAsync(
            emailsDestino,
            estabelecimento.Nome,
            estabelecimento.Telefone,
            instrucoes,
            estabelecimento.Id,
            cancellationToken);

        instrucoes.EmailEnviado = emailEnviado;
        return instrucoes;
    }

    public async Task ConfirmarPorCodigoAsync(
        int estabelecimentoId,
        string codigo,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.NegocioEditar,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ConfirmacaoWhatsAppInvalidaException();
        }

        var hash = _tokenService.HashToken(codigo.Trim());
        var estabelecimento = await _estabelecimentoRepository.ObterPorWhatsAppConfirmacaoCodigoHashAsync(hash, cancellationToken);

        if (estabelecimento is null || estabelecimento.Id != estabelecimentoId)
        {
            throw new ConfirmacaoWhatsAppInvalidaException();
        }

        await ConfirmarEstabelecimentoAsync(estabelecimento, cancellationToken);
    }

    public async Task ConfirmarPorTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ConfirmacaoWhatsAppInvalidaException();
        }

        var hash = _tokenService.HashToken(token.Trim());
        var estabelecimento = await _estabelecimentoRepository.ObterPorWhatsAppConfirmacaoTokenHashAsync(hash, cancellationToken);
        await ConfirmarEstabelecimentoAsync(estabelecimento, cancellationToken);
    }

    public async Task<WhatsAppConfirmacaoInboundResultado> TentarConfirmarPorMensagemInboundAsync(
        string telefoneRemetente,
        string textoMensagem,
        CancellationToken cancellationToken = default)
    {
        var estabelecimentoPorCodigo = await BuscarEstabelecimentoPorCodigoNaMensagemAsync(
            textoMensagem,
            cancellationToken);

        if (estabelecimentoPorCodigo is not null)
        {
            return await ProcessarConfirmacaoInboundDoEstabelecimentoAsync(
                estabelecimentoPorCodigo,
                textoMensagem,
                cancellationToken);
        }

        var telefoneNormalizado = TelefoneHelper.NormalizarParaWhatsApp(telefoneRemetente);
        if (string.IsNullOrWhiteSpace(telefoneNormalizado))
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(WhatsAppConfirmacaoInboundMotivoIgnorado.CodigoInvalido);
        }

        var estabelecimento = await _estabelecimentoRepository.ObterPorTelefoneNormalizadoAsync(
            telefoneNormalizado,
            cancellationToken);

        if (estabelecimento is null)
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(WhatsAppConfirmacaoInboundMotivoIgnorado.EntidadeNaoEncontrada);
        }

        return await ProcessarConfirmacaoInboundDoEstabelecimentoAsync(estabelecimento, textoMensagem, cancellationToken);
    }

    public async Task<WhatsAppConfirmacaoInboundRespostaDestino?> ResolverDestinoRespostaInboundAsync(
        string telefoneRemetente,
        string textoMensagem,
        CancellationToken cancellationToken = default)
    {
        var destinoPorCodigo = await ResolverDestinoPorCodigoAsync(textoMensagem, cancellationToken);
        if (destinoPorCodigo is not null)
        {
            return destinoPorCodigo;
        }

        var telefoneNormalizado = TelefoneHelper.NormalizarParaWhatsApp(telefoneRemetente);
        if (string.IsNullOrWhiteSpace(telefoneNormalizado))
        {
            return null;
        }

        return new WhatsAppConfirmacaoInboundRespostaDestino(telefoneNormalizado, null);
    }

    private async Task<bool> EnviarEmailsConfirmacaoAsync(
        IReadOnlyList<string> emailsDestino,
        string nomeDestinatario,
        string telefonePerfil,
        WhatsAppConfirmacaoInstrucoesDto instrucoes,
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        var destinatarios = EmailDestinoHelper.Deduplicar(emailsDestino);
        if (destinatarios.Count == 0 || string.IsNullOrWhiteSpace(instrucoes.LinkWhatsApp))
        {
            return false;
        }

        var conteudo = ConfirmacaoWhatsAppEmailTemplate.Criar(
            nomeDestinatario,
            telefonePerfil,
            instrucoes.LinkWhatsApp,
            instrucoes.CodigoConfirmacao,
            instrucoes.MensagemSugerida,
            _authOptions.ConfirmacaoWhatsAppHoras);

        foreach (var destinatario in destinatarios)
        {
            await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
            {
                Canal = CanalMensagemNotificacao.Email,
                Destinatario = destinatario,
                Assunto = "Confirme o WhatsApp comercial no Glow Up Connect",
                Conteudo = conteudo,
                EstabelecimentoId = estabelecimentoId,
                Prioridade = 2
            }, cancellationToken);
        }

        return true;
    }

    private async Task<WhatsAppConfirmacaoInboundRespostaDestino?> ResolverDestinoPorCodigoAsync(
        string textoMensagem,
        CancellationToken cancellationToken)
    {
        var estabelecimento = await BuscarEstabelecimentoPorCodigoNaMensagemAsync(textoMensagem, cancellationToken);
        if (estabelecimento is null)
        {
            return null;
        }

        var telefoneCadastrado = ObterTelefoneCadastradoNormalizado(estabelecimento);
        if (string.IsNullOrWhiteSpace(telefoneCadastrado))
        {
            return null;
        }

        return new WhatsAppConfirmacaoInboundRespostaDestino(
            telefoneCadastrado,
            estabelecimento.Nome,
            EstabelecimentoId: estabelecimento.Id);
    }

    private async Task<Estabelecimento?> BuscarEstabelecimentoPorCodigoNaMensagemAsync(
        string textoMensagem,
        CancellationToken cancellationToken)
    {
        foreach (var candidato in ConfirmacaoWhatsAppCodigoHelper.ExtrairCandidatosCodigo(
                     textoMensagem,
                     _authOptions.ConfirmacaoCodigoDigitos))
        {
            var hash = _tokenService.HashToken(candidato);
            var estabelecimento = await _estabelecimentoRepository.ObterPorWhatsAppConfirmacaoCodigoHashAsync(
                hash,
                cancellationToken);

            if (estabelecimento is not null)
            {
                return estabelecimento;
            }
        }

        return null;
    }

    private async Task<WhatsAppConfirmacaoInboundResultado> ProcessarConfirmacaoInboundDoEstabelecimentoAsync(
        Estabelecimento estabelecimento,
        string textoMensagem,
        CancellationToken cancellationToken)
    {
        var telefoneCadastrado = ObterTelefoneCadastradoNormalizado(estabelecimento);

        if (estabelecimento.WhatsAppConfirmadoEm.HasValue)
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(
                WhatsAppConfirmacaoInboundMotivoIgnorado.JaConfirmado,
                estabelecimento.Nome,
                telefoneCadastrado,
                estabelecimento.Email,
                estabelecimento.Id);
        }

        if (!estabelecimento.PendenteConfirmacaoWhatsApp())
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(
                WhatsAppConfirmacaoInboundMotivoIgnorado.SemPendencia,
                estabelecimento.Nome,
                telefoneCadastrado,
                estabelecimento.Email,
                estabelecimento.Id);
        }

        if (!ConfirmacaoWhatsAppCodigoHelper.MensagemContemCodigoValido(
                textoMensagem,
                estabelecimento.WhatsAppConfirmacaoCodigoHash,
                _authOptions.ConfirmacaoCodigoDigitos,
                _tokenService))
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(
                WhatsAppConfirmacaoInboundMotivoIgnorado.CodigoInvalido,
                estabelecimento.Nome,
                telefoneCadastrado,
                estabelecimento.Email,
                estabelecimento.Id);
        }

        await ConfirmarEstabelecimentoAsync(estabelecimento, cancellationToken);

        return WhatsAppConfirmacaoInboundResultado.SucessoEstabelecimento(
            estabelecimento.Nome,
            telefoneCadastrado,
            estabelecimento.Id);
    }

    private static string ObterTelefoneCadastradoNormalizado(Estabelecimento estabelecimento) =>
        TelefoneHelper.NormalizarParaWhatsApp(estabelecimento.Telefone) ?? string.Empty;

    private async Task ConfirmarEstabelecimentoAsync(
        Estabelecimento? estabelecimento,
        CancellationToken cancellationToken)
    {
        if (estabelecimento is null
            || estabelecimento.WhatsAppConfirmacaoExpiraEm is null
            || estabelecimento.WhatsAppConfirmacaoExpiraEm <= DateTime.UtcNow)
        {
            throw new ConfirmacaoWhatsAppInvalidaException();
        }

        if (estabelecimento.WhatsAppConfirmadoEm.HasValue)
        {
            return;
        }

        estabelecimento.WhatsAppConfirmadoEm = DateTime.UtcNow;
        estabelecimento.WhatsAppOptIn = true;
        estabelecimento.LimparConfirmacaoWhatsApp();
        estabelecimento.UpdatedAt = DateTime.UtcNow;

        _estabelecimentoRepository.Atualizar(estabelecimento);
        await _estabelecimentoRepository.SalvarAlteracoesAsync(cancellationToken);
    }

    private static string GerarCodigoNumerico(int digitos)
    {
        var max = (int)Math.Pow(10, digitos);
        var valor = RandomNumberGenerator.GetInt32(0, max);
        return valor.ToString($"D{digitos}");
    }
}
