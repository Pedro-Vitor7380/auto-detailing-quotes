using Microsoft.EntityFrameworkCore;
using Orcamentos.Domain.Comum;
using Orcamentos.Infrastructure.Dados;

namespace Orcamentos.Api.Comandos;

/// <summary>
/// Comando de terminal que altera os dados da oficina (os que aparecem no PDF do orçamento).
/// Uso: dotnet run --project src/Orcamentos.Api -- alterar-oficina
/// Cada pergunta mostra o valor atual: Enter mantém, "-" apaga.
/// </summary>
public static class AlterarOficinaComando
{
    public const string Nome = "alterar-oficina";

    private const string Apagar = "-";

    public static async Task<int> ExecutarAsync(IServiceProvider services)
    {
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

            Console.WriteLine("Enter mantém o valor atual; \"-\" apaga o campo.");
            oficina.AlterarNome(Terminal.PerguntarMantendo("Nome", oficina.Nome)!);
            oficina.AtualizarContato(
                cnpj: PerguntarOpcional("CNPJ", oficina.Cnpj),
                telefone: PerguntarOpcional("Telefone", oficina.Telefone),
                endereco: PerguntarOpcional("Endereço", oficina.Endereco));

            await db.SaveChangesAsync();

            Console.WriteLine($"Oficina atualizada: {oficina.Nome}");
            return 0;
        }
        catch (RegraDeNegocioException ex)
        {
            Console.Error.WriteLine($"Erro: {ex.Message}");
            return 1;
        }
    }

    private static string? PerguntarOpcional(string rotulo, string? valorAtual)
    {
        var resposta = Terminal.PerguntarMantendo(rotulo, valorAtual);
        return resposta?.Trim() == Apagar ? null : resposta;
    }
}
