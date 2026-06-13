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

        var tokenConfirmacao = TelefoneHelper.GerarTokenConfirmacao(estabelecimento.Telefone);

        estabelecimento.WhatsAppConfirmacaoTokenHash = _tokenService.HashToken(tokenConfirmacao);
        estabelecimento.WhatsAppConfirmacaoCodigoHash = null;
        estabelecimento.WhatsAppConfirmacaoExpiraEm = null;
        estabelecimento.UpdatedAt = DateTime.UtcNow;

        _estabelecimentoRepository.Atualizar(estabelecimento);
        await _estabelecimentoRepository.SalvarAlteracoesAsync(cancellationToken);

        var instrucoes = ConfirmacaoWhatsAppInstrucoesBuilder.Criar(
            _whatsAppOptions,
            _authOptions,
            estabelecimento.Telefone,
            whatsAppEnviado: false,
            emailEnviado: false);

        instrucoes.WhatsAppEnviado = await EnviarWhatsAppConfirmacaoAsync(
            estabelecimento.Telefone,
            instrucoes.LinkConfirmacao,
            estabelecimento.Id,
            cancellationToken);

        instrucoes.EmailEnviado = await EnviarEmailsConfirmacaoAsync(
            emailsDestino,
            estabelecimento.Nome,
            estabelecimento.Telefone,
            instrucoes,
            estabelecimento.Id,
            cancellationToken);

        return instrucoes;
    }

    public Task ConfirmarPorCodigoAsync(
        int estabelecimentoId,
        string codigo,
        CancellationToken cancellationToken = default) =>
        throw new ConfirmacaoWhatsAppInvalidaException();

    public Task ConfirmarPorTokenAsync(string token, CancellationToken cancellationToken = default) =>
        throw new ConfirmacaoWhatsAppInvalidaException();

    public async Task<WhatsAppConfirmacaoInboundResultado> TentarConfirmarPorMensagemInboundAsync(
        string telefoneRemetente,
        string textoMensagem,
        CancellationToken cancellationToken = default)
    {
        var resultadoPorCodigo = await TentarConfirmarPorCodigoLegadoAsync(
            telefoneRemetente,
            textoMensagem,
            cancellationToken);

        if (resultadoPorCodigo is not null)
        {
            return resultadoPorCodigo;
        }

        var estabelecimentoPorToken = await BuscarEstabelecimentoPorTokenNaMensagemAsync(textoMensagem, cancellationToken);
        if (estabelecimentoPorToken is not null)
        {
            return await ProcessarConfirmacaoInboundDoEstabelecimentoAsync(estabelecimentoPorToken, cancellationToken);
        }

        if (!ConfirmacaoWhatsAppTokenHelper.MensagemContemTokenConfirmacao(textoMensagem, telefoneRemetente))
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(WhatsAppConfirmacaoInboundMotivoIgnorado.CodigoInvalido);
        }

        var estabelecimento = await BuscarEstabelecimentoPorTelefoneRemetenteAsync(telefoneRemetente, cancellationToken);
        if (estabelecimento is null)
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(WhatsAppConfirmacaoInboundMotivoIgnorado.EntidadeNaoEncontrada);
        }

        if (!TelefoneHelper.SaoEquivalentes(estabelecimento.Telefone, telefoneRemetente))
        {
            return WhatsAppConfirmacaoInboundResultado.Ignorado(
                WhatsAppConfirmacaoInboundMotivoIgnorado.CodigoInvalido,
                estabelecimento.Nome,
                ObterTelefoneCadastradoNormalizado(estabelecimento),
                estabelecimento.Email,
                estabelecimento.Id);
        }

        return await ProcessarConfirmacaoInboundDoEstabelecimentoAsync(estabelecimento, cancellationToken);
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

        var estabelecimentoPorToken = await BuscarEstabelecimentoPorTokenNaMensagemAsync(textoMensagem, cancellationToken);
        if (estabelecimentoPorToken is not null)
        {
            return new WhatsAppConfirmacaoInboundRespostaDestino(
                ObterTelefoneCadastradoNormalizado(estabelecimentoPorToken),
                estabelecimentoPorToken.Nome,
                EstabelecimentoId: estabelecimentoPorToken.Id);
        }

        if (!ConfirmacaoWhatsAppTokenHelper.MensagemContemTokenConfirmacao(textoMensagem, telefoneRemetente))
        {
            return null;
        }

        var estabelecimento = await BuscarEstabelecimentoPorTelefoneRemetenteAsync(telefoneRemetente, cancellationToken);
        if (estabelecimento is null)
        {
            var telefoneNormalizado = TelefoneHelper.NormalizarParaWhatsApp(telefoneRemetente);
            if (string.IsNullOrWhiteSpace(telefoneNormalizado))
            {
                return null;
            }

            return new WhatsAppConfirmacaoInboundRespostaDestino(telefoneNormalizado, null);
        }

        return new WhatsAppConfirmacaoInboundRespostaDestino(
            ObterTelefoneCadastradoNormalizado(estabelecimento),
            estabelecimento.Nome,
            EstabelecimentoId: estabelecimento.Id);
    }

    private async Task<bool> EnviarWhatsAppConfirmacaoAsync(
        string telefoneDestino,
        string linkConfirmacao,
        int estabelecimentoId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(telefoneDestino) || string.IsNullOrWhiteSpace(linkConfirmacao))
        {
            return false;
        }

        var conteudo = ConfirmacaoWhatsAppTemplate.MensagemOutboundConfirmacao(linkConfirmacao);

        await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.WhatsApp,
            Destinatario = TelefoneHelper.NormalizarParaWhatsApp(telefoneDestino),
            Assunto = "Confirmacao WhatsApp comercial Glow Up Connect",
            Conteudo = conteudo,
            EstabelecimentoId = estabelecimentoId,
            Prioridade = 2
        }, cancellationToken);

        return true;
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
        if (destinatarios.Count == 0
            || string.IsNullOrWhiteSpace(instrucoes.LinkConfirmacao)
            || string.IsNullOrWhiteSpace(instrucoes.LinkWhatsApp))
        {
            return false;
        }

        var conteudo = ConfirmacaoWhatsAppEmailTemplate.Criar(
            nomeDestinatario,
            telefonePerfil,
            instrucoes.LinkConfirmacao,
            instrucoes.LinkWhatsApp);

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

    private async Task<WhatsAppConfirmacaoInboundResultado?> TentarConfirmarPorCodigoLegadoAsync(
        string telefoneRemetente,
        string textoMensagem,
        CancellationToken cancellationToken)
    {
        if (!ConfirmacaoWhatsAppCodigoHelper.PareceTentativaConfirmacaoPorCodigo(textoMensagem))
        {
            return null;
        }

        var estabelecimentoPorCodigo = await BuscarEstabelecimentoPorCodigoNaMensagemAsync(
            textoMensagem,
            cancellationToken);

        if (estabelecimentoPorCodigo is not null)
        {
            return await ProcessarConfirmacaoInboundDoEstabelecimentoPorCodigoAsync(
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

        return await ProcessarConfirmacaoInboundDoEstabelecimentoPorCodigoAsync(
            estabelecimento,
            textoMensagem,
            cancellationToken);
    }

    private async Task<Estabelecimento?> BuscarEstabelecimentoPorTelefoneRemetenteAsync(
        string telefoneRemetente,
        CancellationToken cancellationToken)
    {
        var telefoneNormalizado = TelefoneHelper.NormalizarParaWhatsApp(telefoneRemetente);
        if (!string.IsNullOrWhiteSpace(telefoneNormalizado))
        {
            var estabelecimento = await _estabelecimentoRepository.ObterPorTelefoneNormalizadoAsync(
                telefoneNormalizado,
                cancellationToken);

            if (estabelecimento is not null)
            {
                return estabelecimento;
            }
        }

        var telefoneInbound = TelefoneHelper.NormalizarParaConfirmacaoInbound(telefoneRemetente);
        if (string.IsNullOrWhiteSpace(telefoneInbound))
        {
            return null;
        }

        return await _estabelecimentoRepository.ObterPorTelefoneNormalizadoAsync(telefoneInbound, cancellationToken);
    }

    private async Task<Estabelecimento?> BuscarEstabelecimentoPorTokenNaMensagemAsync(
        string textoMensagem,
        CancellationToken cancellationToken)
    {
        foreach (var token in ConfirmacaoWhatsAppTokenHelper.ExtrairTokensCandidatos(textoMensagem))
        {
            if (!ConfirmacaoWhatsAppTokenHelper.TokenPareceTelefoneBrasileiro(token))
            {
                continue;
            }

            var hash = _tokenService.HashToken(token);
            var estabelecimento = await _estabelecimentoRepository.ObterPorWhatsAppConfirmacaoTokenHashAsync(
                hash,
                cancellationToken);

            if (estabelecimento is null)
            {
                continue;
            }

            if (!TelefoneHelper.TokenCorrespondeTelefone(token, estabelecimento.Telefone))
            {
                continue;
            }

            return estabelecimento;
        }

        return null;
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

        await ConfirmarEstabelecimentoAsync(estabelecimento, cancellationToken);

        return WhatsAppConfirmacaoInboundResultado.SucessoEstabelecimento(
            estabelecimento.Nome,
            telefoneCadastrado,
            estabelecimento.Id);
    }

    private async Task<WhatsAppConfirmacaoInboundResultado> ProcessarConfirmacaoInboundDoEstabelecimentoPorCodigoAsync(
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
        if (estabelecimento is null)
        {
            throw new ConfirmacaoWhatsAppInvalidaException();
        }

        if (!string.IsNullOrEmpty(estabelecimento.WhatsAppConfirmacaoCodigoHash)
            && (estabelecimento.WhatsAppConfirmacaoExpiraEm is null
                || estabelecimento.WhatsAppConfirmacaoExpiraEm <= DateTime.UtcNow))
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
}
