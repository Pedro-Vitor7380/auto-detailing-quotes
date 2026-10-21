using System.Security.Cryptography;
using Orcamentos.Infrastructure.Notificacoes;

namespace Orcamentos.Api.Comandos;

/// <summary>
/// Gera as chaves das notificações (VAPID) e a chave da tarefa diária, e grava em backend/.env.notificacoes
/// (ignorado pelo Git). Nada secreto aparece na tela: só a chave PÚBLICA, que não é segredo.
/// Uso: dotnet run --project src/Orcamentos.Api -- gerar-chaves-notificacao
/// </summary>
public static class GerarChavesDeNotificacaoComando
{
    public const string Nome = "gerar-chaves-notificacao";
    private const string Arquivo = ".env.notificacoes";

    public static int Executar()
    {
        // Grava na pasta "backend" (a que tem o .env.producao), subindo a partir da pasta atual.
        var pasta = new DirectoryInfo(Environment.CurrentDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "Orcamentos.slnx")))
            pasta = pasta.Parent;
        if (pasta is null)
        {
            Console.Error.WriteLine("Não achei a pasta backend (Orcamentos.slnx).");
            return 1;
        }

        var destino = Path.Combine(pasta.FullName, Arquivo);
        if (File.Exists(destino))
        {
            // Trocar as chaves invalida as inscrições de todos os celulares: só de propósito, apagando o arquivo antes.
            Console.Error.WriteLine($"{Arquivo} já existe. Trocar as chaves desativa as notificações em todos os celulares.");
            return 1;
        }

        var (publica, privada) = EnviadorDeNotificacoes.GerarChaves();
        var chaveDaTarefa = EnviadorDeNotificacoes.Base64Url(RandomNumberGenerator.GetBytes(32));

        // Os nomes com "__" são os das variáveis de ambiente do Render (o "__" vira ":" na configuração do .NET).
        File.WriteAllLines(destino,
        [
            $"Notificacoes__ChavePublica={publica}",
            $"Notificacoes__ChavePrivada={privada}",
            "Notificacoes__Contato=mailto:contato@cjestetica.com.br",
            $"Tarefas__Chave={chaveDaTarefa}",
        ]);

        Console.WriteLine($"Chaves gravadas em {destino} (fora do Git).");
        Console.WriteLine($"Chave pública (não é segredo): {publica}");
        return 0;
    }
}
