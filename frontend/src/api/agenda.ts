import { requisicao } from './cliente'

// Espelha AgendaResponse (Features/Agenda/AgendaEndpoints.cs).
export interface CarroAgendado {
  orcamentoId: string
  numero: string
  clienteNome: string
  placa: string
  modelo: string
}

export interface DiaDaAgenda {
  data: string // "2026-10-27"
  aberto: boolean // false: domingo ou dia que já passou
  ocupados: number
  carros: CarroAgendado[]
}

export interface Agenda {
  carrosPorDia: number
  dias: DiaDaAgenda[]
}

export const apiAgenda = {
  obter: (de: string, ate: string) => requisicao<Agenda>(`/api/agenda?de=${de}&ate=${ate}`),
}

// Fica "dentro" de ['orcamentos']: agendar, reagendar ou cancelar invalida e recolore o calendário.
export const chavesAgenda = {
  periodo: (de: string, ate: string) => ['orcamentos', 'agenda', de, ate] as const,
}
