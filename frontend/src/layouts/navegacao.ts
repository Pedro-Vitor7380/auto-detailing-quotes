import { FileText, House, Users, Wrench, type LucideIcon } from 'lucide-react'

export interface ItemDeNavegacao {
  rota: string
  rotulo: string
  icone: LucideIcon
}

// Fonte única do menu: a barra inferior (celular) e o menu lateral (computador) usam esta lista.
export const itensDeNavegacao: ItemDeNavegacao[] = [
  { rota: '/', rotulo: 'Início', icone: House },
  { rota: '/orcamentos', rotulo: 'Orçamentos', icone: FileText },
  { rota: '/clientes', rotulo: 'Clientes', icone: Users },
  { rota: '/servicos', rotulo: 'Serviços', icone: Wrench },
]
