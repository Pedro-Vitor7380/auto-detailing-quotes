import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { useState } from 'react'
import { apiAgenda, chavesAgenda, type DiaDaAgenda } from '../api/agenda'
import { diaDaSemanaDoPrimeiro, mudarMes, nomeDoMes, periodoDoMes } from '../lib/calendario'
import { AlertaDeErro } from './formulario'

/**
 * Cores pedidas pela equipe, pela quantidade de carros na oficina naquele dia (sem limite de carros):
 *   0 = azul claro · 1 = verde · 2 = laranja · 3 ou mais = vermelho · período escolhido = azul escuro.
 * Vermelho avisa "dia cheio", mas não bloqueia: quem decide se cabe mais um é a equipe.
 * Texto escuro nas cores claras e branco nas escuras: contraste acima de 4,5:1 em todas.
 */
function corDoDia(ocupados: number): string {
  if (ocupados === 0) return 'border border-sky-200 bg-sky-50 text-sky-900'
  if (ocupados === 1) return 'bg-green-500 text-green-950'
  if (ocupados === 2) return 'bg-orange-400 text-orange-950'
  return 'bg-red-600 text-white'
}

const ESCOLHIDO = 'bg-blue-800 text-white'
const FECHADO = 'bg-slate-100 text-slate-400'
const SEMANA = ['D', 'S', 'T', 'Q', 'Q', 'S', 'S']
const LEGENDA = [
  ['border border-sky-200 bg-sky-50', 'Livre'],
  ['bg-green-500', '1 carro'],
  ['bg-orange-400', '2 carros'],
  ['bg-red-600', '3 ou mais'],
]

interface Props {
  /** "escolher": toca-se no 1º e no último dia do período. "ver": qualquer dia, para ver os carros. */
  modo: 'escolher' | 'ver'
  /** Escolher: o período marcado (azul escuro). Ver: o dia tocado (ganha só um contorno). */
  periodo?: { de: string | null; ate: string | null }
  aoTocarDia: (dia: DiaDaAgenda) => void
  /** Mês que aparece primeiro (padrão: o atual). */
  mesInicial?: { ano: number; mes: number }
}

export function CalendarioDaAgenda({ modo, periodo, aoTocarDia, mesInicial }: Props) {
  const [mes, setMes] = useState(() => {
    const hoje = new Date()
    return mesInicial ?? { ano: hoje.getFullYear(), mes: hoje.getMonth() + 1 }
  })
  const { de, ate } = periodoDoMes(mes.ano, mes.mes)
  const agenda = useQuery({
    queryKey: chavesAgenda.periodo(de, ate),
    queryFn: () => apiAgenda.obter(de, ate),
    placeholderData: keepPreviousData, // ao trocar de mês, as cores antigas ficam até as novas chegarem
  })

  // Datas "AAAA-MM-DD" se comparam como texto: "2026-10-22" < "2026-10-26".
  const inicio = periodo?.de ?? null
  const fim = periodo?.ate ?? inicio
  const dentroDoPeriodo = (data: string) => inicio !== null && fim !== null && data >= inicio && data <= fim

  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-3">
      <div className="flex items-center justify-between">
        <button type="button" onClick={() => setMes(mudarMes(mes.ano, mes.mes, -1))} aria-label="Agenda: mês anterior" className="flex size-11 items-center justify-center rounded-lg text-slate-500 hover:bg-slate-100">
          <ChevronLeft className="size-5" aria-hidden />
        </button>
        <p className="font-semibold" aria-live="polite">{nomeDoMes(mes.ano, mes.mes)}</p>
        <button type="button" onClick={() => setMes(mudarMes(mes.ano, mes.mes, 1))} aria-label="Agenda: próximo mês" className="flex size-11 items-center justify-center rounded-lg text-slate-500 hover:bg-slate-100">
          <ChevronRight className="size-5" aria-hidden />
        </button>
      </div>

      <div className={`mt-1 grid grid-cols-7 gap-1 text-center transition-opacity ${agenda.isPlaceholderData ? 'opacity-60' : ''}`}>
        {SEMANA.map((letra, i) => (
          <span key={i} className="py-1 text-xs font-semibold text-slate-500" aria-hidden>
            {letra}
          </span>
        ))}
        {Array.from({ length: diaDaSemanaDoPrimeiro(mes.ano, mes.mes) }, (_, i) => (
          <span key={`vazia-${i}`} />
        ))}
        {(agenda.data?.dias ?? []).map((dia) => {
          const domingo = new Date(`${dia.data}T12:00:00`).getDay() === 0
          const noPeriodo = dentroDoPeriodo(dia.data)
          // Escolhendo: só se começa ou termina em dia aberto (não é passado nem domingo). Vendo: qualquer dia, menos domingo.
          const podeTocar = modo === 'ver' ? !domingo : dia.aberto
          const fechado = domingo || (modo === 'escolher' && !dia.aberto)
          const corBase = fechado ? FECHADO : corDoDia(dia.ocupados)
          // Escolhendo: o período inteiro fica azul escuro (inclusive um domingo no meio, em que o carro fica).
          // Vendo: o dia tocado mantém a cor da lotação e ganha só um contorno.
          const cor = !noPeriodo ? corBase : modo === 'escolher' ? ESCOLHIDO : `${corBase} ring-2 ring-blue-800 ring-offset-2`
          const passado = modo === 'ver' && !dia.aberto && !domingo ? 'opacity-50' : ''
          const numero = Number(dia.data.slice(8))
          return (
            <button
              key={dia.data}
              type="button"
              disabled={!podeTocar}
              onClick={() => aoTocarDia(dia)}
              aria-pressed={noPeriodo}
              aria-label={`${numero}: ${domingo ? 'fechado' : `${dia.ocupados} ${dia.ocupados === 1 ? 'carro' : 'carros'}`}`}
              className={`flex h-11 items-center justify-center rounded-lg text-sm font-semibold transition-transform active:scale-95 disabled:cursor-not-allowed ${cor} ${passado}`}
            >
              {numero}
            </button>
          )
        })}
      </div>

      {/* Legenda: as cores precisam de explicação para quem chega na oficina */}
      <ul className="mt-3 flex flex-wrap gap-x-3 gap-y-1 text-xs text-slate-600" aria-label="Legenda">
        {[...LEGENDA, ...(modo === 'escolher' ? [['bg-blue-800', 'Escolhido']] : [])].map(([cor, rotulo]) => (
          <li key={rotulo} className="flex items-center gap-1">
            <span className={`inline-block size-3 rounded ${cor}`} aria-hidden /> {rotulo}
          </li>
        ))}
      </ul>
      <AlertaDeErro erro={agenda.error} />
    </div>
  )
}
