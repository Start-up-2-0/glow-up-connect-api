using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class ComodidadeConfiguration : IEntityTypeConfiguration<Comodidade>
{
    public void Configure(EntityTypeBuilder<Comodidade> builder)
    {
        builder.ToTable("Comodidades");
        builder.HasKey(comodidade => comodidade.Id);
        builder.Property(comodidade => comodidade.Nome).IsRequired().HasMaxLength(80);
        builder.Property(comodidade => comodidade.Slug).IsRequired().HasMaxLength(80);
        builder.Property(comodidade => comodidade.Icone).IsRequired().HasMaxLength(50);
        builder.HasIndex(comodidade => comodidade.Slug).IsUnique();
        builder.Property(comodidade => comodidade.Ativo).HasDefaultValue(true);

        builder.HasData(
            Criar(1, "Wi-Fi", "wi-fi", "wifi", 1),
            Criar(2, "Ambiente climatizado", "ambiente-climatizado", "snowflake", 2),
            Criar(3, "Bebidas", "bebidas", "cup-soda", 3),
            Criar(4, "Café", "cafe", "coffee", 4),
            Criar(5, "Videogame", "videogame", "gamepad-2", 5),
            Criar(6, "TV", "tv", "tv", 6),
            Criar(7, "Estacionamento", "estacionamento", "car", 7),
            Criar(8, "Acessibilidade", "acessibilidade", "accessibility", 8),
            Criar(9, "Atendimento infantil", "atendimento-infantil", "baby", 9),
            Criar(10, "Pet friendly", "pet-friendly", "paw-print", 10));
    }

    private static Comodidade Criar(int id, string nome, string slug, string icone, int ordem) => new()
    {
        Id = id,
        Nome = nome,
        Slug = slug,
        Icone = icone,
        Ordem = ordem,
        Ativo = true,
        CreateAd = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc)
    };
}
