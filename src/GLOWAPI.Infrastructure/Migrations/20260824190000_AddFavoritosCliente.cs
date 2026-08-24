using System;
using GLOWAPI.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260824190000_AddFavoritosCliente")]
public partial class AddFavoritosCliente : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FavoritosCliente",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                UsuarioClienteId = table.Column<int>(type: "int", nullable: false),
                EstabelecimentoId = table.Column<int>(type: "int", nullable: false),
                ProfissionalId = table.Column<int>(type: "int", nullable: true),
                ProfissionalChave = table.Column<int>(type: "int", nullable: false),
                CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FavoritosCliente", x => x.Id);
                table.ForeignKey("FK_FavoritosCliente_Estabelecimentos_EstabelecimentoId", x => x.EstabelecimentoId, "Estabelecimentos", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_FavoritosCliente_Profissionais_ProfissionalId", x => x.ProfissionalId, "Profissionais", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_FavoritosCliente_Usuarios_UsuarioClienteId", x => x.UsuarioClienteId, "Usuarios", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_FavoritosCliente_EstabelecimentoId", "FavoritosCliente", "EstabelecimentoId");
        migrationBuilder.CreateIndex("IX_FavoritosCliente_ProfissionalId", "FavoritosCliente", "ProfissionalId");
        migrationBuilder.CreateIndex("IX_FavoritosCliente_UsuarioClienteId_EstabelecimentoId_ProfissionalChave", "FavoritosCliente", new[] { "UsuarioClienteId", "EstabelecimentoId", "ProfissionalChave" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "FavoritosCliente");
}
