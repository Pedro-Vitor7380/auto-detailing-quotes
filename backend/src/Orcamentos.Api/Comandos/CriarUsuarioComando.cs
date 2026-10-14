using Microsoft.EntityFrameworkCore;
using Orcamentos.Domain.Comum;
using Orcamentos.Domain.Entidades;
using Orcamentos.Infrastructure.Dados;
using Orcamentos.Infrastructure.Seguranca;

namespace Orcamentos.Api.Comandos;

/// <summary>
/// Comando de terminal que cria a oficina (se ainda não existir) e um usuário.
/// Uso: dotnet run --project src/Orcamentos.Api -- criar-usuario
/// Não existe cadastro público no app: só quem tem acesso ao banco cria usuários.
/// </summary>
public static class CriarUsuarioComando
{
    public const string Nome = "criar-usuario";

    public static async Task<int> ExecutarAsync(IServiceProvider services)
    {
        // O DbContext é "Scoped": fora de uma requisição HTTP, criamos o escopo manualmente.
        using var escopo = services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = escopo.ServiceProvider.GetRequiredService<HasherDeSenha>();

        try
        {
            var oficina = await db.Oficinas.OrderBy(o => o.CriadoEm).FirstOrDefaultAsync();
            if (oficina is null)
            {
                Console.WriteLine("Nenhuma oficina cadastrada. Vamos criar a primeira.");
                oficina = new Oficina(Perguntar("Nome da oficina"));
                db.Oficinas.Add(oficina);
            }
            else
            {
                Console.WriteLine($"Usando a oficina existente: {oficina.Nome}");
            }

            var nome = Perguntar("Seu nome");
            // Valida já aqui, para não pedir a senha à toa se o e-mail estiver errado.
            var email = Usuario.NormalizarEmail(Perguntar("E-mail (será o login)"));

            if (await db.Usuarios.AnyAsync(u => u.Email == email))
            {
                Console.Error.WriteLine("Já existe um usuário com esse e-mail.");
                return 1;
            }

            var senha = PerguntarSenha();
            db.Usuarios.Add(new Usuario(oficina.Id, nome, email, hasher.GerarHash(senha)));

            // Grava a oficina (se nova) e o usuário numa única transação: ou salva tudo, ou nada.
            await db.SaveChangesAsync();

            Console.WriteLine($"Usuário {email} criado com sucesso na oficina {oficina.Nome}.");
            return 0;
        }
        catch (RegraDeNegocioException ex)
        {
            Console.Error.WriteLine($"Erro: {ex.Message}");
            return 1;
        }
    }

    private static string Perguntar(string rotulo)
    {
        Console.Write($"{rotulo}: ");
        return Console.ReadLine() ?? "";
    }

    private static string PerguntarSenha()
    {
        while (true)
        {
            var senha = LerSemMostrar($"Senha (mínimo {HasherDeSenha.TamanhoMinimo} caracteres)");
            if (senha.Length < HasherDeSenha.TamanhoMinimo)
            {
                Console.WriteLine("Senha muito curta.");
                continue;
            }

            if (LerSemMostrar("Confirme a senha") != senha)
            {
                Console.WriteLine("As senhas não conferem.");
                continue;
            }

            return senha;
        }
    }

    // Lê a senha sem exibi-la na tela (mostra * no lugar de cada caractere).
    private static string LerSemMostrar(string rotulo)
    {
        Console.Write($"{rotulo}: ");

        // Se a entrada vier de um arquivo/pipe (ex.: script de teste), não há teclado para esconder.
        if (Console.IsInputRedirected)
            return Console.ReadLine() ?? "";

        var senha = new System.Text.StringBuilder();
        while (true)
        {
            var tecla = Console.ReadKey(intercept: true);
            if (tecla.Key == ConsoleKey.Enter) break;

            if (tecla.Key == ConsoleKey.Backspace)
            {
                if (senha.Length == 0) continue;
                senha.Length--;
                Console.Write("\b \b");
            }
            else if (!char.IsControl(tecla.KeyChar))
            {
                senha.Append(tecla.KeyChar);
                Console.Write('*');
            }
        }

        Console.WriteLine();
        return senha.ToString();
    }
}
