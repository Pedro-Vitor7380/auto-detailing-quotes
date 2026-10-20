using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Infrastructure.Dados.Configuracoes;

public class VeiculoConfiguracao : IEntityTypeConfiguration<Veiculo>
{
    public void Configure(EntityTypeBuilder<Veiculo> builder)
    {
        builder.HasKey(v => v.Id);

        // O Id já nasce preenchido no C# (Guid.CreateVersion7). Sem isto, ao achar um veículo NOVO
        // dentro de um cliente já salvo, o EF veria um Id preenchido, pensaria "já existe no banco"
        // e tentaria um UPDATE em vez de INSERT.
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.Placa).HasMaxLength(7).IsRequired();
        builder.Property(v => v.Modelo).HasMaxLength(Veiculo.ModeloTamanhoMaximo).IsRequired();
        builder.Property(v => v.Cor).HasMaxLength(Veiculo.CorTamanhoMaximo);

        // Um carro pertence a um cliente só, dentro da oficina.
        builder.HasIndex(v => new { v.OficinaId, v.Placa }).IsUnique();

        builder.HasOne<Oficina>()
            .WithMany()
            .HasForeignKey(v => v.OficinaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
