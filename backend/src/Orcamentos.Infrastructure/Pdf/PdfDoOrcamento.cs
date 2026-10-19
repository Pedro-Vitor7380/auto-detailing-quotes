using System.Globalization;
using Orcamentos.Domain.Entidades;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Orcamentos.Infrastructure.Pdf;

/// <summary>Tudo que aparece no PDF. O orçamento sozinho não basta: precisa dos dados da oficina, do cliente e do carro.</summary>
public record DadosDoPdf(
    Oficina Oficina, Orcamento Orcamento, string ClienteNome, string ClienteWhatsApp,
    string Placa, string Modelo, DateOnly EmitidoEm);

/// <summary>
/// Monta o PDF do orçamento com o QuestPDF. A página é descrita em C# como "caixas" dentro de caixas
/// (cabeçalho, conteúdo, rodapé; colunas, linhas, tabela), parecido com montar uma tela.
/// O cliente vê só os valores finais e a observação do estado; o preço de catálogo é informação interna.
/// </summary>
public static class PdfDoOrcamento
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private const string Grafite = "#373334";
    private const string Dourado = "#8A6618"; // o dourado escuro: legível em papel branco
    private const string Cinza = "#64748B";
    private const string Linha = "#E2E8F0";

    public static byte[] Gerar(DadosDoPdf d) =>
        Document.Create(documento => documento.Page(pagina =>
        {
            pagina.Size(PageSizes.A4);
            pagina.Margin(1.6f, Unit.Centimetre);
            pagina.DefaultTextStyle(t => t.FontSize(10).FontColor(Grafite));

            pagina.Header().Element(c => Cabecalho(c, d));
            pagina.Content().PaddingVertical(14).Element(c => Conteudo(c, d));
            pagina.Footer().Element(c => Rodape(c, d));
        }))
        .WithMetadata(new DocumentMetadata { Title = $"Orçamento {d.Orcamento.Numero}", Author = d.Oficina.Nome })
        .GeneratePdf();

    private static void Cabecalho(IContainer container, DadosDoPdf d)
    {
        var oficina = d.Oficina;
        container.BorderBottom(2).BorderColor(Dourado).PaddingBottom(10).Row(linha =>
        {
            if (oficina.Logo is not null)
                linha.ConstantItem(62).PaddingRight(10).Image(oficina.Logo).FitArea();

            linha.RelativeItem().Column(coluna =>
            {
                coluna.Item().Text(oficina.Nome).FontSize(15).Bold();
                // Só mostra o que estiver cadastrado (o comando alterar-oficina preenche).
                foreach (var info in new[] { Rotulo("CNPJ", oficina.Cnpj), oficina.Telefone, oficina.Endereco }.Where(i => i is not null))
                    coluna.Item().Text(info!).FontSize(9).FontColor(Cinza);
            });

            linha.ConstantItem(150).AlignRight().Column(coluna =>
            {
                coluna.Item().AlignRight().Text("ORÇAMENTO").FontSize(9).Bold().FontColor(Dourado).LetterSpacing(0.1f);
                coluna.Item().AlignRight().Text(d.Orcamento.Numero).FontSize(14).Bold();
                coluna.Item().AlignRight().Text($"Emitido em {Data(d.EmitidoEm)}").FontSize(9).FontColor(Cinza);
            });
        });
    }

    private static void Conteudo(IContainer container, DadosDoPdf d)
    {
        var o = d.Orcamento;
        container.Column(coluna =>
        {
            coluna.Spacing(14);

            // Cliente e veículo, lado a lado
            coluna.Item().Row(linha =>
            {
                linha.RelativeItem().Element(c => Bloco(c, "CLIENTE", d.ClienteNome, FormatarWhatsApp(d.ClienteWhatsApp)));
                linha.ConstantItem(12);
                linha.RelativeItem().Element(c => Bloco(c, "VEÍCULO", d.Modelo, FormatarPlaca(d.Placa)));
            });

            // Itens
            coluna.Item().Table(tabela =>
            {
                tabela.ColumnsDefinition(colunas =>
                {
                    colunas.RelativeColumn();
                    colunas.ConstantColumn(36);
                    colunas.ConstantColumn(80);
                    colunas.ConstantColumn(80);
                });

                tabela.Header(cabecalho =>
                {
                    foreach (var (titulo, direita) in new[] { ("Serviço", false), ("Qtd.", true), ("Valor unit.", true), ("Total", true) })
                    {
                        var celula = cabecalho.Cell().Background(Grafite).PaddingVertical(5).PaddingHorizontal(6);
                        (direita ? celula.AlignRight() : celula).Text(titulo).FontSize(9).Bold().FontColor(Colors.White);
                    }
                });

                foreach (var item in o.Itens.OrderBy(i => i.CriadoEm))
                {
                    tabela.Cell().Element(CelulaDoItem).Column(c =>
                    {
                        c.Item().Text(item.Descricao);
                        if (item.ObservacaoEstado is not null)
                            c.Item().Text(item.ObservacaoEstado).FontSize(8.5f).Italic().FontColor(Cinza);
                    });
                    tabela.Cell().Element(CelulaDoItem).AlignRight().Text(item.Quantidade.ToString(PtBr));
                    tabela.Cell().Element(CelulaDoItem).AlignRight().Text(Moeda(item.ValorUnitario));
                    tabela.Cell().Element(CelulaDoItem).AlignRight().Text(Moeda(item.Total)).SemiBold();
                }
            });

            // Totais, alinhados à direita
            coluna.Item().AlignRight().Width(220).Column(totais =>
            {
                if (o.ValorDoDesconto > 0)
                {
                    totais.Item().Element(c => LinhaDeTotal(c, "Subtotal", Moeda(o.Subtotal)));
                    var rotulo = o.DescontoTipo == TipoDeDesconto.Percentual ? $"Desconto ({o.DescontoValor.ToString("0.##", PtBr)}%)" : "Desconto";
                    totais.Item().Element(c => LinhaDeTotal(c, rotulo, $"− {Moeda(o.ValorDoDesconto)}"));
                }
                totais.Item().PaddingTop(4).Background(Grafite).Padding(8).Row(linha =>
                {
                    linha.RelativeItem().Text("TOTAL").Bold().FontColor(Colors.White);
                    linha.AutoItem().Text(Moeda(o.Total)).FontSize(14).Bold().FontColor(Colors.White);
                });
            });

            // Validade. Sem prazo de entrega, de propósito: com vários carros na oficina ao mesmo tempo,
            // a soma dos tempos dos serviços não diz quando ESTE carro fica pronto (pedido da equipe).
            coluna.Item().Background("#FBF6EA").Padding(10).Text(t =>
            {
                t.Span("Validade: ").SemiBold();
                t.Span($"este orçamento vale até {Data(o.Validade)}.");
            });

            if (o.Observacoes is not null)
                coluna.Item().Column(obs =>
                {
                    obs.Item().Text("OBSERVAÇÕES").FontSize(8).Bold().FontColor(Cinza);
                    obs.Item().PaddingTop(2).Text(o.Observacoes);
                });
        });
    }

    private static void Rodape(IContainer container, DadosDoPdf d) =>
        container.BorderTop(0.5f).BorderColor(Linha).PaddingTop(6).Row(linha =>
        {
            linha.RelativeItem().Text($"{d.Oficina.Nome} · {d.Orcamento.Numero}").FontSize(8).FontColor(Cinza);
            linha.AutoItem().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(8).FontColor(Cinza));
                t.Span("Página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
        });

    // ===== Peças reaproveitadas =====

    private static void Bloco(IContainer container, string titulo, string principal, string secundario) =>
        container.Border(0.5f).BorderColor(Linha).Padding(8).Column(c =>
        {
            c.Item().Text(titulo).FontSize(8).Bold().FontColor(Cinza);
            c.Item().PaddingTop(2).Text(principal).FontSize(11).SemiBold();
            c.Item().Text(secundario).FontColor(Cinza);
        });

    private static IContainer CelulaDoItem(IContainer c) => c.BorderBottom(0.5f).BorderColor(Linha).PaddingVertical(6).PaddingHorizontal(6);

    private static void LinhaDeTotal(IContainer container, string rotulo, string valor) =>
        container.PaddingVertical(2).PaddingHorizontal(8).Row(linha =>
        {
            linha.RelativeItem().Text(rotulo).FontColor(Cinza);
            linha.AutoItem().Text(valor);
        });

    // ===== Formatação (pt-BR) =====

    private static string Moeda(decimal valor) => valor.ToString("C", PtBr);
    private static string Data(DateOnly data) => data.ToString("dd/MM/yyyy", PtBr);
    private static string? Rotulo(string rotulo, string? valor) => valor is null ? null : $"{rotulo}: {valor}";

    /// <summary>"5511999998888" → "(11) 99999-8888". Mesma regra do app (formatarWhatsApp).</summary>
    public static string FormatarWhatsApp(string numero)
    {
        var local = numero.StartsWith("55") ? numero[2..] : numero;
        var resto = local[2..];
        return $"({local[..2]}) {resto[..^4]}-{resto[^4..]}";
    }

    /// <summary>Placa antiga com hífen ("ABC-1234"), Mercosul sem ("ABC1D23"), como na placa do carro.</summary>
    public static string FormatarPlaca(string placa) =>
        placa.Length == 7 && char.IsDigit(placa[4]) ? $"{placa[..3]}-{placa[3..]}" : placa;
}
