using System.Globalization;
using System.Text.Json;
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Mensageria;
using GLOWAPI.Application.Models.Auth;
using GLOWAPI.Application.Options;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Domain.Exceptions.Auth;
using GLOWAPI.Domain.Exceptions.Usuario;
using Microsoft.Extensions.Options;

namespace GLOWAPI.Application.Services;

public class ExclusaoContaService : IExclusaoContaService
{
    private static readonly AgendamentoStatus[] StatusAgendamentoEncerrados =
    [
        AgendamentoStatus.Cancelado,
        AgendamentoStatus.Concluido,
        AgendamentoStatus.Expirado,
        AgendamentoStatus.Reembolsado,
        AgendamentoStatus.NaoCompareceu
    ];

    private readonly ICurrentUserContext _currentUser;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthSessionService _authSessionService;
    private readonly IAuthService _authService;
    private readonly IEstabelecimentoUsuarioRepository _estabelecimentoUsuarioRepository;
    private readonly IAssinaturaRepository _assinaturaRepository;
    private readonly IAssinaturaVisibilidadeService _assinaturaVisibilidadeService;
    private readonly IAssinaturaEncerramentoService _assinaturaEncerramentoService;
    private readonly IAssinaturaHistoricoService _assinaturaHistoricoService;
    private readonly IEstabelecimentoRepository _estabelecimentoRepository;
    private readonly IAgendamentoRepository _agendamentoRepository;
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;
    private readonly ExclusaoContaOptions _exclusaoOptions;
    private readonly AuthOptions _authOptions;

    public ExclusaoContaService(
        ICurrentUserContext currentUser,
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IAuthSessionService authSessionService,
        IAuthService authService,
        IEstabelecimentoUsuarioRepository estabelecimentoUsuarioRepository,
        IAssinaturaRepository assinaturaRepository,
        IAssinaturaVisibilidadeService assinaturaVisibilidadeService,
        IAssinaturaEncerramentoService assinaturaEncerramentoService,
        IAssinaturaHistoricoService assinaturaHistoricoService,
        IEstabelecimentoRepository estabelecimentoRepository,
        IAgendamentoRepository agendamentoRepository,
        IMensagemNotificacaoService mensagemNotificacaoService,
        IOptions<ExclusaoContaOptions> exclusaoOptions,
        IOptions<AuthOptions> authOptions)
    {
        _currentUser = currentUser;
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _authSessionService = authSessionService;
        _authService = authService;
        _estabelecimentoUsuarioRepository = estabelecimentoUsuarioRepository;
        _assinaturaRepository = assinaturaRepository;
        _assinaturaVisibilidadeService = assinaturaVisibilidadeService;
        _assinaturaEncerramentoService = assinaturaEncerramentoService;
        _assinaturaHistoricoService = assinaturaHistoricoService;
        _estabelecimentoRepository = estabelecimentoRepository;
        _agendamentoRepository = agendamentoRepository;
        _mensagemNotificacaoService = mensagemNotificacaoService;
        _exclusaoOptions = exclusaoOptions.Value;
        _authOptions = authOptions.Value;
    }

    public async Task SolicitarAsync(string senha, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new UnauthorizedException();
        }

        if (string.IsNullOrWhiteSpace(senha))
        {
            throw new InvalidCredentialsException();
        }

        var usuario = await _usuarioRepository.ObterPorIdAsync(_currentUser.UserId.Value, cancellationToken);
        if (usuario is null)
        {
            throw new UsuarioNaoEncontradoException();
        }

        if (!_passwordHasher.Verify(senha, usuario.Senha))
        {
            throw new InvalidCredentialsException();
        }

        if (usuario.ExclusaoPendenteDentroDoPrazo(DateTime.UtcNow))
        {
            return;
        }

        var agora = DateTime.UtcNow;
        usuario.ExclusaoStatus = ExclusaoStatus.Pendente;
        usuario.ExclusaoSolicitadaEm = agora;
        usuario.ExclusaoEfetivarEm = agora.AddDays(_exclusaoOptions.DiasCarencia);
        usuario.UpdatedAt = agora;
        _usuarioRepository.Atualizar(usuario);

