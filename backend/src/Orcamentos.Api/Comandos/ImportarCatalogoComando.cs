using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Orcamentos.Domain.Comum;
using Orcamentos.Domain.Entidades;
using Orcamentos.Infrastructure.Dados;

namespace Orcamentos.Api.Comandos;

/// <summary>
/// Comando de terminal que carrega categorias e serviços de um arquivo JSON.
/// Uso: dotnet run --project src/Orcamentos.Api -- importar-catalogo docs/catalogo-inicial.json
/// Pode ser rodado de novo sem duplicar: o que já existe (pelo nome) é mantido como está.
/// </summary>
public static class ImportarCatalogoComando
{
    public const string Nome = "importar-catalogo";

    // O formato do arquivo. Records simples: o System.Text.Json preenche pelo nome das propriedades.
    private record Arquivo(List<CategoriaDoArquivo> Categorias);
    private record CategoriaDoArquivo(string Nome, List<ServicoDoArquivo> Servicos);
    private record ServicoDoArquivo(string Nome, string? Descricao, decimal PrecoMinimo, decimal PrecoMaximo);

    private static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web); // aceita "precoMinimo"

    public static async Task<int> ExecutarAsync(IServiceProvider services, string? caminho)
    {
        var arquivoEncontrado = Terminal.LocalizarArquivo(caminho);
        if (arquivoEncontrado is null)
        {
            Console.Error.WriteLine($"Arquivo não encontrado: \"{caminho}\". Uso: {Nome} docs/catalogo-inicial.json");
            return 1;
        }

        using var escopo = services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            var arquivo = JsonSerializer.Deserialize<Arquivo>(await File.ReadAllTextAsync(arquivoEncontrado), OpcoesJson)
                ?? throw new RegraDeNegocioException("O arquivo está vazio.");

            var oficina = await db.Oficinas.OrderBy(o => o.CriadoEm).FirstOrDefaultAsync();
            if (oficina is null)
            {
                Console.Error.WriteLine("Nenhuma oficina cadastrada. Use o comando criar-usuario primeiro.");
                return 1;
            }

            // Fora de uma requisição não há token, então o filtro global esconderia TUDO.
            // IgnoreQueryFilters desliga o filtro, e por isso filtramos pela oficina à mão, logo em seguida.
            var categorias = await db.Categorias.IgnoreQueryFilters().Where(c => c.OficinaId == oficina.Id).ToListAsync();
            var nomesDeServicos = (await db.Servicos.IgnoreQueryFilters()
                    .Where(s => s.OficinaId == oficina.Id).Select(s => s.Nome).ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            int categoriasNovas = 0, servicosNovos = 0, servicosExistentes = 0;
            foreach (var item in arquivo.Categorias)
            {
                var categoria = categorias.FirstOrDefault(c => c.Nome.Equals(item.Nome.Trim(), StringComparison.OrdinalIgnoreCase));
                if (categoria is null)
                {
                    categoria = new Categoria(oficina.Id, item.Nome);
                    categorias.Add(categoria);
                    db.Categorias.Add(categoria);
                    categoriasNovas++;
                }

                foreach (var s in item.Servicos)
                {
                    // Add devolve false se o nome já estava no conjunto: serviço existente não é alterado,
                    // porque alguém pode ter ajustado o preço pelo app depois da primeira carga.
                    if (!nomesDeServicos.Add(s.Nome.Trim()))
                    {
                        servicosExistentes++;
                        continue;
                    }

                    // O construtor do Domínio valida cada serviço: um valor errado no arquivo para tudo aqui.
                    db.Servicos.Add(new Servico(oficina.Id, categoria.Id, s.Nome, s.Descricao,
                        s.PrecoMinimo, s.PrecoMaximo));
                    servicosNovos++;
                }
            }

            Console.WriteLine($"Arquivo: {arquivoEncontrado}");
            Console.WriteLine($"Oficina: {oficina.Nome}");
            Console.WriteLine($"  Categorias novas: {categoriasNovas}");
            Console.WriteLine($"  Serviços novos:   {servicosNovos}");
            Console.WriteLine($"  Já existiam (mantidos como estão): {servicosExistentes}");

            if (categoriasNovas + servicosNovos == 0)
            {
                Console.WriteLine("Nada a importar.");
                return 0;
            }

            if (!Terminal.Perguntar("Confirma a importação? (s/N)").Trim().Equals("s", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Cancelado. Nada foi gravado.");
                return 1;
            }

            // Um único SaveChanges = uma transação: ou grava tudo, ou nada.
            await db.SaveChangesAsync();
            Console.WriteLine("Catálogo importado.");
            return 0;
        }
        catch (Exception ex) when (ex is RegraDeNegocioException or JsonException)
        {
            Console.Error.WriteLine($"Erro no arquivo: {ex.Message} Nada foi gravado.");
            return 1;
        }
    }
}
