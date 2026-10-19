using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orcamentos.Api.Comum;
using Orcamentos.Api.Features.Auth;
using Orcamentos.Domain.Entidades;
using Orcamentos.Infrastructure.Dados;
using Orcamentos.Infrastructure.Pdf;

namespace Orcamentos.Api.Features.Orcamentos;

/// <summary>Item enviado pelo app. Com Id = item que já existe; sem Id = item novo (do catálogo se tiver ServicoId).</summary>
public record ItemRequest(
    Guid? Id, Guid? ServicoId, string? Descricao, int Quantidade, decimal? ValorUnitario,
    string? ObservacaoEstado);

/// <summary>O orçamento inteiro, como está na tela. O mesmo formato serve para criar e para salvar.</summary>
public record SalvarOrcamentoRequest(
    Guid ClienteId, Guid VeiculoId, List<ItemRequest>? Itens,
    TipoDeDesconto DescontoTipo, decimal DescontoValor, DateOnly? Validade, string? Observacoes);

public record ItemResponse(
    Guid Id, Guid? ServicoId, string Descricao, int Quantidade, decimal? PrecoCatalogo, decimal ValorUnitario,
    decimal? Ajuste, decimal Total, string? ObservacaoEstado);

public record OrcamentoResponse(
    Guid Id, string Numero, StatusDoOrcamento Status, bool PodeSerEditado,
    Guid ClienteId, string ClienteNome, string ClienteWhatsApp, Guid VeiculoId, string Placa, string Modelo,
    List<ItemResponse> Itens, decimal Subtotal, TipoDeDesconto DescontoTipo, decimal DescontoValor,
    decimal ValorDoDesconto, decimal Total, DateOnly Validade, string? Observacoes,
    DateTimeOffset CriadoEm, DateTimeOffset? EnviadoEm, DateTimeOffset? AprovadoEm, DateTimeOffset? RecusadoEm);

public record OrcamentoResumoResponse(
    Guid Id, string Numero, StatusDoOrcamento Status, string ClienteNome, string Placa, string Modelo,
    decimal Total, DateOnly Validade, DateTimeOffset CriadoEm);

public static partial class OrcamentosEndpoints
{
    public const int LimiteDaLista = 50;

    public static IEndpointRouteBuilder MapOrcamentosEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/orcamentos").WithTags("Orçamentos");

        grupo.MapGet("/", Listar).WithSummary($"Lista os mais recentes (até {LimiteDaLista}), com filtro por status e busca");
        grupo.MapGet("/{id:guid}", Obter).WithSummary("Orçamento completo, com itens e totais");
        grupo.MapPost("/", Criar).WithSummary("Cria um orçamento (rascunho)");
        grupo.MapPut("/{id:guid}", Salvar).WithSummary("Salva o orçamento inteiro (cliente, itens, desconto, validade)");
        grupo.MapDelete("/{id:guid}", Excluir).WithSummary("Exclui um rascunho");

        grupo.MapPost("/{id:guid}/enviar", (Guid id, AppDbContext db, Relogio relogio, CancellationToken ct) =>
            MudarStatus(id, db, relogio, o => o.MarcarComoEnviado(relogio.Agora), ct)).WithSummary("Marca como enviado ao cliente");
        grupo.MapPost("/{id:guid}/aprovar", (Guid id, AppDbContext db, Relogio relogio, CancellationToken ct) =>
            MudarStatus(id, db, relogio, o => o.Aprovar(relogio.Agora), ct)).WithSummary("Marca como aprovado pelo cliente");
        grupo.MapPost("/{id:guid}/recusar", (Guid id, AppDbContext db, Relogio relogio, CancellationToken ct) =>
            MudarStatus(id, db, relogio, o => o.Recusar(relogio.Agora), ct)).WithSummary("Marca como recusado pelo cliente");
        grupo.MapPost("/{id:guid}/duplicar", Duplicar).WithSummary("Cria um rascunho novo com os mesmos dados");
        grupo.MapGet("/{id:guid}/pdf", Pdf).WithSummary("PDF do orçamento, para enviar ao cliente");

