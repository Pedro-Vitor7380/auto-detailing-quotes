using Microsoft.EntityFrameworkCore;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Infrastructure.Dados;

/// <summary>
/// Representa uma sessão com o banco de dados. Cada DbSet vira uma tabela.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Oficina> Oficinas => Set<Oficina>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas as classes IEntityTypeConfiguration da pasta Configuracoes.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
