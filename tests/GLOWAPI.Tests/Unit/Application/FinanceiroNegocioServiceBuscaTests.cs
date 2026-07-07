using GLOWAPI.Application.DTOs.Financeiro;
using GLOWAPI.Application.Interfaces.Repositories;
using GLOWAPI.Application.Interfaces.Services;
using GLOWAPI.Application.Models.Autorizacao;
using GLOWAPI.Application.Services;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using Moq;
using Xunit;

namespace GLOWAPI.Tests.Unit.Application;

public class FinanceiroNegocioServiceBuscaTests
{
    [Fact]
    public async Task BuscarAsync_ComTermoCurto_RetornaVazio()
    {
        var autorizacao = new Mock<IAutorizacaoNegocioService>();
        autorizacao
            .Setup(s => s.AutorizarAsync(It.IsAny<int>(), It.IsAny<PermissaoNegocio>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutorizacaoNegocioResultado(
                1,
                10,
                EstablishmentUserRole.Owner,
                false,
                new HashSet<PermissaoNegocio> { PermissaoNegocio.CaixaVisualizar }));

        var service = new FinanceiroNegocioService(
            new Mock<ICaixaRepository>().Object,
            new Mock<ILancamentoCaixaRepository>().Object,
            new Mock<IComissaoProfissionalRepository>().Object,
            new Mock<IProfissionalEstabelecimentoRepository>().Object,
            new Mock<IContaReceberRepository>().Object,
            new Mock<IContaPagarRepository>().Object,
            new Mock<IConciliacaoItemRepository>().Object,
            new Mock<IMovimentacaoCaixaService>().Object,
            new Mock<IAgendamentoRepository>().Object,
            autorizacao.Object,
            new Mock<IAuditoriaNegocioService>().Object,
            new Mock<ICurrentUserContext>().Object);

        var resultado = await service.BuscarAsync(1, "a", null);

        Assert.Empty(resultado.Lancamentos);
        Assert.Empty(resultado.ContasReceber);
        Assert.Empty(resultado.ContasPagar);
    }
}
