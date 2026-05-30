using GLOWAPI.Application.Models.Autorizacao;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IProfissionalEscopoAcessoService
{
    Task<EscopoProfissionalResultado> ObterEscopoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default);

    Task<EscopoProfissionalResultado> AutorizarAgendamentoItemAsync(
        int estabelecimentoId,
        int agendamentoItemId,
        CancellationToken cancellationToken = default);

    Task<EscopoProfissionalResultado> AutorizarClienteAsync(
        int estabelecimentoId,
        int usuarioClienteId,
        CancellationToken cancellationToken = default);
}
