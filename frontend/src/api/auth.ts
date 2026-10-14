import { requisicao } from './cliente'

// Tipos espelham os records da API (Features/Auth/AuthEndpoints.cs).
export interface Usuario {
  id: string
  nome: string
  email: string
  oficinaId: string
}

export interface RespostaLogin {
  token: string
  expiraEm: string
  usuario: Usuario
}

export const apiAuth = {
  login: (email: string, senha: string) =>
    requisicao<RespostaLogin>('/api/auth/login', { metodo: 'POST', corpo: { email, senha } }),

  usuarioLogado: () => requisicao<Usuario>('/api/auth/me'),
}
