using GLOWAPI.Application.DTOs.Clientes;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Domain.Enums;

namespace GLOWAPI.Application.Services;

public class ClienteNegocioService : IClienteNegocioService
{
    private readonly IAgendamentoRepository _agendamentoRepository;
    private readonly IAutorizacaoNegocioService _autorizacaoNegocioService;

    public ClienteNegocioService(
        IAgendamentoRepository agendamentoRepository,
        IAutorizacaoNegocioService autorizacaoNegocioService)
    {
        _agendamentoRepository = agendamentoRepository;
        _autorizacaoNegocioService = autorizacaoNegocioService;
    }

    public async Task<IReadOnlyList<ClienteNegocioResponseDto>> ListarPorEstabelecimentoAsync(
        int estabelecimentoId,
        CancellationToken cancellationToken = default)
    {
        await _autorizacaoNegocioService.AutorizarAsync(
            estabelecimentoId,
            PermissaoNegocio.ClienteVisualizarGeral,
            cancellationToken);

        var clientes = await _agendamentoRepository.ListarClientesResumoPorEstabelecimentoAsync(
            estabelecimentoId,
            cancellationToken);

        return clientes
            .Select(cliente => new ClienteNegocioResponseDto(
                cliente.Nome,
                cliente.Email,
                cliente.Telefone,
                cliente.TotalAgendamentos,
                cliente.UltimoAgendamentoEm))
            .ToList();
    }
}
