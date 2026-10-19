using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orcamentos.Api.Comum;
using Orcamentos.Api.Features.Orcamentos;
using Orcamentos.Domain.Entidades;
using Orcamentos.Infrastructure.Dados;

namespace Orcamentos.Api.Features.Painel;

/// <param name="EmAberto">Rascunhos e enviados (ainda podem virar faturamento).</param>
/// <param name="ValorEmAberto">Soma dos enviados: o que está "na mão do cliente" esperando resposta.</param>
/// <param name="FaturamentoPrevisto">Soma dos aprovados no mês (pelo dia da aprovação, no fuso da oficina).</param>
public record PainelResponse(
    int Ano, int Mes, int EmAberto, decimal ValorEmAberto, int AprovadosNoMes, decimal FaturamentoPrevisto);

public static class PainelEndpoints
{
    public static IEndpointRouteBuilder MapPainelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/painel", Obter)
            .WithTags("Painel")
            .WithSummary("Números do Início: em aberto e faturamento previsto do mês (padrão: o mês atual)");
        return app;
    }

    private static async Task<Results<Ok<PainelResponse>, ProblemHttpResult>> Obter(
        AppDbContext db, Relogio relogio, CancellationToken ct, int? ano = null, int? mes = null)
    {
        var hoje = relogio.Hoje;
        var (a, m) = (ano ?? hoje.Year, mes ?? hoje.Month);
        if (m is < 1 or > 12 || a is < 2000 or > 2100)
            return Problemas.Invalido("Mês inválido.");

        // Os vencidos viram Expirado antes de contar (a mesma regra das telas de orçamento).
        await OrcamentosEndpoints.ExpirarVencidos(db, relogio, ct);

        // O banco guarda instantes em UTC; o "mês" é o de São Paulo. Ex.: outubro = de 01/10 00:00 (03:00 UTC)
        // até 01/11 00:00 (03:00 UTC). Uma aprovação em 31/10 às 23h30 (02:30 UTC de 01/11) conta em outubro.
        var inicio = Relogio.InicioDoDia(new DateOnly(a, m, 1));
        var fim = Relogio.InicioDoDia(new DateOnly(a, m, 1).AddMonths(1));

        // O total é calculado no C# (soma dos itens), então os orçamentos vêm com os itens.
        var aprovados = await db.Orcamentos.AsNoTracking()
            .Where(o => o.Status == StatusDoOrcamento.Aprovado && o.AprovadoEm >= inicio && o.AprovadoEm < fim)
            .ToListAsync(ct);
        var enviados = await db.Orcamentos.AsNoTracking()
            .Where(o => o.Status == StatusDoOrcamento.Enviado)
            .ToListAsync(ct);
        var rascunhos = await db.Orcamentos.CountAsync(o => o.Status == StatusDoOrcamento.Rascunho, ct);

        return TypedResults.Ok(new PainelResponse(a, m,
            EmAberto: rascunhos + enviados.Count,
            ValorEmAberto: enviados.Sum(o => o.Total),
            AprovadosNoMes: aprovados.Count,
            FaturamentoPrevisto: aprovados.Sum(o => o.Total)));
    }
}
