import { requisicao } from './cliente'

// Tipos espelham os records da API (Features/Catalogo/*Endpoints.cs).
export interface Categoria {
  id: string
  nome: string
  quantidadeDeServicos: number
}

export interface Servico {
  id: string
  categoriaId: string
  categoriaNome: string
  nome: string
  descricao: string | null
  precoMinimo: number
  precoMaximo: number
  ativo: boolean
}

export interface DadosDoServico {
  categoriaId: string
  nome: string
  descricao: string | null
  precoMinimo: number
  precoMaximo: number
}

export const apiCatalogo = {
  listarCategorias: () => requisicao<Categoria[]>('/api/categorias'),
  criarCategoria: (nome: string) => requisicao<Categoria>('/api/categorias', { metodo: 'POST', corpo: { nome } }),
  renomearCategoria: (id: string, nome: string) =>
    requisicao<Categoria>(`/api/categorias/${id}`, { metodo: 'PUT', corpo: { nome } }),
  excluirCategoria: (id: string) => requisicao<void>(`/api/categorias/${id}`, { metodo: 'DELETE' }),

  // O catálogo é pequeno (dezenas de itens): buscamos tudo de uma vez e filtramos no próprio celular.
  listarServicos: () => requisicao<Servico[]>('/api/servicos?incluirInativos=true'),
  obterServico: (id: string) => requisicao<Servico>(`/api/servicos/${id}`),
  criarServico: (dados: DadosDoServico) => requisicao<Servico>('/api/servicos', { metodo: 'POST', corpo: dados }),
  atualizarServico: (id: string, dados: DadosDoServico) =>
    requisicao<Servico>(`/api/servicos/${id}`, { metodo: 'PUT', corpo: dados }),
  definirAtivo: (id: string, ativo: boolean) =>
    requisicao<void>(`/api/servicos/${id}/${ativo ? 'ativar' : 'desativar'}`, { metodo: 'POST' }),
}

/**
 * "Chaves" do cache do TanStack Query. Cada consulta fica guardada sob uma chave;
 * depois de salvar algo, "invalidamos" a chave e a lista é buscada de novo.
 */
export const chavesCatalogo = {
  categorias: ['categorias'] as const,
  servicos: ['servicos'] as const,
  servico: (id: string) => ['servicos', id] as const,
}
