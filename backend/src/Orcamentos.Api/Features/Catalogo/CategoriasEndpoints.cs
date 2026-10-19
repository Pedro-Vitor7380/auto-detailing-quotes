using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orcamentos.Api.Comum;
using Orcamentos.Api.Features.Auth;
using Orcamentos.Domain.Entidades;
using Orcamentos.Infrastructure.Dados;

namespace Orcamentos.Api.Features.Catalogo;

public record CategoriaRequest(string Nome);
public record CategoriaResponse(Guid Id, string Nome, int QuantidadeDeServicos);

public static class CategoriasEndpoints
{
    public static IEndpointRouteBuilder MapCategoriasEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/categorias").WithTags("Catálogo");

        grupo.MapGet("/", Listar).WithSummary("Lista as categorias da oficina, em ordem alfabética");
        grupo.MapPost("/", Criar).WithSummary("Cria uma categoria");
        grupo.MapPut("/{id:guid}", Renomear).WithSummary("Renomeia uma categoria");
        grupo.MapDelete("/{id:guid}", Excluir).WithSummary("Exclui uma categoria sem serviços");

        return app;
    }

    // Repare que nenhuma consulta filtra por oficina: o filtro global do AppDbContext faz isso.
    private static async Task<Ok<List<CategoriaResponse>>> Listar(AppDbContext db, CancellationToken ct)
    {
        var categorias = await db.Categorias
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaResponse(c.Id, c.Nome, db.Servicos.Count(s => s.CategoriaId == c.Id)))
            .ToListAsync(ct);

        return TypedResults.Ok(categorias);
    }

    private static async Task<Results<Created<CategoriaResponse>, ProblemHttpResult>> Criar(
        CategoriaRequest request, AppDbContext db, IOficinaAtual oficinaAtual, CancellationToken ct)
    {
        var categoria = new Categoria(oficinaAtual.IdObrigatorio(), request.Nome);

        if (await NomeEmUso(db, categoria.Nome, idIgnorado: null, ct))
            return Problemas.Conflito($"Já existe uma categoria chamada \"{categoria.Nome}\".");

        db.Categorias.Add(categoria);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/categorias/{categoria.Id}", new CategoriaResponse(categoria.Id, categoria.Nome, 0));
    }

    private static async Task<Results<Ok<CategoriaResponse>, ProblemHttpResult>> Renomear(
        Guid id, CategoriaRequest request, AppDbContext db, CancellationToken ct)
    {
        // Categoria de outra oficina não "existe" para esta requisição: o filtro devolve null e respondemos 404.
        var categoria = await db.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (categoria is null)
            return Problemas.NaoEncontrado("Categoria não encontrada.");

        categoria.Renomear(request.Nome);

        if (await NomeEmUso(db, categoria.Nome, idIgnorado: id, ct))
            return Problemas.Conflito($"Já existe uma categoria chamada \"{categoria.Nome}\".");

        await db.SaveChangesAsync(ct);

        var quantidade = await db.Servicos.CountAsync(s => s.CategoriaId == id, ct);
        return TypedResults.Ok(new CategoriaResponse(categoria.Id, categoria.Nome, quantidade));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Excluir(Guid id, AppDbContext db, CancellationToken ct)
    {
        var categoria = await db.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (categoria is null)
            return Problemas.NaoEncontrado("Categoria não encontrada.");

        // Inclui os inativos: o banco também impediria (Restrict), mas aqui a mensagem é clara.
        if (await db.Servicos.AnyAsync(s => s.CategoriaId == id, ct))
            return Problemas.Conflito("A categoria tem serviços. Mova-os para outra categoria antes de excluir.");

        db.Categorias.Remove(categoria);
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }

    // Compara sem diferenciar maiúsculas: "Películas" e "películas" contam como o mesmo nome.
    private static Task<bool> NomeEmUso(AppDbContext db, string nome, Guid? idIgnorado, CancellationToken ct) =>
        db.Categorias.AnyAsync(c => c.Id != idIgnorado && c.Nome.ToLower() == nome.ToLower(), ct);
}
