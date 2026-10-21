import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, Plus } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import type { DiaDaAgenda } from '../api/agenda'
import { apiPainel, chavesPainel } from '../api/painel'
import { CalendarioDaAgenda } from '../components/CalendarioDaAgenda'
import { AtivarNotificacoes } from './inicio/AtivarNotificacoes'
import { TerminamAmanha } from './inicio/TerminamAmanha'
import { AlertaDeErro } from '../components/formulario'
import { diaPorExtenso } from '../lib/calendario'
import { formatarMoeda, formatarPlaca } from '../lib/formatacao'

/** "2026-10-22" → "22/10" */
const formatarDiaCurto = (iso: string) => `${iso.slice(8)}/${iso.slice(5, 7)}`

const nomeDoMes = (ano: number, mes: number) =>
  new Date(ano, mes - 1, 1).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })

export function InicioPage() {
  const [diaAberto, setDiaAberto] = useState<DiaDaAgenda | null>(null)
  // Lê a data UMA vez, quando a tela abre: new Date() muda a cada chamada, e a tela não deve mudar sozinha.
  const [mesAtual] = useState(() => {
    const hoje = new Date()
    return { ano: hoje.getFullYear(), mes: hoje.getMonth() + 1 }
  })
  const [{ ano, mes }, setPeriodo] = useState(mesAtual)
  const ehMesAtual = ano === mesAtual.ano && mes === mesAtual.mes

  const painel = useQuery({
    queryKey: chavesPainel.mes(ano, mes),
    queryFn: () => apiPainel.obter(ano, mes),
    placeholderData: keepPreviousData, // ao trocar de mês, os números antigos ficam até os novos chegarem
  })

  // new Date(ano, mes - 1 + n, 1) resolve a virada de ano sozinho (mês 0 = dezembro do ano anterior).
  function mudarMes(n: number) {
    const d = new Date(ano, mes - 1 + n, 1)
    setPeriodo({ ano: d.getFullYear(), mes: d.getMonth() + 1 })
  }

  const valor = (texto: string | undefined) =>
    painel.data ? texto : <span className="text-slate-300">—</span>
  const atualizando = painel.isPlaceholderData ? 'opacity-60' : ''

  return (
    <section>
      <h1 className="text-2xl font-bold">Início</h1>
      <p className="mt-1 text-slate-500">Bem-vindo! O que vamos orçar hoje?</p>

      {/* Ação principal do app: grande e fácil de tocar no celular */}
      <Link
        to="/orcamentos/novo"
        className="mt-6 flex h-14 w-full items-center justify-center gap-2 rounded-xl bg-marca-400 text-lg font-semibold text-grafite-900 shadow-sm hover:bg-marca-500 active:scale-[0.98] sm:inline-flex sm:w-auto sm:px-8"
      >
        <Plus className="size-6" aria-hidden />
        Novo orçamento
      </Link>

      {/* O que pede ação hoje vem antes de tudo: carros com entrega amanhã */}
      <TerminamAmanha />

      {/* Agenda primeiro: os dias disponíveis são o que a equipe mais consulta (pedido da equipe) */}
      <section className="mt-6">
        <h2 className="text-sm font-semibold tracking-wide text-slate-500 uppercase">Agenda</h2>
        <div className="mt-2 grid gap-4 lg:grid-cols-[minmax(0,26rem)_1fr]">
          <CalendarioDaAgenda
            modo="ver"
            periodo={{ de: diaAberto?.data ?? null, ate: diaAberto?.data ?? null }}
            aoTocarDia={(dia) => setDiaAberto(dia.data === diaAberto?.data ? null : dia)}
          />
          {diaAberto && <CarrosDoDia dia={diaAberto} />}
        </div>
      </section>

      <h2 className="mt-8 text-sm font-semibold tracking-wide text-slate-500 uppercase">Orçamentos e faturamento</h2>
      <div className="mt-2 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {/* Em aberto não depende do mês: é o que está pendente agora */}
        <Link to="/orcamentos" className="rounded-xl border border-slate-200 bg-white p-5 hover:border-slate-300">
          <p className="text-sm text-slate-500">Orçamentos em aberto</p>
          <p className="mt-1 text-2xl font-bold">{valor(String(painel.data?.emAberto))}</p>
          {painel.data && painel.data.valorEmAberto > 0 && (
            <p className="mt-1 text-sm text-slate-500">{formatarMoeda(painel.data.valorEmAberto)} aguardando o cliente</p>
          )}
        </Link>

        {/* Os dois do mês ficam juntos, com o seletor de mês, para não haver dúvida sobre o período */}
        <div className={`rounded-xl border border-slate-200 bg-white p-5 transition-opacity sm:col-span-1 lg:col-span-2 ${atualizando}`}>
          <div className="-mt-2 -mr-2 flex items-center justify-between">
            <p className="text-sm font-medium text-slate-700 first-letter:uppercase" aria-live="polite">{nomeDoMes(ano, mes)}</p>
            <div className="flex">
              <button type="button" onClick={() => mudarMes(-1)} aria-label="Faturamento: mês anterior" className="flex size-11 items-center justify-center rounded-lg text-slate-500 hover:bg-slate-100">
                <ChevronLeft className="size-5" aria-hidden />
              </button>
              <button type="button" onClick={() => mudarMes(1)} disabled={ehMesAtual} aria-label="Faturamento: próximo mês" className="flex size-11 items-center justify-center rounded-lg text-slate-500 hover:bg-slate-100 disabled:opacity-30">
                <ChevronRight className="size-5" aria-hidden />
              </button>
            </div>
          </div>
          <div className="mt-2 grid grid-cols-2 gap-4">
            <div>
              <p className="text-sm text-slate-500">Aprovados no mês</p>
              <p className="mt-1 text-2xl font-bold">{valor(String(painel.data?.aprovadosNoMes))}</p>
            </div>
            <div>
              <p className="text-sm text-slate-500">Faturamento previsto</p>
              <p className="mt-1 text-2xl font-bold text-marca-700">{valor(painel.data && formatarMoeda(painel.data.faturamentoPrevisto))}</p>
            </div>
          </div>
        </div>
      </div>
      <div className="mt-3">
        <AlertaDeErro erro={painel.error} />
      </div>

      <AtivarNotificacoes />

      {/* Para conferir se o celular já está com a versão mais nova (o app instalado se atualiza sozinho). */}
      <p className="mt-10 text-center text-xs text-slate-500">Versão {__VERSAO_DO_APP__}</p>
    </section>
  )
}

