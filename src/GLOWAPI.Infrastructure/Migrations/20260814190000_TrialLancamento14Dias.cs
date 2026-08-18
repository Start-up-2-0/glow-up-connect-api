using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260814190000_TrialLancamento14Dias")]
    public partial class TrialLancamento14Dias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE `CampanhasPromocionais`
                SET `DiasTrial` = 14,
                    `UpdatedAt` = UTC_TIMESTAMP()
                WHERE `Codigo` = 'lancamento-100';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE `CampanhasPromocionais`
                SET `DiasTrial` = 30,
                    `UpdatedAt` = UTC_TIMESTAMP()
                WHERE `Codigo` = 'lancamento-100';
                """);
        }
    }
}
