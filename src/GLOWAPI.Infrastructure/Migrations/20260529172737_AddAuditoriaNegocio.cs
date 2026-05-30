using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditoriaNegocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditoriasNegocio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EstabelecimentoId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true),
                    TipoAcao = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Entidade = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    EntidadeId = table.Column<int>(type: "integer", nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriasNegocio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditoriasNegocio_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditoriasNegocio_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriasNegocio_CriadoEm",
                table: "AuditoriasNegocio",
                column: "CriadoEm");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriasNegocio_EstabelecimentoId",
                table: "AuditoriasNegocio",
                column: "EstabelecimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriasNegocio_TipoAcao",
                table: "AuditoriasNegocio",
                column: "TipoAcao");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriasNegocio_UsuarioId",
                table: "AuditoriasNegocio",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditoriasNegocio");
        }
    }
}
