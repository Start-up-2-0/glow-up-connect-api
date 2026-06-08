using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckoutProOnboardingPendente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenciaInterna",
                table: "Pagamentos",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OnboardingPendenteJson",
                table: "Assinaturas",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pagamentos_ReferenciaInterna",
                table: "Pagamentos",
                column: "ReferenciaInterna");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pagamentos_ReferenciaInterna",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "ReferenciaInterna",
                table: "Pagamentos");

            migrationBuilder.DropColumn(
                name: "OnboardingPendenteJson",
                table: "Assinaturas");
        }
    }
}
