using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Infrastructure.Dados.Configuracoes;

public class CategoriaConfiguracao : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome).HasMaxLength(Categoria.NomeTamanhoMaximo).IsRequired();

        // O mesmo nome pode existir em oficinas diferentes, mas não repetido dentro da mesma.
        builder.HasIndex(c => new { c.OficinaId, c.Nome }).IsUnique();

        builder.HasOne<Oficina>()
            .WithMany()
            .HasForeignKey(c => c.OficinaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
