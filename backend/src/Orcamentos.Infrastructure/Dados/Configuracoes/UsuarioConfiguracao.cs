using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Infrastructure.Dados.Configuracoes;

public class UsuarioConfiguracao : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nome).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(254).IsRequired(); // limite máximo de um e-mail
        builder.Property(u => u.SenhaHash).IsRequired();

        // O e-mail é o login: não pode repetir. O banco garante isso mesmo se o código falhar.
        builder.HasIndex(u => u.Email).IsUnique();

        // Chave estrangeira: todo usuário aponta para uma oficina existente.
        // Restrict = o banco impede apagar uma oficina que ainda tem usuários.
        builder.HasOne<Oficina>()
            .WithMany()
            .HasForeignKey(u => u.OficinaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
