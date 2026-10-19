using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orcamentos.Api.Comum;
using Orcamentos.Api.Features.Auth;
using Orcamentos.Domain.Entidades;
using Orcamentos.Infrastructure.Dados;

namespace Orcamentos.Api.Features.Clientes;

public record VeiculoRequest(string Placa, string Modelo);
public record CriarClienteRequest(string Nome, string WhatsApp, List<VeiculoRequest>? Veiculos);
public record AtualizarClienteRequest(string Nome, string WhatsApp);

public record VeiculoResponse(Guid Id, string Placa, string Modelo);
public record ClienteResponse(Guid Id, string Nome, string WhatsApp, List<VeiculoResponse> Veiculos);

public static partial class ClientesEndpoints
{
    /// <summary>A busca devolve no máximo isto. Clientes crescem sem parar; serviços, não.</summary>
    public const int LimiteDaBusca = 50;

    public static IEndpointRouteBuilder MapClientesEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/clientes").WithTags("Clientes");

        grupo.MapGet("/", Listar).WithSummary($"Busca por nome, placa ou WhatsApp (até {LimiteDaBusca} resultados)");
        grupo.MapGet("/{id:guid}", Obter).WithSummary("Dados de um cliente e dos seus veículos");
        grupo.MapPost("/", Criar).WithSummary("Cria um cliente, já com os veículos (opcional)");
        grupo.MapPut("/{id:guid}", Atualizar).WithSummary("Altera nome e WhatsApp");
        grupo.MapDelete("/{id:guid}", Excluir).WithSummary("Exclui o cliente e os seus veículos");

        grupo.MapPost("/{id:guid}/veiculos", AdicionarVeiculo).WithSummary("Adiciona um veículo ao cliente");
        grupo.MapPut("/{id:guid}/veiculos/{veiculoId:guid}", AtualizarVeiculo).WithSummary("Altera placa e modelo");
        grupo.MapDelete("/{id:guid}/veiculos/{veiculoId:guid}", RemoverVeiculo).WithSummary("Remove um veículo do cliente");

