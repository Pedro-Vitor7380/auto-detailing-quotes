using Microsoft.EntityFrameworkCore;
using Orcamentos.Domain.Comum;
using Orcamentos.Domain.Entidades;
using Orcamentos.Infrastructure.Dados;
using Orcamentos.Infrastructure.Seguranca;

namespace Orcamentos.Api.Comandos;

/// <summary>
/// Comando de terminal que troca a senha de um usuário.
/// Uso: dotnet run --project src/Orcamentos.Api -- alterar-senha
/// Não pede a senha atual: quem roda o comando já tem acesso ao banco (é o "administrador").
/// </summary>
public static class AlterarSenhaComando
{
    public const string Nome = "alterar-senha";

    public static async Task<int> ExecutarAsync(IServiceProvider services)
    {
        using var escopo = services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = escopo.ServiceProvider.GetRequiredService<HasherDeSenha>();

        try
        {
            var email = Usuario.NormalizarEmail(Terminal.Perguntar("E-mail do usuário"));

            // Sem AsNoTracking: o EF "acompanha" o objeto e, no SaveChanges, gera o UPDATE só da coluna alterada.
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            if (usuario is null)
            {
                Console.Error.WriteLine("Nenhum usuário com esse e-mail.");
                return 1;
            }

            var senha = Terminal.PerguntarSenha("Nova senha");
            usuario.DefinirSenhaHash(hasher.GerarHash(senha));
            await db.SaveChangesAsync();

            Console.WriteLine($"Senha de {email} alterada.");
            // O token (JWT) não é guardado no servidor, então quem já está logado continua até o token expirar.
            Console.WriteLine("Atenção: aparelhos já logados continuam com acesso até o login expirar (7 dias).");
            return 0;
        }
        catch (RegraDeNegocioException ex)
        {
            Console.Error.WriteLine($"Erro: {ex.Message}");
            return 1;
        }
    }
}