        await SuspenderAssinaturasDoOwnerAsync(usuario.Id, cancellationToken);
        await CancelarAgendamentosFuturosDoClienteAsync(usuario.Id, cancellationToken);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        await _authSessionService.RevogarTodasSessoesDoUsuarioAsync(usuario.Id, cancellationToken);
        await EnviarEmailExclusaoAsync(usuario, cancellationToken);
    }

    public async Task<AuthLoginResult> ReativarAsync(
        string email,
        string senha,
        AuthSessionContext context,
        CancellationToken cancellationToken = default)
    {
        var emailNormalizado = ConfirmacaoEmailService.NormalizarEmail(email);
        var usuario = await _usuarioRepository.ObterPorEmailAsync(emailNormalizado, cancellationToken);
        if (usuario is null || !_passwordHasher.Verify(senha, usuario.Senha))
        {
            throw new InvalidCredentialsException();
        }

        if (usuario.ExclusaoStatus != ExclusaoStatus.Pendente
            || !usuario.ExclusaoPendenteDentroDoPrazo(DateTime.UtcNow))
        {
            throw new InactiveUserException();
        }

        usuario.ExclusaoStatus = ExclusaoStatus.Nenhuma;
        usuario.ExclusaoSolicitadaEm = null;
        usuario.ExclusaoEfetivarEm = null;
        usuario.UpdatedAt = DateTime.UtcNow;
        _usuarioRepository.Atualizar(usuario);

        await RestaurarAssinaturasDoOwnerAsync(usuario.Id, cancellationToken);
        await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);

        return await _authService.LoginAsync(
            new DTOs.Auth.LoginRequestDto { Email = emailNormalizado, Senha = senha },
            context,
            cancellationToken);
    }

    public async Task<int> EfetivarVencidasAsync(CancellationToken cancellationToken = default)
    {
        var agora = DateTime.UtcNow;
        var vencidas = await _usuarioRepository.ListarExclusoesPendentesVencidasAsync(agora, cancellationToken);
        var efetivadas = 0;

        foreach (var usuario in vencidas)
        {
            await EncerrarAssinaturasDoOwnerAsync(usuario.Id, cancellationToken);
            await DesativarLojasDoOwnerAsync(usuario.Id, cancellationToken);
            AnonimizarTitular(usuario);
            _usuarioRepository.Atualizar(usuario);
            await _authSessionService.RevogarTodasSessoesDoUsuarioAsync(usuario.Id, cancellationToken);
            efetivadas++;
        }

        if (efetivadas > 0)
        {
            await _usuarioRepository.SalvarAlteracoesAsync(cancellationToken);
        }

        return efetivadas;
    }

    private async Task SuspenderAssinaturasDoOwnerAsync(int usuarioId, CancellationToken cancellationToken)
    {
        var vinculos = await _estabelecimentoUsuarioRepository.ListarAtivosPorUsuarioAsync(usuarioId, cancellationToken);
        foreach (var vinculo in vinculos.Where(v => v.RoleNoEstabelecimento == EstablishmentUserRole.Owner))
        {
            var assinatura = await _assinaturaRepository.ObterAtualPorEstabelecimentoAsync(
                vinculo.EstabelecimentoId,
                cancellationToken);
            if (assinatura is null
                || assinatura.Status is AssinaturaStatus.Cancelada or AssinaturaStatus.Expirada)
            {
                continue;
            }

            var statusAnterior = assinatura.Status;
            assinatura.StatusAntesExclusao = statusAnterior;
            assinatura.Status = AssinaturaStatus.Suspensa;
            assinatura.RenovacaoAutomatica = false;
            assinatura.UpdatedAt = DateTime.UtcNow;
            _assinaturaRepository.Atualizar(assinatura);

            await _assinaturaVisibilidadeService.OcultarLojasVinculadasAsync(assinatura, cancellationToken);
            await _assinaturaHistoricoService.RegistrarAssinaturaAsync(
                assinatura,
                "AssinaturaSuspensaExclusaoConta",
                statusAnterior,
                assinatura.Status,
                observacao: "Assinatura suspensa pelo pedido de exclusao de conta.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task RestaurarAssinaturasDoOwnerAsync(int usuarioId, CancellationToken cancellationToken)
    {
        var vinculos = await _estabelecimentoUsuarioRepository.ListarAtivosPorUsuarioAsync(usuarioId, cancellationToken);
        foreach (var vinculo in vinculos.Where(v => v.RoleNoEstabelecimento == EstablishmentUserRole.Owner))
        {
            var assinatura = await _assinaturaRepository.ObterAtualPorEstabelecimentoAsync(
                vinculo.EstabelecimentoId,
                cancellationToken);
            if (assinatura is null || assinatura.Status != AssinaturaStatus.Suspensa)
            {
                continue;
            }

            var statusAnterior = assinatura.Status;
            var restaurado = assinatura.StatusAntesExclusao ?? AssinaturaStatus.Ativa;
            if (restaurado is AssinaturaStatus.Cancelada or AssinaturaStatus.Expirada or AssinaturaStatus.Suspensa)
            {
                restaurado = AssinaturaStatus.Ativa;
            }

            assinatura.Status = restaurado;
            assinatura.StatusAntesExclusao = null;
            assinatura.RenovacaoAutomatica = true;
            assinatura.UpdatedAt = DateTime.UtcNow;
            _assinaturaRepository.Atualizar(assinatura);

            await _assinaturaVisibilidadeService.ReexibirLojasVinculadasAsync(assinatura, cancellationToken);
            await _assinaturaHistoricoService.RegistrarAssinaturaAsync(
                assinatura,
                "AssinaturaRestauradaExclusaoConta",
                statusAnterior,
                assinatura.Status,
                observacao: "Assinatura restaurada apos reativacao da conta.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task EncerrarAssinaturasDoOwnerAsync(int usuarioId, CancellationToken cancellationToken)
    {
        var vinculos = await _estabelecimentoUsuarioRepository.ListarAtivosPorUsuarioAsync(usuarioId, cancellationToken);
        foreach (var vinculo in vinculos.Where(v => v.RoleNoEstabelecimento == EstablishmentUserRole.Owner))
        {
            var assinatura = await _assinaturaRepository.ObterAtualPorEstabelecimentoAsync(
                vinculo.EstabelecimentoId,
                cancellationToken);
            if (assinatura is null)
            {
                continue;
            }

            await _assinaturaEncerramentoService.EncerrarAsync(
                assinatura,
                AssinaturaStatus.Cancelada,
                "AssinaturaEncerradaExclusaoConta",
                "Assinatura encerrada apos o prazo de exclusao de conta.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task DesativarLojasDoOwnerAsync(int usuarioId, CancellationToken cancellationToken)
    {
        var vinculos = await _estabelecimentoUsuarioRepository.ListarAtivosPorUsuarioAsync(usuarioId, cancellationToken);
        foreach (var vinculo in vinculos.Where(v => v.RoleNoEstabelecimento == EstablishmentUserRole.Owner))
        {
            var estabelecimento = await _estabelecimentoRepository.ObterPorIdAsync(
                vinculo.EstabelecimentoId,
                cancellationToken);
            if (estabelecimento is null || !estabelecimento.Ativo)
            {
                continue;
            }

            estabelecimento.Ativo = false;
            estabelecimento.VisivelPublicamente = false;
            estabelecimento.UpdatedAt = DateTime.UtcNow;
            _estabelecimentoRepository.Atualizar(estabelecimento);
        }
    }

    private async Task CancelarAgendamentosFuturosDoClienteAsync(int usuarioId, CancellationToken cancellationToken)
    {
        var agendamentos = await _agendamentoRepository.ListarPorUsuarioClienteAsync(usuarioId, cancellationToken);
        var agora = DateTime.UtcNow;
        var alterado = false;

        foreach (var agendamento in agendamentos)
        {
            if (agendamento.Inicio <= agora || StatusAgendamentoEncerrados.Contains(agendamento.Status))
            {
                continue;
            }

            agendamento.Status = AgendamentoStatus.Cancelado;
            agendamento.CanceladoEm = agora;
            agendamento.UpdatedAt = agora;
            if (string.IsNullOrWhiteSpace(agendamento.Observacao))
            {
                agendamento.Observacao = "Cancelado automaticamente pelo pedido de exclusao de conta.";
            }

            _agendamentoRepository.Atualizar(agendamento);
            alterado = true;
        }

        if (alterado)
        {
            await _agendamentoRepository.SalvarAlteracoesAsync(cancellationToken);
        }
    }

    private static void AnonimizarTitular(Usuario usuario)
    {
        usuario.Nome = "Titular removido";
        usuario.Email = $"deleted+{usuario.Id}@invalid.local";
        usuario.Telefone = string.Empty;
        usuario.AvatarBase64 = null;
        usuario.CodigoAgendamento = null;
        usuario.WhatsAppOptIn = false;
        usuario.WhatsAppConfirmadoEm = null;
        usuario.LimparConfirmacaoEmail();
        usuario.LimparConfirmacaoWhatsApp();
        usuario.Ativo = false;
        usuario.ExclusaoStatus = ExclusaoStatus.Concluida;
        usuario.UpdatedAt = DateTime.UtcNow;
    }

    private async Task EnviarEmailExclusaoAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(usuario.Email) || !usuario.ExclusaoEfetivarEm.HasValue)
        {
            return;
        }

        var limite = usuario.ExclusaoEfetivarEm.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR"));
        var link = $"{_authOptions.FrontendBaseUrl.TrimEnd('/')}/auth/conta-em-exclusao?email={Uri.EscapeDataString(usuario.Email)}";
        var assunto = "Exclusão de conta solicitada";

        await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.Email,
            Destinatario = usuario.Email.Trim(),
            Assunto = assunto,
            Conteudo = TransacionalEmailTemplate.Criar(
                assunto,
                "Você tem 30 dias para reativar sua conta.",
                [
                    "Recebemos o pedido de exclusão da sua conta no Glow Up Connect.",
                    $"A conta, a loja e a assinatura ficarão indisponíveis. Você pode reativar até {limite} (UTC).",
                    "Após essa data, os dados pessoais serão anonimizados e a conta não poderá ser recuperada."
                ],
                botao: new EmailTemplateBotao
                {
                    Texto = "Reativar conta",
                    Url = link,
                    Estilo = EmailTemplateBotaoEstilo.Link
                },
                linkFallback: link),
            Prioridade = 1,
            PayloadJson = JsonSerializer.Serialize(new
            {
                evento = "exclusao-conta-solicitada",
                usuarioId = usuario.Id,
                efetivarEm = usuario.ExclusaoEfetivarEm
            })
        }, cancellationToken);
    }
}
