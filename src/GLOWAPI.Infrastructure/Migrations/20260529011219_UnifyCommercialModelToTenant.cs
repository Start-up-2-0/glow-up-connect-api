using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnifyCommercialModelToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agendamentos_Profissionais_ProfissionalAutonomoId",
                table: "Agendamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_Assinaturas_Profissionais_ProfissionalAutonomoId",
                table: "Assinaturas");

            migrationBuilder.DropForeignKey(
                name: "FK_Caixas_Profissionais_ProfissionalAutonomoId",
                table: "Caixas");

            migrationBuilder.DropForeignKey(
                name: "FK_Enderecos_Profissionais_ProfissionalAutonomoId",
                table: "Enderecos");

            migrationBuilder.DropForeignKey(
                name: "FK_Servicos_Profissionais_ProfissionalAutonomoId",
                table: "Servicos");

            migrationBuilder.DropIndex(
                name: "IX_Servicos_ProfissionalAutonomoId",
                table: "Servicos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Servicos_Titular",
                table: "Servicos");

            migrationBuilder.DropIndex(
                name: "IX_Enderecos_ProfissionalAutonomoId",
                table: "Enderecos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Enderecos_Titular",
                table: "Enderecos");

            migrationBuilder.DropIndex(
                name: "IX_Caixas_ProfissionalAutonomoId",
                table: "Caixas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Caixas_Titular",
                table: "Caixas");

            migrationBuilder.DropIndex(
                name: "IX_Assinaturas_ProfissionalAutonomoId",
                table: "Assinaturas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Assinaturas_Titular",
                table: "Assinaturas");

            migrationBuilder.DropIndex(
                name: "IX_Agendamentos_ProfissionalAutonomoId",
                table: "Agendamentos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Agendamentos_Titular",
                table: "Agendamentos");

            migrationBuilder.DropColumn(
                name: "ProfissionalAutonomoId",
                table: "Servicos");

            migrationBuilder.DropColumn(
                name: "ProfissionalAutonomoId",
                table: "Enderecos");

            migrationBuilder.DropColumn(
                name: "ProfissionalAutonomoId",
                table: "Caixas");

            migrationBuilder.DropColumn(
                name: "ProfissionalAutonomoId",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "ProfissionalAutonomoId",
                table: "Agendamentos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProfissionalAutonomoId",
                table: "Servicos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfissionalAutonomoId",
                table: "Enderecos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfissionalAutonomoId",
                table: "Caixas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfissionalAutonomoId",
                table: "Assinaturas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfissionalAutonomoId",
                table: "Agendamentos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Servicos_ProfissionalAutonomoId",
                table: "Servicos",
                column: "ProfissionalAutonomoId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Servicos_Titular",
                table: "Servicos",
                sql: "((\"EstabelecimentoId\" IS NOT NULL AND \"ProfissionalAutonomoId\" IS NULL) OR (\"EstabelecimentoId\" IS NULL AND \"ProfissionalAutonomoId\" IS NOT NULL))");

            migrationBuilder.CreateIndex(
                name: "IX_Enderecos_ProfissionalAutonomoId",
                table: "Enderecos",
                column: "ProfissionalAutonomoId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Enderecos_Titular",
                table: "Enderecos",
                sql: "((\"EstabelecimentoId\" IS NOT NULL AND \"ProfissionalAutonomoId\" IS NULL) OR (\"EstabelecimentoId\" IS NULL AND \"ProfissionalAutonomoId\" IS NOT NULL))");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_ProfissionalAutonomoId",
                table: "Caixas",
                column: "ProfissionalAutonomoId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Caixas_Titular",
                table: "Caixas",
                sql: "((\"EstabelecimentoId\" IS NOT NULL AND \"ProfissionalAutonomoId\" IS NULL) OR (\"EstabelecimentoId\" IS NULL AND \"ProfissionalAutonomoId\" IS NOT NULL))");

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_ProfissionalAutonomoId",
                table: "Assinaturas",
                column: "ProfissionalAutonomoId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assinaturas_Titular",
                table: "Assinaturas",
                sql: "((\"EstabelecimentoId\" IS NOT NULL AND \"ProfissionalAutonomoId\" IS NULL) OR (\"EstabelecimentoId\" IS NULL AND \"ProfissionalAutonomoId\" IS NOT NULL))");

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_ProfissionalAutonomoId",
                table: "Agendamentos",
                column: "ProfissionalAutonomoId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Agendamentos_Titular",
                table: "Agendamentos",
                sql: "((\"EstabelecimentoId\" IS NOT NULL AND \"ProfissionalAutonomoId\" IS NULL) OR (\"EstabelecimentoId\" IS NULL AND \"ProfissionalAutonomoId\" IS NOT NULL))");

            migrationBuilder.AddForeignKey(
                name: "FK_Agendamentos_Profissionais_ProfissionalAutonomoId",
                table: "Agendamentos",
                column: "ProfissionalAutonomoId",
                principalTable: "Profissionais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assinaturas_Profissionais_ProfissionalAutonomoId",
                table: "Assinaturas",
                column: "ProfissionalAutonomoId",
                principalTable: "Profissionais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Caixas_Profissionais_ProfissionalAutonomoId",
                table: "Caixas",
                column: "ProfissionalAutonomoId",
                principalTable: "Profissionais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enderecos_Profissionais_ProfissionalAutonomoId",
                table: "Enderecos",
                column: "ProfissionalAutonomoId",
                principalTable: "Profissionais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Servicos_Profissionais_ProfissionalAutonomoId",
                table: "Servicos",
                column: "ProfissionalAutonomoId",
                principalTable: "Profissionais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
