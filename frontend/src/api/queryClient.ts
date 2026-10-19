import { QueryClient } from '@tanstack/react-query'
import { ErroDaApi } from './cliente'

/**
 * Configuração do TanStack Query: ele guarda as respostas da API em cache,
 * mostra o dado guardado na hora e atualiza em segundo plano.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Por 30 s o dado é considerado "fresco": trocar de tela e voltar não chama a API de novo.
      staleTime: 30_000,
      // Tenta de novo (até 2 vezes) só falhas de rede ou do servidor. Erro 4xx não melhora repetindo.
      retry: (tentativas, erro) =>
        tentativas < 2 && !(erro instanceof ErroDaApi && erro.status >= 400 && erro.status < 500),
    },
  },
})
