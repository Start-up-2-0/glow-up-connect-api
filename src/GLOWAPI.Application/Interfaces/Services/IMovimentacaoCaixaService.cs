using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Caixa;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Exceptions.Negocios;

namespace GLOWAPI.Application.Interfaces.Services;

public interface IMovimentacaoCaixaService
{
    Task<LancamentoCaixa> RegistrarLancamentoAsync(
        int estabelecimentoId,
        RegistrarLancamentoCaixaComando comando,
        CancellationToken cancellationToken = default);

    Task RecalcularSaldosAsync(
        int caixaId,
        CancellationToken cancellationToken = default);
}
