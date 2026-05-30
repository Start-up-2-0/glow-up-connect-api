using GLOWAPI.Domain.Entities;
using GLOWAPI.Infrastructure;
using GLOWAPI.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Tests.Unit.Infrastructure;

public class HorarioAtendimentoProfissionalRepositoryTests
{
    [Fact]
    public async Task ListarPorEstabelecimentoAsync_DeveFiltrarEOrdenarHorarios()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.HorariosAtendimentoProfissional.AddRange(
            new HorarioAtendimentoProfissional
            {
                Id = 1,
                EstabelecimentoId = 20,
                ProfissionalId = 40,
                DiaSemana = DayOfWeek.Monday,
                HoraInicio = new TimeOnly(13, 0),
                HoraFim = new TimeOnly(18, 0),
                Ativo = true
            },
            new HorarioAtendimentoProfissional
            {
                Id = 2,
                EstabelecimentoId = 20,
                ProfissionalId = 40,
                DiaSemana = DayOfWeek.Monday,
                HoraInicio = new TimeOnly(8, 0),
                HoraFim = new TimeOnly(12, 0),
                Ativo = true
            },
            new HorarioAtendimentoProfissional
            {
                Id = 3,
                EstabelecimentoId = 20,
                ProfissionalId = 41,
                DiaSemana = DayOfWeek.Monday,
                HoraInicio = new TimeOnly(9, 0),
                HoraFim = new TimeOnly(12, 0),
                Ativo = true
            },
            new HorarioAtendimentoProfissional
            {
                Id = 4,
                EstabelecimentoId = 20,
                ProfissionalId = 40,
                DiaSemana = DayOfWeek.Monday,
                HoraInicio = new TimeOnly(19, 0),
                HoraFim = new TimeOnly(20, 0),
                Ativo = false
            },
            new HorarioAtendimentoProfissional
            {
                Id = 5,
                EstabelecimentoId = 99,
                ProfissionalId = 40,
                DiaSemana = DayOfWeek.Monday,
                HoraInicio = new TimeOnly(7, 0),
                HoraFim = new TimeOnly(8, 0),
                Ativo = true
            });
        await context.SaveChangesAsync();

        var repository = new HorarioAtendimentoProfissionalRepository(context);

        var resultado = await repository.ListarPorEstabelecimentoAsync(
            20,
            profissionalId: 40,
            diaSemana: DayOfWeek.Monday,
            ativo: true);

        Assert.Equal([2, 1], resultado.Select(horario => horario.Id).ToList());
    }
}
