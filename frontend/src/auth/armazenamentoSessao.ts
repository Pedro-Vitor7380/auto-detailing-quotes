import type { RespostaLogin, Usuario } from '../api/auth'

/**
 * Guarda a sessão (token + dados do usuário) no localStorage do navegador, para o login
 * sobreviver a um recarregamento e o app abrir na hora, sem esperar a API.
 * try/catch: em modo anônimo ou com armazenamento bloqueado, o localStorage pode lançar erro.
 */
const CHAVE = 'orcamentos.sessao'

export interface Sessao {
  token: string
  expiraEm: string
  usuario: Usuario
}

export function salvarSessao(sessao: RespostaLogin) {
  try {
    localStorage.setItem(CHAVE, JSON.stringify(sessao satisfies Sessao))
  } catch {
    // sem armazenamento: o login vale só até recarregar a página
  }
}

/** Devolve a sessão salva, ou null se não houver ou se o token já tiver expirado. */
export function lerSessao(): Sessao | null {
  try {
    const salvo = localStorage.getItem(CHAVE)
    if (!salvo) return null

    const sessao = JSON.parse(salvo) as Sessao
    if (new Date(sessao.expiraEm) <= new Date()) {
      removerSessao()
      return null
    }
    return sessao
  } catch {
    return null
  }
}

export function removerSessao() {
  try {
    localStorage.removeItem(CHAVE)
  } catch {
    // nada a fazer
  }
}
