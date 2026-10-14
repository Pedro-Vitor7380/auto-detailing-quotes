using Orcamentos.Domain.Comum;

namespace Orcamentos.Domain.Entidades;

/// <summary>
/// A oficina (empresa) dona dos dados. Seus dados aparecem no cabeçalho do PDF do orçamento.
/// </summary>
public class Oficina : Entidade
{
    public string Nome { get; private set; } = null!;
    public string? Cnpj { get; private set; }
    public string? Telefone { get; private set; }
    public string? Endereco { get; private set; }
    public string? LogoUrl { get; private set; }

    public Oficina(string nome)
    {
        AlterarNome(nome);
    }

    // Construtor vazio exigido pelo Entity Framework para recriar o objeto a partir do banco.
    private Oficina() { }

    public void AlterarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new RegraDeNegocioException("O nome da oficina é obrigatório.");

        Nome = nome.Trim();
    }

    public void AtualizarContato(string? cnpj, string? telefone, string? endereco)
    {
        Cnpj = Limpar(cnpj);
        Telefone = Limpar(telefone);
        Endereco = Limpar(endereco);
    }

    public void DefinirLogo(string? logoUrl) => LogoUrl = Limpar(logoUrl);

    // Texto vazio ou só com espaços vira null, para não guardar "lixo" no banco.
    private static string? Limpar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
