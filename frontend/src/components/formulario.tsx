import { LoaderCircle } from 'lucide-react'
import type { ReactNode } from 'react'
import { ErroDaApi } from '../api/cliente'

// text-base (16px): abaixo disso, o iPhone dá zoom automático ao tocar no campo.
export const classeDoCampo =
  'mt-1 h-12 w-full rounded-lg border border-slate-300 bg-white px-3 text-base outline-none focus:border-marca-600 focus:ring-2 focus:ring-marca-100 aria-invalid:border-red-500'

export const classeDoBotaoPrincipal =
  'flex h-12 items-center justify-center gap-2 rounded-lg bg-marca-400 px-5 font-semibold text-grafite-900 hover:bg-marca-500 active:scale-[0.98] disabled:opacity-60'

export const classeDoBotaoSecundario =
  'flex h-12 items-center justify-center gap-2 rounded-lg border border-slate-300 bg-white px-5 font-medium text-slate-700 hover:bg-slate-100 disabled:opacity-60'

interface CampoProps {
  id: string
  rotulo: string
  erro?: string
  dica?: string
  children: ReactNode
}

/** Rótulo + campo + mensagem de erro, sempre no mesmo formato. */
export function Campo({ id, rotulo, erro, dica, children }: CampoProps) {
  return (
    <div>
      <label htmlFor={id} className="block text-sm font-medium text-slate-700">
        {rotulo}
      </label>
      {children}
      {erro ? (
        <p id={`${id}-erro`} className="mt-1 text-sm text-red-600">
          {erro}
        </p>
      ) : (
        dica && <p className="mt-1 text-sm text-slate-500">{dica}</p>
      )}
    </div>
  )
}

/** Caixa de erro vinda da API (ex.: "Já existe um serviço com esse nome."). */
export function AlertaDeErro({ erro }: { erro: unknown }) {
  if (!erro) return null
  const mensagem = erro instanceof ErroDaApi ? erro.message : 'Erro inesperado. Tente novamente.'
  return (
    <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">
      {mensagem}
    </p>
  )
}

export function Carregando({ texto = 'Carregando...' }: { texto?: string }) {
  return (
    <p className="mt-8 flex items-center justify-center gap-2 text-slate-500" role="status">
      <LoaderCircle className="size-5 animate-spin" aria-hidden />
      {texto}
    </p>
  )
}
