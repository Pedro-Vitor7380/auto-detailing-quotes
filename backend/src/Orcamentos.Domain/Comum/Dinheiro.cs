namespace Orcamentos.Domain.Comum;

/// <summary>
/// Regras de dinheiro usadas em todo o Domínio. Antes ficava dentro do Servico;
/// com o orçamento (itens, desconto, total), passou a ser usada em vários lugares.
/// </summary>
public static class Dinheiro
{
    // 2 casas: evita guardar R$ 10,005, que nenhum cliente consegue pagar.
    // AwayFromZero é o arredondamento "da escola" (,005 sobe). O padrão do .NET arredondaria para o par.
    public static decimal Arredondar(decimal valor) => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
}
