using System.Text.RegularExpressions;
using Orcamentos.Domain.Comum;

namespace Orcamentos.Domain.Entidades;

/// <summary>
/// Carro de um cliente. Só é criado e alterado pelo próprio Cliente (ver Cliente.AdicionarVeiculo).
/// </summary>
public partial class Veiculo : EntidadeDaOficina
{
    public const int ModeloTamanhoMaximo = 100;
    public const int CorTamanhoMaximo = 40;
    public const int PrimeiroAnoAceito = 1900; // o teto (ano atual + 1) é conferido no app, que sabe a data de hoje

    public Guid ClienteId { get; private set; }

    /// <summary>Sempre maiúscula e sem hífen: "ABC1234" (antiga) ou "ABC1D23" (Mercosul).</summary>
    public string Placa { get; private set; } = null!;

    public string Modelo { get; private set; } = null!;

    /// <summary>Opcional. Ajuda a oficina a achar a tinta certa (pedido da equipe).</summary>
    public string? Cor { get; private set; }

    /// <summary>Opcional. Junto com a cor, ajuda a identificar a tinta de fábrica.</summary>
    public int? AnoFabricacao { get; private set; }

    // internal: só o Cliente (mesmo projeto) cria veículos. Quem usa o Domínio passa sempre pelo Cliente.
    internal Veiculo(Guid oficinaId, Guid clienteId, string placa, string modelo, string? cor, int? anoFabricacao)
        : base(oficinaId)
    {
        ClienteId = clienteId;
        Atualizar(placa, modelo, cor, anoFabricacao);
    }

    // Construtor vazio exigido pelo Entity Framework para recriar o objeto a partir do banco.
    private Veiculo() { }

    internal void Atualizar(string placa, string modelo, string? cor, int? anoFabricacao)
    {
        var placaNormalizada = NormalizarPlaca(placa);

        if (string.IsNullOrWhiteSpace(modelo))
            throw new RegraDeNegocioException("O modelo do veículo é obrigatório.");

        modelo = modelo.Trim();
        if (modelo.Length > ModeloTamanhoMaximo)
            throw new RegraDeNegocioException($"O modelo pode ter no máximo {ModeloTamanhoMaximo} caracteres.");

        cor = string.IsNullOrWhiteSpace(cor) ? null : cor.Trim();
        if (cor?.Length > CorTamanhoMaximo)
            throw new RegraDeNegocioException($"A cor pode ter no máximo {CorTamanhoMaximo} caracteres.");

        if (anoFabricacao is < PrimeiroAnoAceito or > 2100)
            throw new RegraDeNegocioException("Ano de fabricação inválido.");

        Placa = placaNormalizada;
        Modelo = modelo;
        Cor = cor;
        AnoFabricacao = anoFabricacao;
    }

    /// <summary>
    /// Valida a placa e devolve no formato guardado. Aceita "abc-1234", "ABC 1D23" etc.
    /// Público para a busca usar a mesma regra (o usuário digita "abc-1d23" e acha "ABC1D23").
    /// </summary>
    public static string NormalizarPlaca(string placa)
    {
        var limpa = Regex.Replace(placa ?? "", "[^A-Za-z0-9]", "").ToUpperInvariant();

        // 3 letras + 1 número + (número ou letra) + 2 números. A 5ª posição define antiga ou Mercosul.
        if (!FormatoDePlaca().IsMatch(limpa))
            throw new RegraDeNegocioException("Placa inválida. Use o formato ABC1234 ou ABC1D23.");

        return limpa;
    }

    // [GeneratedRegex]: o .NET gera o código da expressão regular na compilação (mais rápido que montar em tempo de execução).
    [GeneratedRegex("^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$")]
    private static partial Regex FormatoDePlaca();
}
