using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations;

/// <summary>
/// Adiciona tipo (Individual/Combo) e imagem ilustrativa aos serviços.
/// </summary>
public partial class AddTipoServicoEImagemServico : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Imagem",
            table: "Servicos",
            type: "longtext",
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<string>(
            name: "TipoServico",
            table: "Servicos",
            type: "varchar(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "Individual")
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Imagem",
            table: "Servicos");

        migrationBuilder.DropColumn(
            name: "TipoServico",
            table: "Servicos");
    }
}
