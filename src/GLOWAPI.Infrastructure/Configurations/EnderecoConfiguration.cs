using GLOWAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GLOWAPI.Infrastructure.Configurations;

public class EnderecoConfiguration : IEntityTypeConfiguration<Endereco>
{
    public void Configure(EntityTypeBuilder<Endereco> builder)
    {
        builder.ToTable("Enderecos");

        builder.HasKey(endereco => endereco.Id);

        builder.Property(endereco => endereco.Cep)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(endereco => endereco.Logradouro)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(endereco => endereco.Numero)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(endereco => endereco.Complemento)
            .HasMaxLength(100);

        builder.Property(endereco => endereco.Bairro)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(endereco => endereco.Cidade)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(endereco => endereco.Estado)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(endereco => endereco.CreateAd)
            .IsRequired();

        builder.Property(endereco => endereco.UpdatedAt);

        builder.HasOne(endereco => endereco.Estabelecimento)
            .WithOne(estabelecimento => estabelecimento.Endereco)
            .HasForeignKey<Endereco>(endereco => endereco.EstabelecimentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(endereco => endereco.EstabelecimentoId)
            .IsUnique();
    }
}