        return app;
    }

    private static async Task<Ok<List<OrcamentoResumoResponse>>> Listar(
        AppDbContext db, Relogio relogio, CancellationToken ct, StatusDoOrcamento? status = null, string? busca = null)
    {
        await ExpirarVencidos(db, relogio, ct);

        var consulta = db.Orcamentos.AsQueryable();
        if (status is not null)
            consulta = consulta.Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var termo = $"%{busca.Trim()}%";
            var placa = SoLetrasENumeros().Replace(busca, "").ToUpperInvariant();
            // "ORC-2026-0003", "0003" ou "3" acham o orçamento 3. Só quando a busca INTEIRA é um número de
            // orçamento: toda placa termina em números, e "ABC1D23" não deve achar o orçamento 23.
            var numero = NumeroDoOrcamento().Match(busca.Trim()) is { Success: true } m && int.TryParse(m.Groups[1].Value, out var n)
                ? n
                : (int?)null;

            consulta = consulta.Where(o =>
                (numero != null && o.Sequencial == numero)
                || db.Clientes.Any(c => c.Id == o.ClienteId && EF.Functions.ILike(EF.Functions.Unaccent(c.Nome), EF.Functions.Unaccent(termo)))
                || (placa.Length >= 3 && db.Veiculos.Any(v => v.Id == o.VeiculoId && v.Placa.Contains(placa))));
        }

        // Total é calculado no C# (soma dos itens), então os orçamentos vêm com os itens e calculamos aqui.
        var orcamentos = await consulta.OrderByDescending(o => o.CriadoEm).Take(LimiteDaLista).ToListAsync(ct);
        var nomes = await NomesDeClientesEVeiculos(db, orcamentos, ct);

        return TypedResults.Ok(orcamentos.Select(o => new OrcamentoResumoResponse(
            o.Id, o.Numero, o.Status, nomes.Clientes[o.ClienteId].Nome, nomes.Veiculos[o.VeiculoId].Placa,
            nomes.Veiculos[o.VeiculoId].Modelo, o.Total, o.Validade, o.CriadoEm)).ToList());
    }

    private static async Task<Results<Ok<OrcamentoResponse>, ProblemHttpResult>> Obter(
        Guid id, AppDbContext db, Relogio relogio, CancellationToken ct)
    {
        await ExpirarVencidos(db, relogio, ct);
        var orcamento = await db.Orcamentos.FirstOrDefaultAsync(o => o.Id == id, ct);
        return orcamento is null ? NaoEncontrado() : TypedResults.Ok(await ParaResponse(db, orcamento, ct));
    }

    private static async Task<Results<Created<OrcamentoResponse>, ProblemHttpResult>> Criar(
        SalvarOrcamentoRequest request, AppDbContext db, IOficinaAtual oficinaAtual, Relogio relogio, CancellationToken ct)
    {
        var hoje = relogio.Hoje;
        var oficinaId = oficinaAtual.IdObrigatorio();

        var resultado = await GravarComNumeroNovo(db, oficinaId, hoje.Year, sequencial =>
        {
            var orcamento = new Orcamento(oficinaId, hoje.Year, sequencial, request.ClienteId, request.VeiculoId, hoje);
            db.Orcamentos.Add(orcamento);
            return orcamento;
        }, orcamento => Aplicar(db, orcamento, request, hoje, ct), ct);

        return resultado.Problema is { } problema
            ? problema
            : TypedResults.Created($"/api/orcamentos/{resultado.Orcamento!.Id}", await ParaResponse(db, resultado.Orcamento, ct));
    }

    private static async Task<Results<Ok<OrcamentoResponse>, ProblemHttpResult>> Salvar(
        Guid id, SalvarOrcamentoRequest request, AppDbContext db, Relogio relogio, CancellationToken ct)
    {
        await ExpirarVencidos(db, relogio, ct);
        var orcamento = await db.Orcamentos.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (orcamento is null)
            return NaoEncontrado();

        if (await Aplicar(db, orcamento, request, relogio.Hoje, ct) is { } problema)
            return problema;

        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(await ParaResponse(db, orcamento, ct));
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Excluir(Guid id, AppDbContext db, CancellationToken ct)
    {
        var orcamento = await db.Orcamentos.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (orcamento is null)
            return NaoEncontrado();

        orcamento.GarantirQuePodeSerExcluido();
        db.Orcamentos.Remove(orcamento); // os itens vão junto (cascade)
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<OrcamentoResponse>, ProblemHttpResult>> MudarStatus(
        Guid id, AppDbContext db, Relogio relogio, Action<Orcamento> mudanca, CancellationToken ct)
    {
        // Expira antes: um orçamento vencido não pode ser aprovado como se ainda valesse.
        await ExpirarVencidos(db, relogio, ct);
        var orcamento = await db.Orcamentos.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (orcamento is null)
            return NaoEncontrado();

        mudanca(orcamento); // o Domínio valida a transição; se não puder, o TratadorDeErros responde 400
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(await ParaResponse(db, orcamento, ct));
    }

    private static async Task<Results<Created<OrcamentoResponse>, ProblemHttpResult>> Duplicar(
        Guid id, AppDbContext db, Relogio relogio, CancellationToken ct)
    {
        var original = await db.Orcamentos.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);
        if (original is null)
            return NaoEncontrado();

        var hoje = relogio.Hoje;
        var resultado = await GravarComNumeroNovo(db, original.OficinaId, hoje.Year, sequencial =>
        {
            var copia = original.Duplicar(hoje.Year, sequencial, hoje);
            db.Orcamentos.Add(copia);
            return copia;
        }, aplicar: null, ct);

        return resultado.Problema is { } problema
            ? problema
            : TypedResults.Created($"/api/orcamentos/{resultado.Orcamento!.Id}", await ParaResponse(db, resultado.Orcamento, ct));
    }

    private static async Task<Results<FileContentHttpResult, ProblemHttpResult>> Pdf(
        Guid id, AppDbContext db, Relogio relogio, CancellationToken ct)
    {
        await ExpirarVencidos(db, relogio, ct);
        var orcamento = await db.Orcamentos.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);
        if (orcamento is null)
            return NaoEncontrado();

        var oficina = await db.Oficinas.AsNoTracking().FirstAsync(o => o.Id == orcamento.OficinaId, ct);
        var nomes = await NomesDeClientesEVeiculos(db, [orcamento], ct);
        var cliente = nomes.Clientes[orcamento.ClienteId];
        var veiculo = nomes.Veiculos[orcamento.VeiculoId];

        var pdf = PdfDoOrcamento.Gerar(new DadosDoPdf(oficina, orcamento, cliente.Nome, cliente.WhatsApp,
            veiculo.Placa, veiculo.Modelo, Relogio.DataNoFuso(orcamento.CriadoEm)));

        // O nome do arquivo ("ORC-2026-0001.pdf") é o que aparece para o cliente no WhatsApp.
        return TypedResults.File(pdf, "application/pdf", $"{orcamento.Numero}.pdf");
    }

    // ===== Regras que precisam do banco =====

    /// <summary>
    /// Leva o que veio do app para o orçamento: confere cliente e veículo, sincroniza os itens
    /// (atualiza os que já existem, adiciona os novos, remove os que sumiram) e aplica desconto, validade e observações.
    /// As regras em si estão no Domínio; aqui fica só o que depende do banco.
    /// </summary>
    private static async Task<ProblemHttpResult?> Aplicar(
        AppDbContext db, Orcamento orcamento, SalvarOrcamentoRequest request, DateOnly hoje, CancellationToken ct)
    {
        // O veículo precisa ser DESTE cliente (e, pelo filtro global, desta oficina).
        if (!await db.Veiculos.AnyAsync(v => v.Id == request.VeiculoId && v.ClienteId == request.ClienteId, ct))
            return Problemas.Invalido("Escolha um veículo do cliente selecionado.");
        orcamento.DefinirClienteEVeiculo(request.ClienteId, request.VeiculoId);

        var itens = request.Itens ?? [];
        var idsExistentes = orcamento.Itens.Select(i => i.Id).ToHashSet();

        foreach (var removido in idsExistentes.Except(itens.Where(i => i.Id is not null).Select(i => i.Id!.Value)).ToList())
            orcamento.RemoverItem(removido);

        // Busca de uma vez só os serviços dos itens novos (o filtro global garante: só desta oficina).
        var idsDeServicos = itens.Where(i => i.Id is null && i.ServicoId is not null).Select(i => i.ServicoId!.Value).Distinct().ToList();
        var servicos = await db.Servicos.Where(s => idsDeServicos.Contains(s.Id)).ToDictionaryAsync(s => s.Id, ct);

        foreach (var item in itens)
        {
            if (item.Id is { } itemId)
            {
                if (!idsExistentes.Contains(itemId))
                    return Problemas.Invalido("Um dos itens não pertence a este orçamento. Recarregue a tela.");
                orcamento.AtualizarItem(itemId, item.Quantidade, item.ValorUnitario ?? 0, item.ObservacaoEstado, item.Descricao);
            }
            else if (item.ServicoId is { } servicoId)
            {
                if (!servicos.TryGetValue(servicoId, out var servico))
                    return Problemas.Invalido("Serviço não encontrado no catálogo.");
                orcamento.AdicionarServico(servico, item.Quantidade, item.ValorUnitario, item.ObservacaoEstado);
            }
            else
            {
                orcamento.AdicionarAvulso(item.Descricao ?? "", item.Quantidade, item.ValorUnitario ?? 0, item.ObservacaoEstado);
            }
        }

        // Depois dos itens: o desconto em R$ é conferido contra o subtotal já atualizado.
        orcamento.DefinirDesconto(request.DescontoTipo, request.DescontoValor);
        if (request.Validade is { } validade && validade != orcamento.Validade)
            orcamento.DefinirValidade(validade, hoje);
        orcamento.DefinirObservacoes(request.Observacoes);

        return null;
    }

    /// <summary>
    /// Pega o próximo número (ContadorDeOrcamentos) e grava o orçamento numa ÚNICA transação.
    /// Pedidos simultâneos esperam um pelo outro no contador, então nunca saem dois números iguais.
    /// Se algo falhar no meio (ex.: item inválido), a transação é desfeita e o número volta: sem "buracos".
    /// </summary>
    private static async Task<(Orcamento? Orcamento, ProblemHttpResult? Problema)> GravarComNumeroNovo(
        AppDbContext db, Guid oficinaId, int ano, Func<int, Orcamento> criar, Func<Orcamento, Task<ProblemHttpResult?>>? aplicar, CancellationToken ct)
    {
        // "await using": ao sair do método sem Commit (erro ou return antecipado), a transação é desfeita (rollback).
        await using var transacao = await db.Database.BeginTransactionAsync(ct);

        var orcamento = criar(await ContadorDeOrcamentos.ProximoAsync(db, oficinaId, ano, ct));
        if (aplicar is not null && await aplicar(orcamento) is { } problema)
            return (null, problema);

        await db.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);
        return (orcamento, null);
    }

    /// <summary>
    /// A mesma regra de Orcamento.ExpirarSeVencido, mas direto no banco: um único UPDATE para todos os vencidos,
    /// sem carregar cada orçamento. Roda antes de ler, então quem consulta sempre vê o status certo.
    /// internal: o Painel também usa (uma regra, um lugar só).
    /// </summary>
    internal static Task ExpirarVencidos(AppDbContext db, Relogio relogio, CancellationToken ct)
    {
        var hoje = relogio.Hoje;
        return db.Orcamentos
            .Where(o => o.Status == StatusDoOrcamento.Enviado && o.Validade < hoje)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, StatusDoOrcamento.Expirado), ct);
    }

    private static async Task<OrcamentoResponse> ParaResponse(AppDbContext db, Orcamento o, CancellationToken ct)
    {
        var nomes = await NomesDeClientesEVeiculos(db, [o], ct);
        var cliente = nomes.Clientes[o.ClienteId];
        var veiculo = nomes.Veiculos[o.VeiculoId];

        // Ordem em que os itens foram adicionados (o Id não serve: a ordem de Guid no C# não é a de criação).
        var itens = o.Itens.OrderBy(i => i.CriadoEm).Select(i => new ItemResponse(
            i.Id, i.ServicoId, i.Descricao, i.Quantidade, i.PrecoCatalogo, i.ValorUnitario, i.Ajuste, i.Total,
            i.ObservacaoEstado)).ToList();

        return new OrcamentoResponse(o.Id, o.Numero, o.Status, o.PodeSerEditado,
            o.ClienteId, cliente.Nome, cliente.WhatsApp, o.VeiculoId, veiculo.Placa, veiculo.Modelo,
            itens, o.Subtotal, o.DescontoTipo, o.DescontoValor, o.ValorDoDesconto, o.Total,
            o.Validade, o.Observacoes, o.CriadoEm, o.EnviadoEm, o.AprovadoEm, o.RecusadoEm);
    }

    private record NomesDoCliente(string Nome, string WhatsApp);
    private record NomesDoVeiculo(string Placa, string Modelo);

    // Duas consultas (clientes e veículos) para todos os orçamentos da lista, em vez de duas POR orçamento.
    private static async Task<(Dictionary<Guid, NomesDoCliente> Clientes, Dictionary<Guid, NomesDoVeiculo> Veiculos)> NomesDeClientesEVeiculos(
        AppDbContext db, IReadOnlyCollection<Orcamento> orcamentos, CancellationToken ct)
    {
        var idsClientes = orcamentos.Select(o => o.ClienteId).Distinct().ToList();
        var idsVeiculos = orcamentos.Select(o => o.VeiculoId).Distinct().ToList();

        var clientes = await db.Clientes.Where(c => idsClientes.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => new NomesDoCliente(c.Nome, c.WhatsApp), ct);
        var veiculos = await db.Veiculos.Where(v => idsVeiculos.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => new NomesDoVeiculo(v.Placa, v.Modelo), ct);

        return (clientes, veiculos);
    }

    private static ProblemHttpResult NaoEncontrado() => Problemas.NaoEncontrado("Orçamento não encontrado.");

    [GeneratedRegex("[^A-Za-z0-9]")]
    private static partial Regex SoLetrasENumeros();

    [GeneratedRegex(@"^(?:ORC-?\d{4}-?)?(\d{1,6})$", RegexOptions.IgnoreCase)]
    private static partial Regex NumeroDoOrcamento();
}
