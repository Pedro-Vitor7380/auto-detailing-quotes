using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orcamentos.Api.Comum;
using Orcamentos.Domain.Entidades;
using Orcamentos.Infrastructure.Dados;

namespace Orcamentos.Api.Features.Agenda;

/// <param name="De">Dia em que o carro chega.</param>
/// <param name="Ate">Dia da entrega (no Início, o app marca "entrega" nesse dia).</param>
public record CarroAgendado(Guid OrcamentoId, string Numero, string ClienteNome, string Placa, string Modelo, DateOnly De, DateOnly Ate);

/// <param name="Aberto">False em dia que a oficina não abre (domingo) ou que já passou: não dá para começar ou terminar ali.</param>
/// <param name="Ocupados">Quantos carros estão na oficina nesse dia (todos os períodos que passam por ele).</param>
public record DiaDaAgenda(DateOnly Data, bool Aberto, int Ocupados, List<CarroAgendado> Carros);

public record AgendaResponse(List<DiaDaAgenda> Dias);

/// <param name="AvisadoEm">Quando alguém da equipe avisou o cliente (null = ainda não avisou).</param>
public record EntregaResponse(Guid OrcamentoId, string Numero, string ClienteNome, string ClienteWhatsApp,
    string Placa, string Modelo, DateOnly De, DateOnly Ate, DateTimeOffset? AvisadoEm);

public static class AgendaEndpoints
{
    private const int MaximoDeDias = 62; // ~2 meses: o calendário mostra um mês por vez

    public static IEndpointRouteBuilder MapAgendaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/agenda", Obter)
            .WithTags("Agenda")
            .WithSummary("Dias de um período com quantos carros estão na oficina (para colorir o calendário)");
        app.MapGet("/api/agenda/entregas", Entregas)
            .WithTags("Agenda")
            .WithSummary("Carros com entrega num dia (padrão: amanhã), para o quadro \"Terminam amanhã\"");
        return app;
    }

    private static async Task<Results<Ok<AgendaResponse>, ProblemHttpResult>> Obter(
        DateOnly de, DateOnly ate, AppDbContext db, Relogio relogio, CancellationToken ct)
    {
        if (ate < de || ate.DayNumber - de.DayNumber > MaximoDeDias)
            return Problemas.Invalido($"Período inválido (até {MaximoDeDias} dias).");

        // Um agendamento aparece no mês se o período dele CRUZA o mês: começa antes do fim do mês e
        // termina depois do início. Ex.: um carro de 30/10 a 03/11 aparece em outubro E em novembro.
        var agendados = await (
            from o in db.Orcamentos
            where o.Status == StatusDoOrcamento.Aprovado && o.AgendadoDe <= ate && o.AgendadoAte >= de
            join c in db.Clientes on o.ClienteId equals c.Id
            join v in db.Veiculos on o.VeiculoId equals v.Id
            orderby o.Sequencial
            select new { o.Id, o.Ano, o.Sequencial, Cliente = c.Nome, v.Placa, v.Modelo, De = o.AgendadoDe!.Value, Ate = o.AgendadoAte!.Value })
            .ToListAsync(ct);

        var carros = agendados
            .Select(a => new CarroAgendado(a.Id, Orcamento.FormatarNumero(a.Ano, a.Sequencial), a.Cliente, a.Placa, a.Modelo, a.De, a.Ate))
            .ToList();

        var hoje = relogio.Hoje;
        var dias = Enumerable.Range(0, ate.DayNumber - de.DayNumber + 1)
            .Select(de.AddDays)
            .Select(dia =>
            {
                // Todos os carros cujo período passa por este dia (inclusive o primeiro e o último).
                var noDia = carros.Where(c => c.De <= dia && dia <= c.Ate).ToList();
                return new DiaDaAgenda(dia, Oficina.AbreEm(dia) && dia >= hoje, noDia.Count, noDia);
            })
            .ToList();

        return TypedResults.Ok(new AgendaResponse(dias));
    }

    private static async Task<Ok<List<EntregaResponse>>> Entregas(AppDbContext db, Relogio relogio, CancellationToken ct, DateOnly? dia = null)
    {
        var entrega = dia ?? relogio.Hoje.AddDays(1);
        var lista = await (
            from o in db.Orcamentos
            where o.Status == StatusDoOrcamento.Aprovado && o.AgendadoAte == entrega
            join c in db.Clientes on o.ClienteId equals c.Id
            join v in db.Veiculos on o.VeiculoId equals v.Id
            orderby o.Sequencial
            select new { o.Id, o.Ano, o.Sequencial, c.Nome, c.WhatsApp, v.Placa, v.Modelo, De = o.AgendadoDe!.Value, Ate = o.AgendadoAte!.Value, o.AvisoDeEntregaEm })
            .ToListAsync(ct);

        return TypedResults.Ok(lista.Select(e => new EntregaResponse(e.Id, Orcamento.FormatarNumero(e.Ano, e.Sequencial), e.Nome,
            e.WhatsApp, e.Placa, e.Modelo, e.De, e.Ate, e.AvisoDeEntregaEm)).ToList());
    }
}
