using Orcamentos.Infrastructure.Seguranca;

namespace Orcamentos.Api.Comandos;

/// <summary>
/// Perguntas no terminal usadas pelos comandos (criar-usuario, alterar-senha, alterar-oficina).
/// Ficam aqui para que todos os comandos se comportem do mesmo jeito.
/// </summary>
internal static class Terminal
{
    public static string Perguntar(string rotulo)
    {
        Console.Write($"{rotulo}: ");
        return Console.ReadLine() ?? "";
    }

    /// <summary>
    /// Mostra o valor atual entre colchetes. Enter (resposta vazia) mantém o valor atual.
    /// </summary>
    public static string? PerguntarMantendo(string rotulo, string? valorAtual)
    {
        Console.Write($"{rotulo} [{valorAtual ?? "vazio"}]: ");
        var resposta = Console.ReadLine();
        return string.IsNullOrWhiteSpace(resposta) ? valorAtual : resposta;
    }

    public static string PerguntarSenha(string rotulo = "Senha")
    {
        while (true)
        {
            var senha = LerSemMostrar($"{rotulo} (mínimo {HasherDeSenha.TamanhoMinimo} caracteres)");
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

    /// <summary>
    /// O "dotnet run --project" executa o programa DENTRO da pasta do projeto (src/Orcamentos.Api),
    /// não na pasta onde o comando foi digitado. Por isso, um caminho relativo é procurado
    /// na pasta atual e depois em cada pasta acima, até achar (ex.: a raiz do repositório).
    /// </summary>
    public static string? LocalizarArquivo(string? caminho)
    {
        if (string.IsNullOrWhiteSpace(caminho))
            return null;

        if (Path.IsPathRooted(caminho))
            return File.Exists(caminho) ? caminho : null;

        for (var pasta = new DirectoryInfo(Environment.CurrentDirectory); pasta is not null; pasta = pasta.Parent)
        {
            var candidato = Path.Combine(pasta.FullName, caminho);
            if (File.Exists(candidato))
                return Path.GetFullPath(candidato);
        }

        return null;
    }
}
