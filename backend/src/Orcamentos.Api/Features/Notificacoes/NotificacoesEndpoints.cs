using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orcamentos.Api.Comum;
using Orcamentos.Api.Features.Auth;
using Orcamentos.Domain.Entidades;
using Orcamentos.Infrastructure.Dados;
using Orcamentos.Infrastructure.Notificacoes;

namespace Orcamentos.Api.Features.Notificacoes;

public record InscricaoRequest(string Endpoint, string P256dh, string Auth);
public record ChavePublicaResponse(string Chave);

/// <summary>O que o celular mostra. "url" é a tela que abre ao tocar na notificação.</summary>
public record ConteudoDaNotificacao(string Titulo, string Corpo, string Url);

public record ResultadoDaTarefa(DateOnly Entregas, int Oficinas, int Carros, int Enviadas, int Removidas, int Falharam);

public static class NotificacoesEndpoints
{
    public const string CabecalhoDaChave = "X-Chave-Tarefas";

    public static IEndpointRouteBuilder MapNotificacoesEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/notificacoes").WithTags("Notificações");
        grupo.MapGet("/chave-publica", ChavePublica).WithSummary("Chave VAPID pública, que o navegador usa para se inscrever");
        grupo.MapPost("/inscricoes", Inscrever).WithSummary("Este aparelho passa a receber notificações da oficina");
        grupo.MapDelete("/inscricoes", Desinscrever).WithSummary("Este aparelho deixa de receber notificações");
        grupo.MapPost("/teste", Testar).WithSummary("Manda uma notificação de teste para os aparelhos da oficina");

