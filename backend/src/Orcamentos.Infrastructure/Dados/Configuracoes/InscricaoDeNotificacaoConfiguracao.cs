using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Infrastructure.Dados.Configuracoes;

public class InscricaoDeNotificacaoConfiguracao : IEntityTypeConfiguration<InscricaoDeNotificacao>
{
    public void Configure(EntityTypeBuilder<InscricaoDeNotificacao> builder)
    {
        builder.ToTable("inscricoes_de_notificacao");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Endpoint).HasMaxLength(InscricaoDeNotificacao.EndpointTamanhoMaximo).IsRequired();
        builder.Property(i => i.P256dh).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Auth).HasMaxLength(100).IsRequired();

        // O endereço identifica o aparelho: ativar de novo no mesmo celular atualiza, não duplica.
        builder.HasIndex(i => i.Endpoint).IsUnique();

        builder.HasOne<Oficina>().WithMany().HasForeignKey(i => i.OficinaId).OnDelete(DeleteBehavior.Restrict);
    }
}
