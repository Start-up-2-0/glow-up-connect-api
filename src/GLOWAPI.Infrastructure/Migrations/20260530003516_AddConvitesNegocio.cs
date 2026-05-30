using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConvitesNegocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConvitesNegocio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EstabelecimentoId = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Telefone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NomePublico = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TipoConvite = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RoleSugerida = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PodeReceberAgendamento = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CriadoPorUsuarioId = table.Column<int>(type: "integer", nullable: false),
                    AceitoPorUsuarioId = table.Column<int>(type: "integer", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondidoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConvitesNegocio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConvitesNegocio_Estabelecimentos_EstabelecimentoId",
                        column: x => x.EstabelecimentoId,
                        principalTable: "Estabelecimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConvitesNegocio_Usuarios_AceitoPorUsuarioId",
                        column: x => x.AceitoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ConvitesNegocio_Usuarios_CriadoPorUsuarioId",
                        column: x => x.CriadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConvitesNegocio_AceitoPorUsuarioId",
                table: "ConvitesNegocio",
                column: "AceitoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ConvitesNegocio_CriadoPorUsuarioId",
                table: "ConvitesNegocio",
                column: "CriadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ConvitesNegocio_EstabelecimentoId_Email_TipoConvite_Status",
                table: "ConvitesNegocio",
                columns: new[] { "EstabelecimentoId", "Email", "TipoConvite", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ConvitesNegocio_TokenHash",
                table: "ConvitesNegocio",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConvitesNegocio");
        }
    }
}