        // Chamado pelo "despertador" (GitHub Actions), não por uma pessoa: não tem login, tem uma chave própria.
        app.MapPost("/api/tarefas/avisar-entregas", AvisarEntregasDeAmanha)
            .AllowAnonymous()
            .WithTags("Tarefas")
            .WithSummary("Notifica a equipe dos serviços que terminam amanhã (rodado 1x por dia)");
        return app;
    }

    private static Results<Ok<ChavePublicaResponse>, ProblemHttpResult> ChavePublica(IServiceProvider servicos) =>
        servicos.GetService<ChavesVapid>() is { } chaves
            ? TypedResults.Ok(new ChavePublicaResponse(chaves.Publica))
            : NaoConfigurado();

    private static async Task<Results<NoContent, ProblemHttpResult>> Inscrever(
        InscricaoRequest request, AppDbContext db, IOficinaAtual oficinaAtual, CancellationToken ct)
    {
        // O mesmo aparelho pode ter se inscrito antes (talvez logado em outra oficina): apaga e cria de novo.
        var anteriores = await db.InscricoesDeNotificacao.IgnoreQueryFilters().Where(i => i.Endpoint == request.Endpoint).ToListAsync(ct);
        db.InscricoesDeNotificacao.RemoveRange(anteriores);
        db.InscricoesDeNotificacao.Add(new InscricaoDeNotificacao(oficinaAtual.IdObrigatorio(), request.Endpoint, request.P256dh, request.Auth));
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> Desinscrever(string endpoint, AppDbContext db, CancellationToken ct)
    {
        await db.InscricoesDeNotificacao.Where(i => i.Endpoint == endpoint).ExecuteDeleteAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<ResultadoDaTarefa>, ProblemHttpResult>> Testar(
        AppDbContext db, IOficinaAtual oficinaAtual, IServiceProvider servicos, Relogio relogio, CancellationToken ct)
    {
        if (servicos.GetService<EnviadorDeNotificacoes>() is not { } enviador)
            return NaoConfigurado();

        var inscricoes = await db.InscricoesDeNotificacao.ToListAsync(ct); // o filtro global limita à oficina
        var conteudo = new ConteudoDaNotificacao("Notificações ativadas", "Este aparelho vai receber o aviso dos serviços que terminam no dia seguinte.", "/");
        var (enviadas, removidas, falharam) = await EnviarParaTodos(db, enviador, inscricoes, conteudo, ct);
        return TypedResults.Ok(new ResultadoDaTarefa(relogio.Hoje, 1, 0, enviadas, removidas, falharam));
    }

    /// <summary>
    /// Para cada oficina com carros a entregar AMANHÃ, notifica todos os aparelhos inscritos.
    /// Roda sem usuário logado, então desliga o filtro por oficina (IgnoreQueryFilters) e separa à mão.
    /// </summary>
    private static async Task<Results<Ok<ResultadoDaTarefa>, ProblemHttpResult, UnauthorizedHttpResult>> AvisarEntregasDeAmanha(
        HttpRequest pedido, IConfiguration configuracao, AppDbContext db, IServiceProvider servicos, Relogio relogio, CancellationToken ct)
    {
        var chaveEsperada = configuracao["Tarefas:Chave"];
        if (string.IsNullOrWhiteSpace(chaveEsperada) || servicos.GetService<EnviadorDeNotificacoes>() is not { } enviador)
            return NaoConfigurado();

        // FixedTimeEquals compara sempre no mesmo tempo, acertando ou errando: o tempo de resposta não dá pistas da chave.
        var recebida = Encoding.UTF8.GetBytes(pedido.Headers[CabecalhoDaChave].ToString());
        if (!CryptographicOperations.FixedTimeEquals(recebida, Encoding.UTF8.GetBytes(chaveEsperada)))
            return TypedResults.Unauthorized();

        var amanha = relogio.Hoje.AddDays(1);
        var entregas = await (
            from o in db.Orcamentos.IgnoreQueryFilters()
            where o.Status == StatusDoOrcamento.Aprovado && o.AgendadoAte == amanha
            join c in db.Clientes.IgnoreQueryFilters() on o.ClienteId equals c.Id
            join v in db.Veiculos.IgnoreQueryFilters() on o.VeiculoId equals v.Id
            select new { o.OficinaId, Cliente = c.Nome, v.Modelo, v.Placa })
            .ToListAsync(ct);

        int enviadas = 0, removidas = 0, falharam = 0;
        foreach (var daOficina in entregas.GroupBy(e => e.OficinaId))
        {
            var inscricoes = await db.InscricoesDeNotificacao.IgnoreQueryFilters().Where(i => i.OficinaId == daOficina.Key).ToListAsync(ct);
            var quantos = daOficina.Count();
            var carros = string.Join(" · ", daOficina.Select(e => $"{e.Modelo} ({e.Placa}), {e.Cliente.Split(' ')[0]}"));
            var conteudo = new ConteudoDaNotificacao(
                quantos == 1 ? "Amanhã: 1 serviço para concluir" : $"Amanhã: {quantos} serviços para concluir",
                $"{carros}. Precisa adiar ou pode avisar o cliente?",
                "/");
            var (e1, r1, f1) = await EnviarParaTodos(db, enviador, inscricoes, conteudo, ct);
            (enviadas, removidas, falharam) = (enviadas + e1, removidas + r1, falharam + f1);
        }

        return TypedResults.Ok(new ResultadoDaTarefa(amanha, entregas.Select(e => e.OficinaId).Distinct().Count(), entregas.Count, enviadas, removidas, falharam));
    }

    private static async Task<(int Enviadas, int Removidas, int Falharam)> EnviarParaTodos(
        AppDbContext db, EnviadorDeNotificacoes enviador, List<InscricaoDeNotificacao> inscricoes, ConteudoDaNotificacao conteudo, CancellationToken ct)
    {
        int enviadas = 0, removidas = 0, falharam = 0;
        foreach (var inscricao in inscricoes)
        {
            switch (await enviador.EnviarAsync(inscricao, conteudo, ct))
            {
                case ResultadoDoEnvio.Enviado: enviadas++; break;
                case ResultadoDoEnvio.InscricaoExpirada: db.InscricoesDeNotificacao.Remove(inscricao); removidas++; break;
                default: falharam++; break;
            }
        }
        await db.SaveChangesAsync(ct); // apaga as inscrições de aparelhos que não existem mais
        return (enviadas, removidas, falharam);
    }

    private static ProblemHttpResult NaoConfigurado() => TypedResults.Problem(
        statusCode: StatusCodes.Status503ServiceUnavailable, title: "Indisponível", detail: "As notificações não estão configuradas no servidor.");
}
