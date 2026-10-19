import { requisicao } from './cliente'

// Espelha PainelResponse (Features/Painel/PainelEndpoints.cs).
export interface Painel {
  ano: number
  mes: number
  emAberto: number
  valorEmAberto: number
  aprovadosNoMes: number
  faturamentoPrevisto: number
}

export const apiPainel = {
  obter: (ano: number, mes: number) => requisicao<Painel>(`/api/painel?ano=${ano}&mes=${mes}`),
}

// Fica "dentro" de ['orcamentos']: criar, aprovar ou excluir um orçamento invalida e atualiza o Início.
export const chavesPainel = {
  mes: (ano: number, mes: number) => ['orcamentos', 'painel', ano, mes] as const,
}
