using GLOWAPI.Application.Models.Agenda;
using GLOWAPI.Domain.Entities;
using GLOWAPI.Infrastructure;
using GLOWAPI.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Tests.Unit.Infrastructure;

public class AgendamentoRepositoryTests
{
    [Fact]
    public async Task ListarAgendaGeralAsync_DeveAplicarIntervaloNoMesmoItemDoAgendamento()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var agendamento = new Agendamento
        {
            Id = 1,
            EstabelecimentoId = 20,
            UsuarioClienteId = 10,
            Itens =
            [
                new AgendamentoItem
                {
                    Id = 1,
                    ServicoId = 1,
                    ProfissionalId = 30,
                    Inicio = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
                    Fim = new DateTime(2026, 5, 1, 11, 0, 0, DateTimeKind.Utc)
                },
                new AgendamentoItem
                {
                    Id = 2,
                    ServicoId = 1,
                    ProfissionalId = 30,
                    Inicio = new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc),
                    Fim = new DateTime(2026, 5, 10, 11, 0, 0, DateTimeKind.Utc)
                }
            ]
        };
        context.Agendamentos.Add(agendamento);
        await context.SaveChangesAsync();

        var repository = new AgendamentoRepository(context);

        var resultado = await repository.ListarAgendaGeralAsync(new AgendaGeralFiltro(
            EstabelecimentoId: 20,
            ProfissionalId: null,
            ClienteId: null,
            Status: null,
            Inicio: new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc),
            Fim: new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc)));

        Assert.Empty(resultado);
    }
}
