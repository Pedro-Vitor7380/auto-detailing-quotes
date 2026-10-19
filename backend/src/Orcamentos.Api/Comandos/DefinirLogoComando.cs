using Microsoft.EntityFrameworkCore;
using Orcamentos.Domain.Comum;
using Orcamentos.Infrastructure.Dados;

namespace Orcamentos.Api.Comandos;

/// <summary>
/// Comando de terminal que grava a logo da oficina (aparece no cabeçalho do PDF do orçamento).
/// Uso: dotnet run --project src/Orcamentos.Api -- definir-logo docs/marca/logo-pdf.png
///      dotnet run --project src/Orcamentos.Api -- definir-logo --remover
/// </summary>
public static class DefinirLogoComando
{
    public const string Nome = "definir-logo";

    public static async Task<int> ExecutarAsync(IServiceProvider services, string? caminho)
    {
        var remover = caminho == "--remover";
        var arquivo = remover ? null : Terminal.LocalizarArquivo(caminho);
        if (!remover && arquivo is null)
        {
            Console.Error.WriteLine($"Arquivo não encontrado: \"{caminho}\". Uso: {Nome} docs/marca/logo-pdf.png (ou --remover)");
            return 1;
        }

        using var escopo = services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            var oficina = await db.Oficinas.OrderBy(o => o.CriadoEm).FirstOrDefaultAsync();
            if (oficina is null)
            {
                Console.Error.WriteLine("Nenhuma oficina cadastrada. Use o comando criar-usuario primeiro.");
                return 1;
            }

            // O Domínio confere o formato (PNG/JPEG pelos primeiros bytes) e o tamanho (até 1 MB).
            oficina.DefinirLogo(arquivo is null ? null : await File.ReadAllBytesAsync(arquivo));
            await db.SaveChangesAsync();

            Console.WriteLine(remover
                ? $"Logo removida da oficina {oficina.Nome}."
                : $"Logo da oficina {oficina.Nome} gravada ({oficina.Logo!.Length / 1024} KB, de {arquivo}).");
            return 0;
        }
        catch (RegraDeNegocioException ex)
        {
            Console.Error.WriteLine($"Erro: {ex.Message}");
            return 1;
        }
    }
}
