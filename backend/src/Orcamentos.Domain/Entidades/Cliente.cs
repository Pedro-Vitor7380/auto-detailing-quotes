using System.Text.RegularExpressions;
using Orcamentos.Domain.Comum;

namespace Orcamentos.Domain.Entidades;

/// <summary>
/// Cliente da oficina, com os seus veículos. É a "raiz" do conjunto Cliente + Veículos (um agregado):
/// todo veículo é adicionado, alterado ou removido por aqui, e o Cliente garante as regras do conjunto.
/// LGPD: guardamos só o necessário (nome, WhatsApp, placa e modelo).
/// </summary>
public partial class Cliente : EntidadeDaOficina
{
    public const int NomeTamanhoMaximo = 150;

    private readonly List<Veiculo> _veiculos = [];

    public string Nome { get; private set; } = null!;

    /// <summary>Só números, com o código do país: "5511999998888". É o formato do link wa.me.</summary>
    public string WhatsApp { get; private set; } = null!;

    // Quem está de fora só LÊ a lista. Para mudar, usa os métodos abaixo (que validam).
    public IReadOnlyCollection<Veiculo> Veiculos => _veiculos.AsReadOnly();

    public Cliente(Guid oficinaId, string nome, string whatsApp) : base(oficinaId)
    {
        Atualizar(nome, whatsApp);
    }

    // Construtor vazio exigido pelo Entity Framework para recriar o objeto a partir do banco.
    private Cliente() { }

    public void Atualizar(string nome, string whatsApp)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new RegraDeNegocioException("O nome do cliente é obrigatório.");

        nome = nome.Trim();
        if (nome.Length > NomeTamanhoMaximo)
            throw new RegraDeNegocioException($"O nome pode ter no máximo {NomeTamanhoMaximo} caracteres.");

        var numero = NormalizarWhatsApp(whatsApp);

        Nome = nome;
        WhatsApp = numero;
    }

    public Veiculo AdicionarVeiculo(string placa, string modelo, string? cor = null, int? anoFabricacao = null)
    {
        var veiculo = new Veiculo(OficinaId, Id, placa, modelo, cor, anoFabricacao);

        // Regra do conjunto: o mesmo cliente não tem a mesma placa duas vezes.
        // (A regra "placa única na oficina inteira" precisa do banco, então fica no endpoint.)
        if (_veiculos.Any(v => v.Placa == veiculo.Placa))
            throw new RegraDeNegocioException($"O veículo {veiculo.Placa} já está cadastrado para este cliente.");

        _veiculos.Add(veiculo);
        return veiculo;
    }

    public void AtualizarVeiculo(Guid veiculoId, string placa, string modelo, string? cor = null, int? anoFabricacao = null)
    {
        var veiculo = ObterVeiculo(veiculoId);
        var placaNormalizada = Veiculo.NormalizarPlaca(placa);

        if (_veiculos.Any(v => v.Id != veiculoId && v.Placa == placaNormalizada))
            throw new RegraDeNegocioException($"O veículo {placaNormalizada} já está cadastrado para este cliente.");

        veiculo.Atualizar(placa, modelo, cor, anoFabricacao);
    }

    public void RemoverVeiculo(Guid veiculoId) => _veiculos.Remove(ObterVeiculo(veiculoId));

    private Veiculo ObterVeiculo(Guid veiculoId) =>
        _veiculos.FirstOrDefault(v => v.Id == veiculoId)
        ?? throw new RegraDeNegocioException("Veículo não encontrado neste cliente.");

    /// <summary>
    /// Aceita "(11) 99999-8888", "11 3333-4444" ou "+55 11 99999-8888" e devolve "5511999998888".
    /// Público para a busca usar a mesma regra.
    /// </summary>
    public static string NormalizarWhatsApp(string whatsApp)
    {
        var numeros = SomenteNumeros().Replace(whatsApp ?? "", "");
        var temCodigoDoPais = (whatsApp ?? "").TrimStart().StartsWith('+');

        // Sem o "+": DDD (2) + número (8 ou 9 dígitos). Acrescentamos o código do Brasil.
        if (!temCodigoDoPais && numeros.Length is 10 or 11)
            numeros = "55" + numeros;

        // 55 + DDD + 8 dígitos (fixo) ou 9 dígitos começando com 9 (celular).
        var valido = numeros.StartsWith("55") && (numeros.Length == 12 || (numeros.Length == 13 && numeros[4] == '9'));
        if (!valido)
            throw new RegraDeNegocioException("WhatsApp inválido. Informe o DDD e o número, ex.: (11) 99999-8888.");

        return numeros;
    }

    [GeneratedRegex("[^0-9]")]
    private static partial Regex SomenteNumeros();
}
