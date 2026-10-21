import { ErroDaApi, requisicao } from './cliente'

export const apiNotificacoes = {
  /** null: o servidor não tem as chaves configuradas (o app esconde o botão de ativar). */
  chavePublica: () =>
    requisicao<{ chave: string }>('/api/notificacoes/chave-publica').then(
      (r) => r.chave,
      (erro) => {
        if (erro instanceof ErroDaApi && erro.status === 503) return null
        throw erro
      },
    ),
  inscrever: (dados: { endpoint: string; p256dh: string; auth: string }) =>
    requisicao<void>('/api/notificacoes/inscricoes', { metodo: 'POST', corpo: dados }),
  desinscrever: (endpoint: string) =>
    requisicao<void>(`/api/notificacoes/inscricoes?endpoint=${encodeURIComponent(endpoint)}`, { metodo: 'DELETE' }),
  testar: () => requisicao<{ enviadas: number }>('/api/notificacoes/teste', { metodo: 'POST' }),
}
