using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Infrastructure.Dados.Configuracoes;

/// <summary>
/// Como a entidade Oficina é guardada no banco: tamanhos de coluna, obrigatoriedade etc.
/// </summary>
public class OficinaConfiguracao : IEntityTypeConfiguration<Oficina>
{
    public void Configure(EntityTypeBuilder<Oficina> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Nome).HasMaxLength(150).IsRequired();
        builder.Property(o => o.Cnpj).HasMaxLength(18);       // 00.000.000/0000-00
        builder.Property(o => o.Telefone).HasMaxLength(20);
        builder.Property(o => o.Endereco).HasMaxLength(250);
        builder.Property(o => o.LogoUrl).HasMaxLength(500);
    }
}
