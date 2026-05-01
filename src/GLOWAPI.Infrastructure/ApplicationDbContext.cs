using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GLOWAPI.Infrastructure;

public class ApplicationDbContext : DbContext
{
    // Construtor que recebe as configurações de banco (SQL, MySQL ou Postgres)
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Estabelecimento> Estabelecimentos { get; set; }
    public DbSet<EstabelecimentoUsuario> EstabelecimentoUsuarios { get; set; }
    public DbSet<Profissional> Profissionais { get; set; }
    public DbSet<ProfissionalEstabelecimento> ProfissionalEstabelecimentos { get; set; }
    public DbSet<Endereco> Enderecos { get; set; }
    public DbSet<Servico> Servicos { get; set; }
    public DbSet<ProfissionalServico> ProfissionalServicos { get; set; }
    public DbSet<HorarioFuncionamentoEstabelecimento> HorariosFuncionamentoEstabelecimento { get; set; }
    public DbSet<HorarioAtendimentoProfissional> HorariosAtendimentoProfissional { get; set; }
    public DbSet<Agendamento> Agendamentos { get; set; }
    public DbSet<AgendamentoItem> AgendamentoItens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Chama a implementação base
        base.OnModelCreating(modelBuilder);

        // Esta linha faz o EF procurar automaticamente por todas as classes 
        // de configuração (Fluent API) que estiverem neste projeto (Infrastructure).
        // Assim, você mantém este arquivo limpo.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Exemplo de configuração global: Garante que strings sem tamanho definido 
        // sejam criadas como varchar(255) em vez de varchar(max/text), se desejar.
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties().Where(p => p.ClrType == typeof(string)))
            {
                if (string.IsNullOrEmpty(property.GetColumnType()) && property.GetMaxLength() == null)
                {
                    property.SetMaxLength(255);
                }
            }
        }
    }
}
