using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Mensageria;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class MensagemNotificacaoService : IMensagemNotificacaoService
{
    private readonly IMensagemNotificacaoRepository _repository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly MensageriaOptions _options;

    public MensagemNotificacaoService(
        IMensagemNotificacaoRepository repository,
        IUsuarioRepository usuarioRepository,
        IEstabelecimentoRepository estabelecimentoRepository,
        IOptions<MensageriaOptions> options)
    {
        _repository = repository;
        _usuarioRepository = usuarioRepository;
        _estabelecimentoRepository = estabelecimentoRepository;
        _options = options.Value;
    }

    public async Task<MensagemNotificacaoResponseDto> RegistrarAsync(
        RegistrarMensagemNotificacaoDto dto,
        CancellationToken cancellationToken = default)
    {
        await ValidarWhatsAppAsync(dto, cancellationToken);
        var utcNow = DateTime.UtcNow;

        var mensagem = new MensagemNotificacao
        {
            Guid = Guid.NewGuid(),
            EstabelecimentoId = dto.EstabelecimentoId,
            UsuarioId = dto.UsuarioId,
            EhVerificacaoWhatsApp = dto.EhVerificacaoWhatsApp,
            Canal = dto.Canal,
            Destinatario = dto.Destinatario.Trim(),
            Assunto = dto.Assunto?.Trim() ?? string.Empty,
            Conteudo = dto.Conteudo,
            PayloadJson = string.IsNullOrWhiteSpace(dto.PayloadJson) ? "{}" : dto.PayloadJson,
            Status = StatusMensagemNotificacao.Pendente,
            MaximoTentativas = dto.MaximoTentativas ?? _options.MaximoTentativasPadrao,
            Prioridade = dto.Prioridade,
            Provedor = dto.Provedor,
            AgendadoPara = dto.AgendadoPara,
            CriadoEm = utcNow
        };

        await _repository.AdicionarAsync(mensagem, cancellationToken);
        await _repository.SalvarAlteracoesAsync(cancellationToken);

        return MensagemNotificacaoResponseDto.From(mensagem);
    }

    public async Task<MensagemNotificacaoResponseDto> RegistrarEnviadoAsync(
        RegistrarMensagemNotificacaoDto dto,
        string provedor,
        CancellationToken cancellationToken = default)
    {
        await ValidarWhatsAppAsync(dto, cancellationToken);
        var utcNow = DateTime.UtcNow;

        var mensagem = new MensagemNotificacao
        {
            Guid = Guid.NewGuid(),
            EstabelecimentoId = dto.EstabelecimentoId,
            UsuarioId = dto.UsuarioId,
            EhVerificacaoWhatsApp = dto.EhVerificacaoWhatsApp,
            Canal = dto.Canal,
            Destinatario = dto.Destinatario.Trim(),
            Assunto = dto.Assunto?.Trim() ?? string.Empty,
            Conteudo = dto.Conteudo,
            PayloadJson = string.IsNullOrWhiteSpace(dto.PayloadJson) ? "{}" : dto.PayloadJson,
            MaximoTentativas = dto.MaximoTentativas ?? _options.MaximoTentativasPadrao,
            Prioridade = dto.Prioridade,
            Provedor = string.IsNullOrWhiteSpace(provedor) ? dto.Provedor : provedor,
            AgendadoPara = dto.AgendadoPara,
            CriadoEm = utcNow
        };

        mensagem.MarcarEnviada(utcNow);

        await _repository.AdicionarAsync(mensagem, cancellationToken);
        await _repository.SalvarAlteracoesAsync(cancellationToken);

        return MensagemNotificacaoResponseDto.From(mensagem);
    }

    public async Task CancelarPorGuidAsync(Guid guid, CancellationToken cancellationToken = default)
    {
        var mensagem = await _repository.ObterPorGuidAsync(guid, cancellationToken);
        if (mensagem is null)
        {
            throw new MensagemNotificacaoNaoEncontradaException();
        }

        if (mensagem.Status == StatusMensagemNotificacao.Enviado)
        {
            throw new MensagemNotificacaoJaEnviadaException();
        }

        if (!mensagem.PodeSerCancelada())
        {
            throw new MensagemNotificacaoNaoCancelavelException();
        }

        mensagem.Cancelar(DateTime.UtcNow);
        _repository.Atualizar(mensagem);
        await _repository.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task ValidarWhatsAppAsync(RegistrarMensagemNotificacaoDto dto, CancellationToken cancellationToken)
    {
        if (dto.Canal != CanalMensagemNotificacao.WhatsApp || dto.EhVerificacaoWhatsApp) return;

        if (dto.UsuarioId.HasValue)
        {
            var usuario = await _usuarioRepository.ObterPorIdAsync(dto.UsuarioId.Value, cancellationToken);
            if (usuario?.PodeReceberAlertasWhatsApp() == true
                && Helpers.TelefoneHelper.SaoEquivalentes(usuario.Telefone, dto.Destinatario)) return;
        }
        else if (dto.EstabelecimentoId.HasValue)
        {
            var estabelecimento = await _estabelecimentoRepository.ObterPorIdAsync(dto.EstabelecimentoId.Value, cancellationToken);
            if (estabelecimento?.PodeReceberAlertasWhatsApp() == true
                && Helpers.TelefoneHelper.SaoEquivalentes(estabelecimento.Telefone, dto.Destinatario)) return;
        }

        throw new InvalidOperationException("WhatsApp nao confirmado ou destinatario sem entidade identificada.");
    }
}
