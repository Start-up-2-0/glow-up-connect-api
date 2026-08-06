using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCodigoAgendamentoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoAgendamento",
                table: "Usuarios",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CodigoAgendamento",
                table: "Usuarios",
                column: "CodigoAgendamento",
                unique: true,
                filter: "CodigoAgendamento IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_CodigoAgendamento",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CodigoAgendamento",
                table: "Usuarios");
        }
    }
}
