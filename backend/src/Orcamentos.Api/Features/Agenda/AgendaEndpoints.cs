using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orcamentos.Api.Comum;
using Orcamentos.Domain.Entidades;
using Orcamentos.Infrastructure.Dados;

namespace Orcamentos.Api.Features.Agenda;

public record CarroAgendado(Guid OrcamentoId, string Numero, string ClienteNome, string Placa, string Modelo);

/// <param name="Aberto">False em dia que a oficina não abre (domingo) ou que já passou: não dá para agendar.</param>
public record DiaDaAgenda(DateOnly Data, bool Aberto, int Ocupados, List<CarroAgendado> Carros);

public record AgendaResponse(int CarrosPorDia, List<DiaDaAgenda> Dias);

public static class AgendaEndpoints
{
    private const int MaximoDeDias = 62; // ~2 meses: o calendário mostra um mês por vez

    public static IEndpointRouteBuilder MapAgendaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/agenda", Obter)
            .WithTags("Agenda")
            .WithSummary("Dias de um período com quantos carros estão agendados (para colorir o calendário)");
        return app;
    }

    private static async Task<Results<Ok<AgendaResponse>, ProblemHttpResult>> Obter(
        DateOnly de, DateOnly ate, AppDbContext db, IOficinaAtual oficinaAtual, Relogio relogio, CancellationToken ct)
    {
        if (ate < de || ate.DayNumber - de.DayNumber > MaximoDeDias)
            return Problemas.Invalido($"Período inválido (até {MaximoDeDias} dias).");

        var oficinaId = oficinaAtual.Id;
        var carrosPorDia = await db.Oficinas.Where(o => o.Id == oficinaId).Select(o => o.CarrosPorDia).SingleAsync(ct);

        // Só orçamentos aprovados e agendados no período (o filtro global já limita à oficina).
        var agendados = await (
            from o in db.Orcamentos
            where o.Status == StatusDoOrcamento.Aprovado && o.DataAgendada >= de && o.DataAgendada <= ate
            join c in db.Clientes on o.ClienteId equals c.Id
            join v in db.Veiculos on o.VeiculoId equals v.Id
            orderby o.Sequencial
            select new { Dia = o.DataAgendada!.Value, o.Id, o.Ano, o.Sequencial, Cliente = c.Nome, v.Placa, v.Modelo })
            .ToListAsync(ct);

        // ToLookup: agrupa por dia, como um dicionário em que cada chave tem uma lista (e dia sem nada = lista vazia).
        var porDia = agendados.ToLookup(
            a => a.Dia,
            a => new CarroAgendado(a.Id, Orcamento.FormatarNumero(a.Ano, a.Sequencial), a.Cliente, a.Placa, a.Modelo));

        var hoje = relogio.Hoje;
        var dias = Enumerable.Range(0, ate.DayNumber - de.DayNumber + 1)
            .Select(de.AddDays)
            .Select(dia => new DiaDaAgenda(dia, Oficina.AbreEm(dia) && dia >= hoje, porDia[dia].Count(), porDia[dia].ToList()))
            .ToList();

        return TypedResults.Ok(new AgendaResponse(carrosPorDia, dias));
    }
}
