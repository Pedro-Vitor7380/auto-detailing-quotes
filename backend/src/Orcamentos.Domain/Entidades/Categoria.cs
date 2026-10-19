using Orcamentos.Domain.Comum;

namespace Orcamentos.Domain.Entidades;

/// <summary>
/// Agrupa os serviços do catálogo (ex.: "Funilaria e pintura"), para a lista caber no celular.
/// </summary>
public class Categoria : EntidadeDaOficina
{
    public const int NomeTamanhoMaximo = 80;

    public string Nome { get; private set; } = null!;

    public Categoria(Guid oficinaId, string nome) : base(oficinaId)
    {
        Renomear(nome);
    }

    // Construtor vazio exigido pelo Entity Framework para recriar o objeto a partir do banco.
    private Categoria() { }

    public void Renomear(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new RegraDeNegocioException("O nome da categoria é obrigatório.");

        nome = nome.Trim();
        if (nome.Length > NomeTamanhoMaximo)
            throw new RegraDeNegocioException($"O nome da categoria pode ter no máximo {NomeTamanhoMaximo} caracteres.");

        Nome = nome;
    }
}
