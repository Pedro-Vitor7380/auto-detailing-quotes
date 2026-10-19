import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { apiAuth, type Usuario } from '../api/auth'
import { definirAoSessaoExpirar } from '../api/cliente'
import { queryClient } from '../api/queryClient'
import { lerSessao, removerSessao, salvarSessao } from './armazenamentoSessao'
import { AuthContexto } from './contexto'

/** Guarda quem está logado e disponibiliza para o app inteiro via useAuth(). */
export function AuthProvider({ children }: { children: ReactNode }) {
  // Começa com o usuário salvo: o app abre na hora, mesmo se a API estiver "acordando".
  const [usuario, setUsuario] = useState<Usuario | null>(() => lerSessao()?.usuario ?? null)

  const sair = useCallback(() => {
    removerSessao()
    // Apaga o cache das consultas: quem entrar depois neste aparelho não pode ver dados de outra conta.
    queryClient.clear()
    setUsuario(null)
  }, [])

  const entrar = useCallback(async (email: string, senha: string) => {
    const resposta = await apiAuth.login(email, senha)
    salvarSessao(resposta)
    setUsuario(resposta.usuario)
  }, [])

  // Se qualquer chamada à API receber 401 (token vencido/inválido), encerra a sessão.
  useEffect(() => definirAoSessaoExpirar(sair), [sair])

  // Ao abrir o app, confirma em segundo plano se o token salvo ainda vale.
  // Se a API responder 401, o cliente chama sair(); se estiver sem internet, mantém a sessão.
  useEffect(() => {
    if (!lerSessao()) return
    apiAuth.usuarioLogado().then(setUsuario).catch(() => {})
  }, [])

  const valor = useMemo(() => ({ usuario, entrar, sair }), [usuario, entrar, sair])

  return <AuthContexto.Provider value={valor}>{children}</AuthContexto.Provider>
}
