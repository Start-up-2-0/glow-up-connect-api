using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenomearCategoriasParaOficio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Labels de ofício (neutras para loja e profissional autônomo).
            migrationBuilder.Sql("""
                UPDATE `CategoriasEstabelecimento`
                SET `Nome` = 'Cabelo e barba'
                WHERE `Id` = 1;

                UPDATE `CategoriasEstabelecimento`
                SET `Nome` = 'Beleza e estética'
                WHERE `Id` = 2;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE `CategoriasEstabelecimento`
                SET `Nome` = 'Barbearia'
                WHERE `Id` = 1;

                UPDATE `CategoriasEstabelecimento`
                SET `Nome` = 'Salão de Beleza'
                WHERE `Id` = 2;
                """);
        }
    }
}
