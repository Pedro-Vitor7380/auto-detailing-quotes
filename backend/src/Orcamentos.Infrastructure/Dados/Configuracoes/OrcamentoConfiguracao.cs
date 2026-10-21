using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Infrastructure.Dados.Configuracoes;

public class OrcamentoConfiguracao : IEntityTypeConfiguration<Orcamento>
{
    public void Configure(EntityTypeBuilder<Orcamento> builder)
    {
        builder.HasKey(o => o.Id);

        // Enum gravado como texto ("Rascunho"), não como número (0): legível no banco e
        // seguro se um dia a ordem do enum mudar.
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.DescontoTipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.DescontoValor).HasPrecision(10, 2);
        builder.Property(o => o.Observacoes).HasMaxLength(Orcamento.ObservacoesTamanhoMaximo);

        // O número não se repete na oficina. Quem garante é o ContadorDeOrcamentos; o índice é a última linha de defesa.
        builder.HasIndex(o => new { o.OficinaId, o.Ano, o.Sequencial }).IsUnique();
        builder.HasIndex(o => new { o.OficinaId, o.Status });
        builder.HasIndex(o => new { o.OficinaId, o.AgendadoDe, o.AgendadoAte }); // a agenda busca os períodos que cruzam o mês

        builder.HasOne<Oficina>().WithMany().HasForeignKey(o => o.OficinaId).OnDelete(DeleteBehavior.Restrict);

        // Restrict: cliente e veículo com orçamentos não podem ser excluídos (o histórico apontaria para o nada).
        builder.HasOne<Cliente>().WithMany().HasForeignKey(o => o.ClienteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Veiculo>().WithMany().HasForeignKey(o => o.VeiculoId).OnDelete(DeleteBehavior.Restrict);

        // OwnsMany: os itens PERTENCEM ao orçamento (não existem sozinhos, não têm DbSet próprio).
        // Ficam numa tabela separada e são carregados sempre junto com o orçamento.
        builder.OwnsMany(o => o.Itens, item =>
        {
            item.ToTable("itens_orcamento");
            item.WithOwner().HasForeignKey("OrcamentoId");
            item.HasKey(i => i.Id);
            item.Property(i => i.Id).ValueGeneratedNever(); // mesma pegadinha do Veiculo (passo 16)

            item.Property(i => i.Descricao).HasMaxLength(ItemOrcamento.DescricaoTamanhoMaximo).IsRequired();
            item.Property(i => i.ObservacaoEstado).HasMaxLength(ItemOrcamento.ObservacaoTamanhoMaximo);
            item.Property(i => i.PrecoCatalogo).HasPrecision(10, 2);
            item.Property(i => i.ValorUnitario).HasPrecision(10, 2);

            // Serviço desativado continua existindo, então a referência nunca quebra.
            item.HasOne<Servico>().WithMany().HasForeignKey(i => i.ServicoId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation(o => o.Itens).HasField("_itens").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
