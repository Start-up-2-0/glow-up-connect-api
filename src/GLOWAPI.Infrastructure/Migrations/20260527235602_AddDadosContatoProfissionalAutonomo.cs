using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDadosContatoProfissionalAutonomo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Profissionais",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Logo",
                table: "Profissionais",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Telefone",
                table: "Profissionais",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Profissionais");

            migrationBuilder.DropColumn(
                name: "Logo",
                table: "Profissionais");

            migrationBuilder.DropColumn(
                name: "Telefone",
                table: "Profissionais");
        }
    }
}
