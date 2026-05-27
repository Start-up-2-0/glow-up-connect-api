using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanoAlteracaoPendenteAssinatura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlanoAlteracaoPendenteId",
                table: "Assinaturas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_PlanoAlteracaoPendenteId",
                table: "Assinaturas",
                column: "PlanoAlteracaoPendenteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assinaturas_Planos_PlanoAlteracaoPendenteId",
                table: "Assinaturas",
                column: "PlanoAlteracaoPendenteId",
                principalTable: "Planos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assinaturas_Planos_PlanoAlteracaoPendenteId",
                table: "Assinaturas");

            migrationBuilder.DropIndex(
                name: "IX_Assinaturas_PlanoAlteracaoPendenteId",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "PlanoAlteracaoPendenteId",
                table: "Assinaturas");
        }
    }
}
