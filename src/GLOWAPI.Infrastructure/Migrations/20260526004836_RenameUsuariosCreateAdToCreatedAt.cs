using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLOWAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameUsuariosCreateAdToCreatedAt : Migration
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
                          AND column_name = 'CreateAd'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'Usuarios'
                          AND column_name = 'CreatedAt'
                    ) THEN
                        ALTER TABLE "Usuarios" RENAME COLUMN "CreateAd" TO "CreatedAt";
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
                          AND column_name = 'CreatedAt'
                    ) AND NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'Usuarios'
                          AND column_name = 'CreateAd'
                    ) THEN
                        ALTER TABLE "Usuarios" RENAME COLUMN "CreatedAt" TO "CreateAd";
                    END IF;
                END $$;
                """);
        }
    }
}
