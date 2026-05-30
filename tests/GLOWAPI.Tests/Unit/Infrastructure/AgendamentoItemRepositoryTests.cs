using GLOWAPI.Domain.Entities;
using GLOWAPI.Domain.Enums;
using GLOWAPI.Infrastructure;
using GLOWAPI.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Tests.Unit.Infrastructure;

public class AgendamentoItemRepositoryTests
{
    [Fact]
    public async Task ExisteAgendamentoFuturoImpactadoPorAlteracaoHorarioAsync_DeveDetectarItemQueFicariaForaDoNovoHorario()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.Agendamentos.Add(new Agendamento
        {
            Id = 1,
            EstabelecimentoId = 20,
            UsuarioClienteId = 10,
            Status = AgendamentoStatus.Confirmado,
            Itens =
            [
                new AgendamentoItem
                {
                    Id = 1,
                    ServicoId = 1,
                    ProfissionalId = 40,
                    Inicio = ProximaData(DayOfWeek.Monday, new TimeOnly(9, 30)),
                    Fim = ProximaData(DayOfWeek.Monday, new TimeOnly(10, 30)),
                    Status = AgendamentoItemStatus.Confirmado
                }
            ]
        });
        await context.SaveChangesAsync();

        var repository = new AgendamentoItemRepository(context);

        var existeImpacto = await repository.ExisteAgendamentoFuturoImpactadoPorAlteracaoHorarioAsync(
            estabelecimentoId: 20,
            profissionalId: 40,
            diaSemanaAtual: DayOfWeek.Monday,
            horaInicioAtual: new TimeOnly(9, 0),
            horaFimAtual: new TimeOnly(12, 0),
            novoDiaSemana: DayOfWeek.Monday,
            novaHoraInicio: new TimeOnly(10, 0),
            novaHoraFim: new TimeOnly(13, 0));

        Assert.True(existeImpacto);
    }

    private static DateTime ProximaData(DayOfWeek diaSemana, TimeOnly horario)
    {
        var data = DateTime.UtcNow.Date.AddDays(1);
        while (data.DayOfWeek != diaSemana)
        {
            data = data.AddDays(1);
        }

        return data.Add(horario.ToTimeSpan());
    }
}
