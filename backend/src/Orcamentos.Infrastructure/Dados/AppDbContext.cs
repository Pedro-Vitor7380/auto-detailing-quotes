using Microsoft.EntityFrameworkCore;
using Orcamentos.Domain.Entidades;

namespace Orcamentos.Infrastructure.Dados;

/// <summary>
/// Representa uma sessão com o banco de dados. Cada DbSet vira uma tabela.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, IOficinaAtual oficinaAtual) : DbContext(options)
{
    public DbSet<Oficina> Oficinas => Set<Oficina>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();
    public DbSet<ContadorDeOrcamentos> ContadoresDeOrcamentos => Set<ContadorDeOrcamentos>();

    // Lida de novo a cada consulta: o EF trata esta propriedade como um parâmetro, não como um valor fixo.
    private Guid? OficinaIdAtual => oficinaAtual.Id;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // unaccent: função do PostgreSQL que tira acentos. Permite buscar "joao" e achar "João".
        modelBuilder.HasPostgresExtension("unaccent");

        // Aplica todas as classes IEntityTypeConfiguration da pasta Configuracoes.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<ContadorDeOrcamentos>(contador =>
        {
            contador.HasKey(c => new { c.OficinaId, c.Ano }); // uma linha por oficina e ano
            contador.HasOne<Oficina>().WithMany().HasForeignKey(c => c.OficinaId).OnDelete(DeleteBehavior.Restrict);
        });

        // Filtro global: TODA consulta nestas tabelas ganha "WHERE oficina_id = <oficina do token>",
        // sem precisar lembrar de escrever isso em cada endpoint. Sem usuário logado, não vê nada.
        // Toda entidade de negócio nova (Cliente, Orçamento...) precisa de uma linha aqui.
        // Usuario fica de fora de propósito: o login procura o e-mail ANTES de saber a oficina.
        modelBuilder.Entity<Categoria>().HasQueryFilter(c => c.OficinaId == OficinaIdAtual);
        modelBuilder.Entity<Servico>().HasQueryFilter(s => s.OficinaId == OficinaIdAtual);
        modelBuilder.Entity<Cliente>().HasQueryFilter(c => c.OficinaId == OficinaIdAtual);
        modelBuilder.Entity<Veiculo>().HasQueryFilter(v => v.OficinaId == OficinaIdAtual);
        modelBuilder.Entity<Orcamento>().HasQueryFilter(o => o.OficinaId == OficinaIdAtual); // os itens (owned) vêm junto
    }
}
