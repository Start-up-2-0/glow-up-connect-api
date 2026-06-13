using System.Text.Json;
using GLOWAPI.Application.DTOs.Mensageria;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Mensageria;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Services;

public class EquipeNotificacaoService : IEquipeNotificacaoService
{
    private readonly IMensagemNotificacaoService _mensagemNotificacaoService;

    public EquipeNotificacaoService(IMensagemNotificacaoService mensagemNotificacaoService)
    {
        _mensagemNotificacaoService = mensagemNotificacaoService;
    }

    public Task UsuarioEquipeConvidadoAsync(
        EstabelecimentoUsuario vinculo,
        Usuario usuario,
        CancellationToken cancellationToken = default) =>
        EnfileirarAsync(
            vinculo.EstabelecimentoId,
            usuario.Email,
            "Voce recebeu acesso ao negocio",
            $"Ola {usuario.Nome}, seu acesso ao negocio foi configurado como {vinculo.RoleNoEstabelecimento}.",
            "usuario-equipe-convidado",
            new
            {
                vinculo.Id,
                vinculo.UsuarioId,
                role = vinculo.RoleNoEstabelecimento.ToString(),
                vinculo.Ativo
            },
            cancellationToken);

    public Task ProfissionalConvidadoAsync(
        ProfissionalEstabelecimento vinculo,
        Profissional profissional,
        CancellationToken cancellationToken = default) =>
        EnfileirarAsync(
            vinculo.EstabelecimentoId,
            profissional.Email,
            "Voce foi vinculado como profissional",
            $"Ola {profissional.NomePublico}, voce foi vinculado como profissional e pode receber agendamentos conforme a configuracao do negocio.",
            "profissional-convidado",
            new
            {
                vinculo.Id,
                vinculo.ProfissionalId,
                vinculo.PodeReceberAgendamento,
                vinculo.Ativo
            },
            cancellationToken);

    public Task RoleUsuarioAlteradaAsync(
        EstabelecimentoUsuario vinculo,
        Usuario usuario,
        EstablishmentUserRole roleAnterior,
        EstablishmentUserRole roleNova,
        CancellationToken cancellationToken = default) =>
        EnfileirarAsync(
            vinculo.EstabelecimentoId,
            usuario.Email,
            "Seu nivel de acesso foi alterado",
            $"Ola {usuario.Nome}, seu nivel de acesso foi alterado de {roleAnterior} para {roleNova}.",
            "usuario-equipe-role-alterada",
            new
            {
                vinculo.Id,
                vinculo.UsuarioId,
                roleAnterior = roleAnterior.ToString(),
                roleNova = roleNova.ToString()
            },
            cancellationToken);

    public Task StatusUsuarioAlteradoAsync(
        EstabelecimentoUsuario vinculo,
        Usuario usuario,
        bool ativoAnterior,
        bool ativoNovo,
        CancellationToken cancellationToken = default)
    {
        var assunto = ativoNovo
            ? "Seu acesso ao negocio foi reativado"
            : "Seu acesso ao negocio foi removido";
        var conteudo = ativoNovo
            ? $"Ola {usuario.Nome}, seu acesso ao negocio foi reativado."
            : $"Ola {usuario.Nome}, seu acesso ao negocio foi removido.";

        return EnfileirarAsync(
            vinculo.EstabelecimentoId,
            usuario.Email,
            assunto,
            conteudo,
            "usuario-equipe-status-alterado",
            new
            {
                vinculo.Id,
                vinculo.UsuarioId,
                ativoAnterior,
                ativoNovo,
                role = vinculo.RoleNoEstabelecimento.ToString()
            },
            cancellationToken);
    }

    public Task StatusProfissionalAlteradoAsync(
        ProfissionalEstabelecimento vinculo,
        Profissional profissional,
        bool ativoAnterior,
        bool ativoNovo,
        CancellationToken cancellationToken = default)
    {
        var assunto = ativoNovo
            ? "Seu vinculo profissional foi reativado"
            : "Seu vinculo profissional foi removido";
        var conteudo = ativoNovo
            ? $"Ola {profissional.NomePublico}, seu vinculo profissional foi reativado no negocio."
            : $"Ola {profissional.NomePublico}, seu vinculo profissional foi removido do negocio.";

        return EnfileirarAsync(
            vinculo.EstabelecimentoId,
            profissional.Email,
            assunto,
            conteudo,
            "profissional-status-alterado",
            new
            {
                vinculo.Id,
                vinculo.ProfissionalId,
                ativoAnterior,
                ativoNovo,
                vinculo.PodeReceberAgendamento
            },
            cancellationToken);
    }

    private async Task EnfileirarAsync(
        int estabelecimentoId,
        string? destinatario,
        string assunto,
        string conteudo,
        string evento,
        object payload,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(destinatario))
        {
            return;
        }

        await _mensagemNotificacaoService.RegistrarAsync(new RegistrarMensagemNotificacaoDto
        {
            Canal = CanalMensagemNotificacao.Email,
            Destinatario = destinatario.Trim(),
            Assunto = assunto,
            Conteudo = TransacionalEmailTemplate.Criar(assunto, assunto, [conteudo]),
            EstabelecimentoId = estabelecimentoId,
            Prioridade = 2,
            PayloadJson = JsonSerializer.Serialize(new
            {
                evento,
                dados = payload
            })
        }, cancellationToken);
    }
}
