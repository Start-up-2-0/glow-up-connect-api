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
        var inicio = ProximaData(DayOfWeek.Monday, new TimeOnly(9, 30));
        var fim = ProximaData(DayOfWeek.Monday, new TimeOnly(10, 30));

        context.Servicos.Add(new Servico
        {
            Id = 1,
            Nome = "Corte",
            DuracaoMinutos = 60,
            Ativo = true
        });
        context.Profissionais.Add(new Profissional
        {
            Id = 40,
            NomePublico = "Profissional Teste",
            TipoProfissional = ProfessionalType.VinculadoEstabelecimento,
            Ativo = true
        });
        context.Agendamentos.Add(new Agendamento
        {
            Id = 1,
            EstabelecimentoId = 20,
            UsuarioClienteId = null,
            Inicio = inicio,
            Fim = fim,
            Status = AgendamentoStatus.Confirmado,
            ClienteNome = "Cliente Teste",
            Itens =
            [
                new AgendamentoItem
                {
                    Id = 1,
                    ServicoId = 1,
                    ProfissionalId = 40,
                    Inicio = inicio,
                    Fim = fim,
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
        var data = DateTime.UtcNow.Date.AddDays(7);
        while (data.DayOfWeek != diaSemana)
        {
            data = data.AddDays(1);
        }

        return DateTime.SpecifyKind(data.Add(horario.ToTimeSpan()), DateTimeKind.Utc);
    }
}