/** Os carros agendados num dia, com link para cada orçamento. */
function CarrosDoDia({ dia }: { dia: DiaDaAgenda }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-4">
      <p className="font-semibold first-letter:uppercase">{diaPorExtenso(dia.data)}</p>
      {dia.carros.length === 0 ? (
        <p className="mt-1 text-sm text-slate-500">Nenhum carro agendado.</p>
      ) : (
        <ul className="mt-2 divide-y divide-slate-100">
          {dia.carros.map((c) => (
            <li key={c.orcamentoId}>
              <Link to={`/orcamentos/${c.orcamentoId}`} className="flex flex-col py-2 hover:bg-slate-50">
                <span className="font-medium">{c.clienteNome}</span>
                <span className="text-sm text-slate-500">
                  <span className="font-mono">{formatarPlaca(c.placa)}</span> {c.modelo} · {c.numero}
                </span>
                {/* O período inteiro, e se neste dia o carro chega ou é entregue */}
                <span className="mt-0.5 flex flex-wrap items-center gap-2 text-xs text-slate-600">
                  {c.de !== c.ate && <span>{formatarDiaCurto(c.de)} → {formatarDiaCurto(c.ate)}</span>}
                  {dia.data === c.de && <span className="rounded-full bg-sky-100 px-2 py-0.5 font-semibold text-sky-900">chega</span>}
                  {dia.data === c.ate && <span className="rounded-full bg-green-100 px-2 py-0.5 font-semibold text-green-900">entrega</span>}
                </span>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
