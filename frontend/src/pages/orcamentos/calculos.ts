import type { TipoDeDesconto } from '../../api/orcamentos'

// As MESMAS fórmulas do Domínio (Orcamento.cs / Dinheiro.cs), só para mostrar o total enquanto se monta o orçamento.
// Quem manda é a API: ao salvar, a tela passa a mostrar os valores que ela devolveu.

/** Arredondamento "da escola" (,005 sobe), igual ao MidpointRounding.AwayFromZero do C#. */
export function arredondar(valor: number): number {
  // Number.EPSILON corrige erros de ponto flutuante do JavaScript (1.005 * 100 = 100.49999...).
  return Math.round((valor + Number.EPSILON) * 100) / 100
}

export interface ItemParaCalculo {
  quantidade: number
  valorUnitario: number
  tempoEstimadoMin: number
}

export function calcularTotais(itens: ItemParaCalculo[], descontoTipo: TipoDeDesconto, descontoValor: number) {
  const subtotal = arredondar(itens.reduce((soma, i) => soma + i.quantidade * i.valorUnitario, 0))
  const desconto =
    descontoTipo === 'Valor'
      ? Math.min(descontoValor, subtotal)
      : descontoTipo === 'Percentual'
        ? arredondar((subtotal * descontoValor) / 100)
        : 0
  return {
    subtotal,
    desconto,
    total: arredondar(subtotal - desconto),
    tempoTotalMin: itens.reduce((soma, i) => soma + i.tempoEstimadoMin * i.quantidade, 0),
  }
}
