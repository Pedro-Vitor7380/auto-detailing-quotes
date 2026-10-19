using Orcamentos.Domain.Comum;

namespace Orcamentos.Domain.Entidades;

/// <summary>
/// Uma linha do orçamento. Só existe dentro de um Orcamento (é criado e alterado por ele).
/// Guarda uma CÓPIA dos dados do serviço no momento em que entrou: mudar o catálogo depois
/// não altera orçamentos já feitos.
/// </summary>
public class ItemOrcamento : Entidade
{
    public const int DescricaoTamanhoMaximo = 150;
    public const int ObservacaoTamanhoMaximo = 300;

    /// <summary>Null em item avulso (fora do catálogo).</summary>
    public Guid? ServicoId { get; private set; }

    public string Descricao { get; private set; } = null!;
    public int Quantidade { get; private set; }

    /// <summary>Preço mínimo do catálogo quando o item entrou. Null em item avulso.</summary>
    public decimal? PrecoCatalogo { get; private set; }

    /// <summary>Valor cobrado por unidade, já ajustado pelo estado do carro.</summary>
    public decimal ValorUnitario { get; private set; }

    public int TempoEstimadoMin { get; private set; }

    /// <summary>Por que o valor foi ajustado (ex.: "interior com muitos pelos de animal").</summary>
    public string? ObservacaoEstado { get; private set; }

    public decimal Total => Quantidade * ValorUnitario;

    /// <summary>Quanto o estado do carro mudou o preço, por unidade (positivo = mais caro). Null em item avulso.</summary>
    public decimal? Ajuste => PrecoCatalogo is null ? null : ValorUnitario - PrecoCatalogo;

    internal ItemOrcamento(Guid? servicoId, decimal? precoCatalogo, string descricao, int quantidade,
        decimal valorUnitario, int tempoEstimadoMin, string? observacaoEstado)
    {
        ServicoId = servicoId;
        PrecoCatalogo = precoCatalogo;
        Atualizar(descricao, quantidade, valorUnitario, tempoEstimadoMin, observacaoEstado);
    }

    // Construtor vazio exigido pelo Entity Framework para recriar o objeto a partir do banco.
    private ItemOrcamento() { }

    internal void Atualizar(string descricao, int quantidade, decimal valorUnitario, int tempoEstimadoMin, string? observacaoEstado)
    {
        if (string.IsNullOrWhiteSpace(descricao))
            throw new RegraDeNegocioException("A descrição do item é obrigatória.");

        descricao = descricao.Trim();
        if (descricao.Length > DescricaoTamanhoMaximo)
            throw new RegraDeNegocioException($"A descrição do item pode ter no máximo {DescricaoTamanhoMaximo} caracteres.");

        if (quantidade < 1)
            throw new RegraDeNegocioException("A quantidade precisa ser pelo menos 1.");

        if (valorUnitario < 0)
            throw new RegraDeNegocioException("O valor do item não pode ser negativo.");

        if (tempoEstimadoMin < 0)
            throw new RegraDeNegocioException("O tempo do item não pode ser negativo.");

        observacaoEstado = string.IsNullOrWhiteSpace(observacaoEstado) ? null : observacaoEstado.Trim();
        if (observacaoEstado?.Length > ObservacaoTamanhoMaximo)
            throw new RegraDeNegocioException($"A observação pode ter no máximo {ObservacaoTamanhoMaximo} caracteres.");

        Descricao = descricao;
        Quantidade = quantidade;
        ValorUnitario = Dinheiro.Arredondar(valorUnitario);
        TempoEstimadoMin = tempoEstimadoMin;
        ObservacaoEstado = observacaoEstado;
    }
}
