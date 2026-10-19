using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orcamentos.Api.Comum;
using Orcamentos.Api.Features.Auth;
using Orcamentos.Domain.Entidades;
using Orcamentos.Infrastructure.Dados;

namespace Orcamentos.Api.Features.Catalogo;

public record ServicoRequest(
    Guid CategoriaId, string Nome, string? Descricao,
    decimal PrecoMinimo, decimal PrecoMaximo, int TempoEstimadoMin);

public record ServicoResponse(
    Guid Id, Guid CategoriaId, string CategoriaNome, string Nome, string? Descricao,
    decimal PrecoMinimo, decimal PrecoMaximo, int TempoEstimadoMin, bool Ativo);

public static class ServicosEndpoints
{
    public static IEndpointRouteBuilder MapServicosEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/servicos").WithTags("Catálogo");

        grupo.MapGet("/", Listar).WithSummary("Lista os serviços, com busca por nome e filtro por categoria");
        grupo.MapGet("/{id:guid}", Obter).WithSummary("Dados de um serviço");
        grupo.MapPost("/", Criar).WithSummary("Cria um serviço");
        grupo.MapPut("/{id:guid}", Atualizar).WithSummary("Altera os dados de um serviço");
        grupo.MapPost("/{id:guid}/ativar", (Guid id, AppDbContext db, CancellationToken ct) => DefinirAtivo(id, true, db, ct))
            .WithSummary("Volta a oferecer o serviço nos orçamentos");
        grupo.MapPost("/{id:guid}/desativar", (Guid id, AppDbContext db, CancellationToken ct) => DefinirAtivo(id, false, db, ct))
            .WithSummary("Deixa de oferecer o serviço (não apaga: orçamentos antigos continuam com ele)");

        return app;
    }

    private static async Task<Ok<List<ServicoResponse>>> Listar(
        AppDbContext db, CancellationToken ct, string? busca = null, Guid? categoriaId = null, bool incluirInativos = false)
    {
        var consulta = db.Servicos.AsQueryable();

        if (!incluirInativos)
            consulta = consulta.Where(s => s.Ativo);

        if (categoriaId is not null)
            consulta = consulta.Where(s => s.CategoriaId == categoriaId);

        if (!string.IsNullOrWhiteSpace(busca))
            // ILIKE é o "LIKE sem diferenciar maiúsculas" do PostgreSQL: "pintura" acha "Pintura — Capô".
            consulta = consulta.Where(s => EF.Functions.ILike(s.Nome, $"%{busca.Trim()}%"));

        var servicos = await ParaResponse(consulta, db).ToListAsync(ct);

        return TypedResults.Ok(servicos);
    }

    private static async Task<Results<Ok<ServicoResponse>, ProblemHttpResult>> Obter(Guid id, AppDbContext db, CancellationToken ct)
    {
        var servico = await ParaResponse(db.Servicos.Where(s => s.Id == id), db).FirstOrDefaultAsync(ct);

        return servico is null ? Problemas.NaoEncontrado("Serviço não encontrado.") : TypedResults.Ok(servico);
    }

    private static async Task<Results<Created<ServicoResponse>, ProblemHttpResult>> Criar(
        ServicoRequest request, AppDbContext db, IOficinaAtual oficinaAtual, CancellationToken ct)
    {
        var servico = new Servico(oficinaAtual.IdObrigatorio(), request.CategoriaId, request.Nome, request.Descricao,
            request.PrecoMinimo, request.PrecoMaximo, request.TempoEstimadoMin);

        if (await Validar(db, servico, ct) is { } problema)
            return problema;

        db.Servicos.Add(servico);
        await db.SaveChangesAsync(ct);

        var resposta = await ParaResponse(db.Servicos.Where(s => s.Id == servico.Id), db).FirstAsync(ct);
        return TypedResults.Created($"/api/servicos/{servico.Id}", resposta);
    }

    private static async Task<Results<Ok<ServicoResponse>, ProblemHttpResult>> Atualizar(
        Guid id, ServicoRequest request, AppDbContext db, CancellationToken ct)
    {
        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (servico is null)
            return Problemas.NaoEncontrado("Serviço não encontrado.");

        servico.Atualizar(request.CategoriaId, request.Nome, request.Descricao,
            request.PrecoMinimo, request.PrecoMaximo, request.TempoEstimadoMin);

        if (await Validar(db, servico, ct) is { } problema)
            return problema;

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(await ParaResponse(db.Servicos.Where(s => s.Id == id), db).FirstAsync(ct));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DefinirAtivo(Guid id, bool ativo, AppDbContext db, CancellationToken ct)
    {
        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (servico is null)
            return Problemas.NaoEncontrado("Serviço não encontrado.");

        if (ativo) servico.Ativar();
        else servico.Desativar();

        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Regras que dependem do banco (o Domínio não acessa o banco, então elas ficam aqui).
    /// </summary>
    private static async Task<ProblemHttpResult?> Validar(AppDbContext db, Servico servico, CancellationToken ct)
    {
        // Segurança: a categoria precisa ser DESTA oficina. Como o filtro global esconde as das outras,
        // um id de categoria de outra oficina simplesmente não é encontrado.
        if (!await db.Categorias.AnyAsync(c => c.Id == servico.CategoriaId, ct))
            return Problemas.Invalido("Categoria não encontrada.");

        if (await db.Servicos.AnyAsync(s => s.Id != servico.Id && s.Nome.ToLower() == servico.Nome.ToLower(), ct))
            return Problemas.Conflito($"Já existe um serviço chamado \"{servico.Nome}\".");

        return null;
    }

    // Junta o serviço com o nome da categoria. O EF transforma isto num único SELECT com JOIN.
    // A ordenação vem ANTES do select: depois de virar ServicoResponse, o EF não sabe mais traduzir para SQL.
    private static IQueryable<ServicoResponse> ParaResponse(IQueryable<Servico> servicos, AppDbContext db) =>
        from s in servicos
        join c in db.Categorias on s.CategoriaId equals c.Id
        orderby c.Nome, s.Nome
        select new ServicoResponse(s.Id, s.CategoriaId, c.Nome, s.Nome, s.Descricao,
            s.PrecoMinimo, s.PrecoMaximo, s.TempoEstimadoMin, s.Ativo);
}
