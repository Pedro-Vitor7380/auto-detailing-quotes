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
    public const int LogoTamanhoMaximo = 1024 * 1024; // 1 MB: sobra para uma logo; o PDF não fica pesado

    /// <summary>Imagem da logo (PNG ou JPEG), usada no cabeçalho do PDF. Fica no banco, ligada à oficina.</summary>
    public byte[]? Logo { get; private set; }

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

    /// <summary>Null remove a logo. Confere o formato pelos primeiros bytes do arquivo, não pela extensão.</summary>
    public void DefinirLogo(byte[]? imagem)
    {
        if (imagem is { Length: > LogoTamanhoMaximo })
            throw new RegraDeNegocioException($"A logo pode ter no máximo {LogoTamanhoMaximo / 1024} KB.");
        if (imagem is not null && !EhPng(imagem) && !EhJpeg(imagem))
            throw new RegraDeNegocioException("A logo precisa ser uma imagem PNG ou JPEG.");

        Logo = imagem;
    }

    // Todo PNG começa com os bytes 89 50 4E 47 ("‰PNG"); todo JPEG, com FF D8 FF.
    // É a "assinatura" do formato: renomear um arquivo para .png não muda esses bytes.
    private static bool EhPng(byte[] b) => b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47;
    private static bool EhJpeg(byte[] b) => b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF;

    // Texto vazio ou só com espaços vira null, para não guardar "lixo" no banco.
    private static string? Limpar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