        return app;
    }

    private static async Task<Ok<List<ClienteResponse>>> Listar(AppDbContext db, CancellationToken ct, string? busca = null)
    {
        var consulta = db.Clientes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = $"%{busca.Trim()}%";
            var placa = SoLetrasENumeros().Replace(busca, "").ToUpperInvariant(); // "abc-1d" → "ABC1D"
            var numeros = SoNumeros().Replace(busca, "");

            // Uma caixa de busca só, para três coisas. Assim, no pátio, digita-se o que estiver à mão.
            consulta = consulta.Where(c =>
                // unaccent dos dois lados: "joao" acha "João" e "JOÃO".
                EF.Functions.ILike(EF.Functions.Unaccent(c.Nome), EF.Functions.Unaccent(termo))
                || (placa.Length >= 3 && c.Veiculos.Any(v => v.Placa.Contains(placa)))
                || (numeros.Length >= 4 && c.WhatsApp.Contains(numeros)));
        }

        var clientes = await consulta
            .OrderBy(c => c.Nome)
            .Take(LimiteDaBusca)
            .Select(c => new ClienteResponse(c.Id, c.Nome, c.WhatsApp,
                c.Veiculos.OrderBy(v => v.Placa).Select(v => new VeiculoResponse(v.Id, v.Placa, v.Modelo)).ToList()))
            .ToListAsync(ct);

        return TypedResults.Ok(clientes);
    }

    private static async Task<Results<Ok<ClienteResponse>, ProblemHttpResult>> Obter(Guid id, AppDbContext db, CancellationToken ct)
    {
        var cliente = await Carregar(db, id, ct);
        return cliente is null ? ClienteNaoEncontrado() : TypedResults.Ok(ParaResponse(cliente));
    }

    private static async Task<Results<Created<ClienteResponse>, ProblemHttpResult>> Criar(
        CriarClienteRequest request, AppDbContext db, IOficinaAtual oficinaAtual, CancellationToken ct)
    {
        var cliente = new Cliente(oficinaAtual.IdObrigatorio(), request.Nome, request.WhatsApp);
        foreach (var v in request.Veiculos ?? [])
            cliente.AdicionarVeiculo(v.Placa, v.Modelo);

        foreach (var veiculo in cliente.Veiculos)
            if (await PlacaEmUso(db, veiculo.Placa, veiculoIgnorado: null, ct) is { } problema)
                return problema;

        db.Clientes.Add(cliente); // os veículos vão junto: o EF grava o agregado inteiro
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/clientes/{cliente.Id}", ParaResponse(cliente));
    }

    private static async Task<Results<Ok<ClienteResponse>, ProblemHttpResult>> Atualizar(
        Guid id, AtualizarClienteRequest request, AppDbContext db, CancellationToken ct)
    {
        var cliente = await Carregar(db, id, ct);
        if (cliente is null)
            return ClienteNaoEncontrado();

        cliente.Atualizar(request.Nome, request.WhatsApp);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(ParaResponse(cliente));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Excluir(Guid id, AppDbContext db, CancellationToken ct)
    {
        var cliente = await Carregar(db, id, ct);
        if (cliente is null)
            return ClienteNaoEncontrado();

        // Na semana 3, um cliente com orçamentos não poderá ser excluído (os orçamentos apontam para ele).
        db.Clientes.Remove(cliente);
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<ClienteResponse>, ProblemHttpResult>> AdicionarVeiculo(
        Guid id, VeiculoRequest request, AppDbContext db, CancellationToken ct)
    {
        var cliente = await Carregar(db, id, ct);
        if (cliente is null)
            return ClienteNaoEncontrado();

        var veiculo = cliente.AdicionarVeiculo(request.Placa, request.Modelo);
        if (await PlacaEmUso(db, veiculo.Placa, veiculoIgnorado: null, ct) is { } problema)
            return problema;

        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(ParaResponse(cliente));
    }

    private static async Task<Results<Ok<ClienteResponse>, ProblemHttpResult>> AtualizarVeiculo(
        Guid id, Guid veiculoId, VeiculoRequest request, AppDbContext db, CancellationToken ct)
    {
        var cliente = await Carregar(db, id, ct);
        if (cliente is null)
            return ClienteNaoEncontrado();

        cliente.AtualizarVeiculo(veiculoId, request.Placa, request.Modelo);
        if (await PlacaEmUso(db, Veiculo.NormalizarPlaca(request.Placa), veiculoIgnorado: veiculoId, ct) is { } problema)
            return problema;

        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(ParaResponse(cliente));
    }

    private static async Task<Results<Ok<ClienteResponse>, ProblemHttpResult>> RemoverVeiculo(
        Guid id, Guid veiculoId, AppDbContext db, CancellationToken ct)
    {
        var cliente = await Carregar(db, id, ct);
        if (cliente is null)
            return ClienteNaoEncontrado();

        cliente.RemoverVeiculo(veiculoId);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(ParaResponse(cliente));
    }

    // Include: carrega os veículos junto (um JOIN). Sem ele, cliente.Veiculos viria vazio.
    private static Task<Cliente?> Carregar(AppDbContext db, Guid id, CancellationToken ct) =>
        db.Clientes.Include(c => c.Veiculos).FirstOrDefaultAsync(c => c.Id == id, ct);

    /// <summary>Placa única na oficina: precisa consultar o banco, por isso fica aqui e não no Domínio.</summary>
    private static async Task<ProblemHttpResult?> PlacaEmUso(AppDbContext db, string placa, Guid? veiculoIgnorado, CancellationToken ct)
    {
        var dono = await db.Veiculos
            .Where(v => v.Placa == placa && v.Id != veiculoIgnorado)
            .Join(db.Clientes, v => v.ClienteId, c => c.Id, (v, c) => c.Nome)
            .FirstOrDefaultAsync(ct);

        return dono is null ? null : Problemas.Conflito($"A placa {placa} já está cadastrada para {dono}.");
    }

    private static ProblemHttpResult ClienteNaoEncontrado() => Problemas.NaoEncontrado("Cliente não encontrado.");

    private static ClienteResponse ParaResponse(Cliente c) =>
        new(c.Id, c.Nome, c.WhatsApp,
            c.Veiculos.OrderBy(v => v.Placa).Select(v => new VeiculoResponse(v.Id, v.Placa, v.Modelo)).ToList());

    [GeneratedRegex("[^A-Za-z0-9]")]
    private static partial Regex SoLetrasENumeros();

    [GeneratedRegex("[^0-9]")]
    private static partial Regex SoNumeros();
}
