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
                oficina = new Oficina(Terminal.Perguntar("Nome da oficina"));
                db.Oficinas.Add(oficina);
            }
            else
            {
                Console.WriteLine($"Usando a oficina existente: {oficina.Nome}");
            }

            var nome = Terminal.Perguntar("Seu nome");
            // Valida já aqui, para não pedir a senha à toa se o e-mail estiver errado.
            var email = Usuario.NormalizarEmail(Terminal.Perguntar("E-mail (será o login)"));

            if (await db.Usuarios.AnyAsync(u => u.Email == email))
            {
                Console.Error.WriteLine("Já existe um usuário com esse e-mail.");
                return 1;
            }

            var senha = Terminal.PerguntarSenha();
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
}
