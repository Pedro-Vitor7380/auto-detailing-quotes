import { Navigate, Outlet, useLocation } from 'react-router'
import { useAuth } from './contexto'

/** Só mostra as rotas filhas para quem está logado; os demais vão para /login. */
export function RotaProtegida() {
  const { usuario } = useAuth()
  const local = useLocation()

  if (!usuario) {
    // Guarda de onde a pessoa veio, para voltar para lá depois do login.
    return <Navigate to="/login" replace state={{ voltarPara: local.pathname }} />
  }

  return <Outlet />
}
