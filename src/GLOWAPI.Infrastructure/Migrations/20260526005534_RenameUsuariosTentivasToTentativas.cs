using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameUsuariosTentivasToTentativas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'Usuarios'
                          AND column_name = 'Tentivas'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'Usuarios'
                          AND column_name = 'Tentativas'
                    ) THEN
                        ALTER TABLE "Usuarios" RENAME COLUMN "Tentivas" TO "Tentativas";
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'Usuarios'
                          AND column_name = 'Tentativas'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'Usuarios'
                          AND column_name = 'Tentivas'
                    ) THEN
                        ALTER TABLE "Usuarios" RENAME COLUMN "Tentativas" TO "Tentivas";
                    END IF;
                END $$;
                """);
        }
    }
}
