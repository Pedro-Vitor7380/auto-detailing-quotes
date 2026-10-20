import { requisicao } from './cliente'

// Tipos espelham os records da API (Features/Clientes/ClientesEndpoints.cs).
export interface Veiculo {
  id: string
  placa: string
  modelo: string
  cor: string | null
  anoFabricacao: number | null
}

export interface Cliente {
  id: string
  nome: string
  whatsApp: string
  veiculos: Veiculo[]
}

export interface DadosDoVeiculo {
  placa: string
  modelo: string
  cor: string | null
  anoFabricacao: number | null
}

export const apiClientes = {
  buscar: (busca: string) => requisicao<Cliente[]>(`/api/clientes?busca=${encodeURIComponent(busca)}`),
  obter: (id: string) => requisicao<Cliente>(`/api/clientes/${id}`),
  criar: (dados: { nome: string; whatsApp: string; veiculos: DadosDoVeiculo[] }) =>
    requisicao<Cliente>('/api/clientes', { metodo: 'POST', corpo: dados }),
  atualizar: (id: string, dados: { nome: string; whatsApp: string }) =>
    requisicao<Cliente>(`/api/clientes/${id}`, { metodo: 'PUT', corpo: dados }),
  excluir: (id: string) => requisicao<void>(`/api/clientes/${id}`, { metodo: 'DELETE' }),

  // As operações de veículo devolvem o cliente inteiro, já atualizado.
  adicionarVeiculo: (clienteId: string, dados: DadosDoVeiculo) =>
    requisicao<Cliente>(`/api/clientes/${clienteId}/veiculos`, { metodo: 'POST', corpo: dados }),
  atualizarVeiculo: (clienteId: string, veiculoId: string, dados: DadosDoVeiculo) =>
    requisicao<Cliente>(`/api/clientes/${clienteId}/veiculos/${veiculoId}`, { metodo: 'PUT', corpo: dados }),
  removerVeiculo: (clienteId: string, veiculoId: string) =>
    requisicao<Cliente>(`/api/clientes/${clienteId}/veiculos/${veiculoId}`, { metodo: 'DELETE' }),
}

export const chavesClientes = {
  todas: ['clientes'] as const,
  busca: (termo: string) => ['clientes', 'busca', termo] as const,
  cliente: (id: string) => ['clientes', id] as const,
}
