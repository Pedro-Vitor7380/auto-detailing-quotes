import { createContext, useContext } from 'react'
import type { Usuario } from '../api/auth'

export interface ValorDoAuth {
  usuario: Usuario | null
  entrar: (email: string, senha: string) => Promise<void>
  sair: () => void
}

export const AuthContexto = createContext<ValorDoAuth | null>(null)

/** Acesso ao usuário logado e às funções de entrar/sair, em qualquer componente. */
export function useAuth(): ValorDoAuth {
  const valor = useContext(AuthContexto)
  if (!valor) throw new Error('useAuth precisa estar dentro de <AuthProvider>.')
  return valor
}
