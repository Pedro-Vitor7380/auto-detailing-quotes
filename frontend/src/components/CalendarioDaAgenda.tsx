import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { useState } from 'react'
import { apiAgenda, chavesAgenda, type DiaDaAgenda } from '../api/agenda'
import { diaDaSemanaDoPrimeiro, mudarMes, nomeDoMes, periodoDoMes } from '../lib/calendario'
import { AlertaDeErro } from './formulario'

/**
 * Cores pedidas pela equipe (com o limite de 3 carros por dia):
 *   0 carros = azul claro · 1 = verde · 2 = laranja · 3 (lotado) = vermelho · dia escolhido = azul escuro.
 * Com outro limite, a regra é a mesma: falta 1 vaga = laranja, lotado = vermelho, entre 1 e isso = verde.
 * Texto escuro nas cores claras e branco nas escuras: contraste acima de 4,5:1 em todas.
 */
function corDoDia(ocupados: number, carrosPorDia: number): string {
  if (ocupados === 0) return 'border border-sky-200 bg-sky-50 text-sky-900'
  if (ocupados >= carrosPorDia) return 'bg-red-600 text-white'
  if (carrosPorDia - ocupados === 1) return 'bg-orange-400 text-orange-950'
  return 'bg-green-500 text-green-950'
}

const SELECIONADO = 'bg-blue-800 text-white ring-2 ring-blue-300 ring-offset-1'
const FECHADO = 'bg-slate-100 text-slate-400'
const SEMANA = ['D', 'S', 'T', 'Q', 'Q', 'S', 'S']

interface Props {
  /** "escolher": só dias abertos e com vaga podem ser tocados. "ver": qualquer dia (para ver os carros). */
  modo: 'escolher' | 'ver'
  selecionado?: string | null
  aoTocarDia: (dia: DiaDaAgenda, carrosPorDia: number) => void
  /** Mês que aparece primeiro (padrão: o atual). */
  mesInicial?: { ano: number; mes: number }
}

export function CalendarioDaAgenda({ modo, selecionado, aoTocarDia, mesInicial }: Props) {
  const [periodo, setPeriodo] = useState(() => {
    const hoje = new Date()
    return mesInicial ?? { ano: hoje.getFullYear(), mes: hoje.getMonth() + 1 }
  })
  const { de, ate } = periodoDoMes(periodo.ano, periodo.mes)
  const agenda = useQuery({
    queryKey: chavesAgenda.periodo(de, ate),
    queryFn: () => apiAgenda.obter(de, ate),
    placeholderData: keepPreviousData, // ao trocar de mês, as cores antigas ficam até as novas chegarem
  })

  const carrosPorDia = agenda.data?.carrosPorDia ?? 3
  const casasVazias = diaDaSemanaDoPrimeiro(periodo.ano, periodo.mes)

  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-3">
      <div className="flex items-center justify-between">
        <button type="button" onClick={() => setPeriodo(mudarMes(periodo.ano, periodo.mes, -1))} aria-label="Agenda: mês anterior" className="flex size-11 items-center justify-center rounded-lg text-slate-500 hover:bg-slate-100">
          <ChevronLeft className="size-5" aria-hidden />
        </button>
        <p className="font-semibold" aria-live="polite">{nomeDoMes(periodo.ano, periodo.mes)}</p>
        <button type="button" onClick={() => setPeriodo(mudarMes(periodo.ano, periodo.mes, 1))} aria-label="Agenda: próximo mês" className="flex size-11 items-center justify-center rounded-lg text-slate-500 hover:bg-slate-100">
          <ChevronRight className="size-5" aria-hidden />
        </button>
      </div>

      <div className={`mt-1 grid grid-cols-7 gap-1 text-center transition-opacity ${agenda.isPlaceholderData ? 'opacity-60' : ''}`}>
        {SEMANA.map((letra, i) => (
          <span key={i} className="py-1 text-xs font-semibold text-slate-500" aria-hidden>
            {letra}
          </span>
        ))}
        {Array.from({ length: casasVazias }, (_, i) => (
          <span key={`vazia-${i}`} />
        ))}
        {(agenda.data?.dias ?? []).map((dia) => {
          const lotado = dia.ocupados >= carrosPorDia
          const ehSelecionado = dia.data === selecionado
          const domingo = new Date(`${dia.data}T12:00:00`).getDay() === 0
          // No modo "escolher", só dá para tocar em dia aberto (não é passado nem domingo) e com vaga.
          const podeTocar = modo === 'ver' ? !domingo : dia.aberto && !lotado
          const corBase = domingo || (modo === 'escolher' && !dia.aberto) ? FECHADO : corDoDia(dia.ocupados, carrosPorDia)
          // Escolhendo: o dia escolhido fica azul escuro. Só vendo (Início): mantém a cor da lotação e ganha um
          // contorno, senão o laranja/vermelho do dia tocado sumiria justamente quando se quer ver os carros dele.
          const cor = !ehSelecionado ? corBase : modo === 'escolher' ? SELECIONADO : `${corBase} ring-2 ring-blue-800 ring-offset-2`
          // No modo "ver", dias que já passaram ficam apagados (histórico), mas mantêm a cor.
          const passado = modo === 'ver' && !dia.aberto && !domingo ? 'opacity-50' : ''
          const numero = Number(dia.data.slice(8))
          return (
            <button
              key={dia.data}
              type="button"
              disabled={!podeTocar}
              onClick={() => aoTocarDia(dia, carrosPorDia)}
              aria-pressed={ehSelecionado}
              aria-label={`${numero}: ${domingo ? 'fechado' : `${dia.ocupados} de ${carrosPorDia} carros`}`}
              className={`flex h-11 items-center justify-center rounded-lg text-sm font-semibold transition-transform active:scale-95 disabled:cursor-not-allowed ${cor} ${passado}`}
            >
              {numero}
            </button>
          )
        })}
      </div>

      {/* Legenda: as cores precisam de explicação para quem chega na oficina */}
      <ul className="mt-3 flex flex-wrap gap-x-3 gap-y-1 text-xs text-slate-600" aria-label="Legenda">
        {[
          ['border border-sky-200 bg-sky-50', 'Livre'],
          // Com limite 3: verde = 1 carro, laranja = 2. Com limite 2 não existe verde (1 carro já é "falta 1 vaga").
          ...(carrosPorDia >= 3 ? [['bg-green-500', carrosPorDia === 3 ? '1 carro' : `1 a ${carrosPorDia - 2} carros`]] : []),
          ...(carrosPorDia >= 2 ? [['bg-orange-400', `${carrosPorDia - 1} ${carrosPorDia - 1 === 1 ? 'carro' : 'carros'}`]] : []),
          ['bg-red-600', 'Lotado'],
          ...(modo === 'escolher' ? [['bg-blue-800', 'Escolhido']] : []),
        ].map(([cor, rotulo]) => (
          <li key={rotulo} className="flex items-center gap-1">
            <span className={`inline-block size-3 rounded ${cor}`} aria-hidden /> {rotulo}
          </li>
        ))}
      </ul>
      <AlertaDeErro erro={agenda.error} />
    </div>
  )
}
