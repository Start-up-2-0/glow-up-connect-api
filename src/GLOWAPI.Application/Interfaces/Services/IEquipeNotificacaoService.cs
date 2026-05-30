using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IEquipeNotificacaoService
{
    Task UsuarioEquipeConvidadoAsync(
        EstabelecimentoUsuario vinculo,
        Usuario usuario,
        CancellationToken cancellationToken = default);

    Task ProfissionalConvidadoAsync(
        ProfissionalEstabelecimento vinculo,
        Profissional profissional,
        CancellationToken cancellationToken = default);

    Task RoleUsuarioAlteradaAsync(
        EstabelecimentoUsuario vinculo,
        Usuario usuario,
        EstablishmentUserRole roleAnterior,
        EstablishmentUserRole roleNova,
        CancellationToken cancellationToken = default);

    Task StatusUsuarioAlteradoAsync(
        EstabelecimentoUsuario vinculo,
        Usuario usuario,
        bool ativoAnterior,
        bool ativoNovo,
        CancellationToken cancellationToken = default);

    Task StatusProfissionalAlteradoAsync(
        ProfissionalEstabelecimento vinculo,
        Profissional profissional,
        bool ativoAnterior,
        bool ativoNovo,
        CancellationToken cancellationToken = default);
}
