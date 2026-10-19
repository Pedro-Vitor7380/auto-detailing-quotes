import { requisicao } from './cliente'

// Tipos espelham os records da API (Features/Orcamentos/OrcamentosEndpoints.cs).
export type StatusDoOrcamento = 'Rascunho' | 'Enviado' | 'Aprovado' | 'Recusado' | 'Expirado'
export type TipoDeDesconto = 'Nenhum' | 'Valor' | 'Percentual'

export interface ItemDoOrcamento {
  id: string
  servicoId: string | null
  descricao: string
  quantidade: number
  precoCatalogo: number | null
  valorUnitario: number
  ajuste: number | null
  total: number
  tempoEstimadoMin: number
  observacaoEstado: string | null
}

export interface Orcamento {
  id: string
  numero: string
  status: StatusDoOrcamento
  podeSerEditado: boolean
  clienteId: string
  clienteNome: string
  clienteWhatsApp: string
  veiculoId: string
  placa: string
  modelo: string
  itens: ItemDoOrcamento[]
  subtotal: number
  descontoTipo: TipoDeDesconto
  descontoValor: number
  valorDoDesconto: number
  total: number
  tempoEstimadoTotalMin: number
  validade: string // "2026-10-26"
  observacoes: string | null
  criadoEm: string
  enviadoEm: string | null
  aprovadoEm: string | null
  recusadoEm: string | null
}

export interface OrcamentoResumo {
  id: string
  numero: string
  status: StatusDoOrcamento
  clienteNome: string
  placa: string
  modelo: string
  total: number
  validade: string
  criadoEm: string
}

/** Item como o app envia: com id = já existe; sem id = novo (do catálogo, se tiver servicoId). */
export interface ItemParaSalvar {
  id?: string
  servicoId?: string
  descricao?: string
  quantidade: number
  valorUnitario: number
  tempoEstimadoMin?: number
  observacaoEstado?: string
}

export interface OrcamentoParaSalvar {
  clienteId: string
  veiculoId: string
  itens: ItemParaSalvar[]
  descontoTipo: TipoDeDesconto
  descontoValor: number
  validade?: string
  observacoes?: string
}

export const apiOrcamentos = {
  listar: (filtros: { status?: StatusDoOrcamento; busca?: string } = {}) => {
    const parametros = new URLSearchParams()
    if (filtros.status) parametros.set('status', filtros.status)
    if (filtros.busca) parametros.set('busca', filtros.busca)
    return requisicao<OrcamentoResumo[]>(`/api/orcamentos?${parametros}`)
  },
  obter: (id: string) => requisicao<Orcamento>(`/api/orcamentos/${id}`),
  criar: (dados: OrcamentoParaSalvar) => requisicao<Orcamento>('/api/orcamentos', { metodo: 'POST', corpo: dados }),
  salvar: (id: string, dados: OrcamentoParaSalvar) =>
    requisicao<Orcamento>(`/api/orcamentos/${id}`, { metodo: 'PUT', corpo: dados }),
  excluir: (id: string) => requisicao<void>(`/api/orcamentos/${id}`, { metodo: 'DELETE' }),
  enviar: (id: string) => requisicao<Orcamento>(`/api/orcamentos/${id}/enviar`, { metodo: 'POST' }),
  aprovar: (id: string) => requisicao<Orcamento>(`/api/orcamentos/${id}/aprovar`, { metodo: 'POST' }),
  recusar: (id: string) => requisicao<Orcamento>(`/api/orcamentos/${id}/recusar`, { metodo: 'POST' }),
  duplicar: (id: string) => requisicao<Orcamento>(`/api/orcamentos/${id}/duplicar`, { metodo: 'POST' }),
  pdf: (id: string) => requisicao<Blob>(`/api/orcamentos/${id}/pdf`, { formato: 'arquivo' }),
}

export const chavesOrcamentos = {
  todos: ['orcamentos'] as const,
  lista: (filtros: object) => ['orcamentos', 'lista', filtros] as const,
  orcamento: (id: string) => ['orcamentos', id] as const,
  // Fica "dentro" de ['orcamentos']: salvar qualquer orçamento invalida e refaz o PDF.
  pdf: (id: string) => ['orcamentos', id, 'pdf'] as const,
}
