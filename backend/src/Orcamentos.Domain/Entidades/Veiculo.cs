using System.Text.RegularExpressions;
using Orcamentos.Domain.Comum;

namespace Orcamentos.Domain.Entidades;

/// <summary>
/// Carro de um cliente. Só é criado e alterado pelo próprio Cliente (ver Cliente.AdicionarVeiculo).
/// </summary>
public partial class Veiculo : EntidadeDaOficina
{
    public const int ModeloTamanhoMaximo = 100;

    public Guid ClienteId { get; private set; }

    /// <summary>Sempre maiúscula e sem hífen: "ABC1234" (antiga) ou "ABC1D23" (Mercosul).</summary>
    public string Placa { get; private set; } = null!;

    public string Modelo { get; private set; } = null!;

    // internal: só o Cliente (mesmo projeto) cria veículos. Quem usa o Domínio passa sempre pelo Cliente.
    internal Veiculo(Guid oficinaId, Guid clienteId, string placa, string modelo) : base(oficinaId)
    {
        ClienteId = clienteId;
        Atualizar(placa, modelo);
    }

    // Construtor vazio exigido pelo Entity Framework para recriar o objeto a partir do banco.
    private Veiculo() { }

    internal void Atualizar(string placa, string modelo)
    {
        var placaNormalizada = NormalizarPlaca(placa);

        if (string.IsNullOrWhiteSpace(modelo))
            throw new RegraDeNegocioException("O modelo do veículo é obrigatório.");

        modelo = modelo.Trim();
        if (modelo.Length > ModeloTamanhoMaximo)
            throw new RegraDeNegocioException($"O modelo pode ter no máximo {ModeloTamanhoMaximo} caracteres.");

        Placa = placaNormalizada;
        Modelo = modelo;
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
