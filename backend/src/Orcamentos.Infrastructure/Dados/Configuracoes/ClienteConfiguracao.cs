using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Infrastructure.Dados.Configuracoes;

public class ClienteConfiguracao : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome).HasMaxLength(Cliente.NomeTamanhoMaximo).IsRequired();
        builder.Property(c => c.WhatsApp).HasMaxLength(13).IsRequired();

        // Sem índice único no WhatsApp: duas pessoas da mesma família podem usar o mesmo número.
        builder.HasIndex(c => new { c.OficinaId, c.Nome });

        builder.HasOne<Oficina>()
            .WithMany()
            .HasForeignKey(c => c.OficinaId)
            .OnDelete(DeleteBehavior.Restrict);

        // A lista de veículos é só leitura por fora; o EF grava direto no campo privado _veiculos.
        builder.HasMany(c => c.Veiculos)
            .WithOne()
            .HasForeignKey(v => v.ClienteId)
            .OnDelete(DeleteBehavior.Cascade); // excluir o cliente exclui os veículos dele (LGPD)
        builder.Navigation(c => c.Veiculos).HasField("_veiculos").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
