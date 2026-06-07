using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CorrigirLimiteAgendamentosPlanoBasic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Planos"
                SET "LimiteAgendamentos" = NULL,
                    "UpdatedAt" = NOW()
                WHERE "Nome" = 'Basic';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Planos"
                SET "LimiteAgendamentos" = 10,
                    "UpdatedAt" = NOW()
                WHERE "Nome" = 'Basic';
                """);
        }
    }
}
