using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Infrastructure.Dados.Configuracoes;

public class ServicoConfiguracao : IEntityTypeConfiguration<Servico>
{
    public void Configure(EntityTypeBuilder<Servico> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Nome).HasMaxLength(Servico.NomeTamanhoMaximo).IsRequired();
        builder.Property(s => s.Descricao).HasMaxLength(Servico.DescricaoTamanhoMaximo);

        // numeric(10,2): valor exato com 2 casas (até R$ 99.999.999,99). Nunca float para dinheiro.
        builder.Property(s => s.PrecoMinimo).HasPrecision(10, 2);
        builder.Property(s => s.PrecoMaximo).HasPrecision(10, 2);

        builder.HasIndex(s => new { s.OficinaId, s.Nome }).IsUnique();

        // A mesma regra do Domínio, garantida também pelo banco.
        builder.ToTable(t => t.HasCheckConstraint("ck_servicos_faixa_de_preco",
            "preco_minimo >= 0 AND preco_maximo >= preco_minimo"));

        builder.HasOne<Oficina>()
            .WithMany()
            .HasForeignKey(s => s.OficinaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict: não deixa apagar uma categoria que ainda tem serviços.
        builder.HasOne<Categoria>()
            .WithMany()
            .HasForeignKey(s => s.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
