using Orcamentos.Domain.Comum;

namespace Orcamentos.Domain.Entidades;

/// <summary>
/// Um serviço do catálogo, com a faixa de preço que serve de referência no orçamento.
/// Serviços não são apagados, só desativados: orçamentos antigos continuam apontando para eles.
/// </summary>
public class Servico : EntidadeDaOficina
{
    public const int NomeTamanhoMaximo = 120;
    public const int DescricaoTamanhoMaximo = 500;

    public Guid CategoriaId { get; private set; }
    public string Nome { get; private set; } = null!;
    public string? Descricao { get; private set; }

    /// <summary>Valor com que o serviço entra no orçamento, antes do ajuste pelo estado do carro.</summary>
    public decimal PrecoMinimo { get; private set; }

    /// <summary>Teto de referência. O valor ajustado no orçamento pode passar dele.</summary>
    public decimal PrecoMaximo { get; private set; }

    public bool Ativo { get; private set; } = true;

    public Servico(Guid oficinaId, Guid categoriaId, string nome, string? descricao,
        decimal precoMinimo, decimal precoMaximo)
        : base(oficinaId)
    {
        Atualizar(categoriaId, nome, descricao, precoMinimo, precoMaximo);
    }

    // Construtor vazio exigido pelo Entity Framework para recriar o objeto a partir do banco.
    private Servico() { }

    /// <summary>
    /// Altera todos os dados de uma vez, para que a validação veja o conjunto
    /// (ex.: mínimo e máximo precisam ser conferidos juntos).
    /// </summary>
    public void Atualizar(Guid categoriaId, string nome, string? descricao,
        decimal precoMinimo, decimal precoMaximo)
    {
        if (categoriaId == Guid.Empty)
            throw new RegraDeNegocioException("O serviço precisa de uma categoria.");

        if (string.IsNullOrWhiteSpace(nome))
            throw new RegraDeNegocioException("O nome do serviço é obrigatório.");

        nome = nome.Trim();
        if (nome.Length > NomeTamanhoMaximo)
            throw new RegraDeNegocioException($"O nome do serviço pode ter no máximo {NomeTamanhoMaximo} caracteres.");

        descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        if (descricao?.Length > DescricaoTamanhoMaximo)
            throw new RegraDeNegocioException($"A descrição pode ter no máximo {DescricaoTamanhoMaximo} caracteres.");

        if (precoMinimo < 0)
            throw new RegraDeNegocioException("O preço mínimo não pode ser negativo.");

        if (precoMaximo < precoMinimo)
            throw new RegraDeNegocioException("O preço máximo não pode ser menor que o mínimo.");

        CategoriaId = categoriaId;
        Nome = nome;
        Descricao = descricao;
        PrecoMinimo = Dinheiro.Arredondar(precoMinimo);
        PrecoMaximo = Dinheiro.Arredondar(precoMaximo);
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;
}
