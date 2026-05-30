using GLOWAPI.Domain.Entities;
using GLOWAPI.Infrastructure;
using GLOWAPI.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Tests.Unit.Infrastructure;

public class HorarioFuncionamentoEstabelecimentoRepositoryTests
{
    [Fact]
    public async Task ListarPorEstabelecimentoAsync_DeveFiltrarEOrdenarHorarios()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.HorariosFuncionamentoEstabelecimento.AddRange(
            new HorarioFuncionamentoEstabelecimento
            {
                Id = 1,
                EstabelecimentoId = 20,
                DiaSemana = DayOfWeek.Monday,
                HoraInicio = new TimeOnly(13, 0),
                HoraFim = new TimeOnly(18, 0),
                Ativo = true
            },
            new HorarioFuncionamentoEstabelecimento
            {
                Id = 2,
                EstabelecimentoId = 20,
                DiaSemana = DayOfWeek.Monday,
                HoraInicio = new TimeOnly(8, 0),
                HoraFim = new TimeOnly(12, 0),
                Ativo = true
            },
            new HorarioFuncionamentoEstabelecimento
            {
                Id = 3,
                EstabelecimentoId = 20,
                DiaSemana = DayOfWeek.Tuesday,
                HoraInicio = new TimeOnly(9, 0),
                HoraFim = new TimeOnly(18, 0),
                Ativo = true
            },
            new HorarioFuncionamentoEstabelecimento
            {
                Id = 4,
                EstabelecimentoId = 20,
                DiaSemana = DayOfWeek.Monday,
                HoraInicio = new TimeOnly(19, 0),
                HoraFim = new TimeOnly(20, 0),
                Ativo = false
            },
            new HorarioFuncionamentoEstabelecimento
            {
                Id = 5,
                EstabelecimentoId = 99,
                DiaSemana = DayOfWeek.Monday,
                HoraInicio = new TimeOnly(7, 0),
                HoraFim = new TimeOnly(8, 0),
                Ativo = true
            });
        await context.SaveChangesAsync();

        var repository = new HorarioFuncionamentoEstabelecimentoRepository(context);

        var resultado = await repository.ListarPorEstabelecimentoAsync(
            20,
            DayOfWeek.Monday,
            ativo: true);

        Assert.Equal([2, 1], resultado.Select(horario => horario.Id).ToList());
    }

    [Fact]
    public async Task ExisteConflitoAtivoAsync_DeveDetectarSobreposicaoNoMesmoDia()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        context.HorariosFuncionamentoEstabelecimento.Add(new HorarioFuncionamentoEstabelecimento
        {
            EstabelecimentoId = 20,
            DiaSemana = DayOfWeek.Monday,
            HoraInicio = new TimeOnly(9, 0),
            HoraFim = new TimeOnly(12, 0),
            Ativo = true
        });
        await context.SaveChangesAsync();

        var repository = new HorarioFuncionamentoEstabelecimentoRepository(context);

        var conflita = await repository.ExisteConflitoAtivoAsync(
            20,
            DayOfWeek.Monday,
            new TimeOnly(11, 0),
            new TimeOnly(13, 0));

        Assert.True(conflita);
    }
}
