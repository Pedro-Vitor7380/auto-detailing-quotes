import { requisicao } from './cliente'

// Espelha AgendaResponse (Features/Agenda/AgendaEndpoints.cs).
export interface CarroAgendado {
  orcamentoId: string
  numero: string
  clienteNome: string
  placa: string
  modelo: string
  de: string // dia em que o carro chega
  ate: string // dia da entrega
}

export interface DiaDaAgenda {
  data: string // "2026-10-27"
  aberto: boolean // false: domingo ou dia que já passou
  ocupados: number // carros na oficina nesse dia (todos os períodos que passam por ele)
  carros: CarroAgendado[]
}

export interface Agenda {
  dias: DiaDaAgenda[]
}

/** Um carro com entrega no dia pedido (padrão: amanhã), para o quadro "Terminam amanhã". */
export interface Entrega {
  orcamentoId: string
  numero: string
  clienteNome: string
  clienteWhatsApp: string
  placa: string
  modelo: string
  de: string
  ate: string
  avisadoEm: string | null // quando alguém da equipe avisou o cliente
}

export const apiAgenda = {
  obter: (de: string, ate: string) => requisicao<Agenda>(`/api/agenda?de=${de}&ate=${ate}`),
  entregasDeAmanha: () => requisicao<Entrega[]>('/api/agenda/entregas'),
}

// Fica "dentro" de ['orcamentos']: agendar, reagendar ou cancelar invalida e recolore o calendário.
export const chavesAgenda = {
  periodo: (de: string, ate: string) => ['orcamentos', 'agenda', de, ate] as const,
  entregasDeAmanha: ['orcamentos', 'agenda', 'entregas'] as const,
}
