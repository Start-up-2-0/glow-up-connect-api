using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260818013000_ExpandirOnboardingPendenteJsonParaLongText")]
    public partial class ExpandirOnboardingPendenteJsonParaLongText : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE `Assinaturas`
                MODIFY `OnboardingPendenteJson` longtext NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE `Assinaturas`
                MODIFY `OnboardingPendenteJson` text NULL;
                """);
        }
    }
}
