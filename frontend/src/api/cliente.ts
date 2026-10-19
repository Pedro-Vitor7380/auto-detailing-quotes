import { lerSessao } from '../auth/armazenamentoSessao'

const URL_BASE = import.meta.env.VITE_API_URL ?? ''

/** Erro devolvido pela API, já com a mensagem pronta para mostrar ao usuário. */
export class ErroDaApi extends Error {
  readonly status: number

  constructor(status: number, mensagem: string) {
    super(mensagem)
    this.name = 'ErroDaApi'
    this.status = status
  }
}

// Chamado quando a API recusa o token (401): a sessão expirou ou o token ficou inválido.
let aoSessaoExpirar: () => void = () => {}
export function definirAoSessaoExpirar(callback: () => void) {
  aoSessaoExpirar = callback
}

/**
 * Faz uma requisição à API. Coloca o token automaticamente, converte o corpo para JSON
 * e transforma respostas de erro em ErroDaApi.
 * formato 'arquivo': devolve o conteúdo como Blob (ex.: o PDF do orçamento), em vez de ler JSON.
 */
export async function requisicao<T>(
  caminho: string,
  opcoes: { metodo?: string; corpo?: unknown; formato?: 'json' | 'arquivo' } = {},
): Promise<T> {
  const token = lerSessao()?.token
  const cabecalhos: Record<string, string> = { Accept: 'application/json' }
  if (opcoes.corpo !== undefined) cabecalhos['Content-Type'] = 'application/json'
  if (token) cabecalhos.Authorization = `Bearer ${token}`

  let resposta: Response
  try {
    resposta = await fetch(`${URL_BASE}${caminho}`, {
      method: opcoes.metodo ?? 'GET',
      headers: cabecalhos,
      body: opcoes.corpo !== undefined ? JSON.stringify(opcoes.corpo) : undefined,
    })
  } catch {
    // fetch só "falha" quando nem chegou resposta: sem internet, API desligada, CORS bloqueado...
    throw new ErroDaApi(0, 'Não foi possível conectar ao servidor. Verifique sua internet.')
  }

  if (resposta.status === 401 && token) aoSessaoExpirar()

  if (!resposta.ok) throw new ErroDaApi(resposta.status, await extrairMensagemDeErro(resposta))

  if (opcoes.formato === 'arquivo') return (await resposta.blob()) as T
  return (resposta.status === 204 ? undefined : await resposta.json()) as T
}

// A API responde erros no formato Problem Details: { title, detail, status }.
async function extrairMensagemDeErro(resposta: Response): Promise<string> {
  try {
    const problema = (await resposta.json()) as { detail?: string; title?: string }
    if (problema.detail || problema.title) return (problema.detail ?? problema.title)!
  } catch {
    // corpo vazio ou não-JSON: usa a mensagem genérica abaixo
  }
  return resposta.status >= 500 ? 'Erro no servidor. Tente novamente em instantes.' : `Erro ${resposta.status}.`
}
