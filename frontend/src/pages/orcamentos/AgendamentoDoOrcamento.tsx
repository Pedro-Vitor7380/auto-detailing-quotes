import { useMutation } from '@tanstack/react-query'
import { CalendarCheck, CalendarX, LoaderCircle, MessageCircle, RefreshCw } from 'lucide-react'
import { useState } from 'react'
import { apiOrcamentos, type Orcamento } from '../../api/orcamentos'
import { CalendarioDaAgenda } from '../../components/CalendarioDaAgenda'
import { AlertaDeErro, classeDoBotaoPrincipal, classeDoBotaoSecundario } from '../../components/formulario'
import { artigoDoDia, descreverPeriodo, diaPorExtenso, diasNoPeriodo } from '../../lib/calendario'
import { formatarPlaca } from '../../lib/formatacao'

interface Periodo {
  de: string | null
  ate: string | null
}

/**
 * Um dia: "Olá, Roberta! Temos a terça-feira, 27/10, disponível para o serviço no Fiat Pulse (placa ZAP1B23). Podemos agendar?"
 * Período: "Olá, Roberta! Temos de terça-feira, 22/10, a sábado, 26/10 disponível para o serviço no ... Podemos agendar?"
 */
function mensagemDoPeriodo(o: Orcamento, de: string, ate: string): string {
  const primeiroNome = o.clienteNome.split(' ')[0]
  const quando = de === ate ? `${artigoDoDia(de)} ${diaPorExtenso(de)},` : `de ${diaPorExtenso(de)}, a ${diaPorExtenso(ate)}`
  return `Olá, ${primeiroNome}! Temos ${quando} disponível para o serviço no ${o.modelo} (placa ${formatarPlaca(o.placa)}). Podemos agendar?`
}

/**
 * Toque 1 = dia em que o carro chega; toque 2 = dia da entrega (pode ser o mesmo).
 * Tocar antes do início, ou depois de já ter os dois, recomeça a escolha.
 */
function proximoPeriodo(atual: Periodo, dia: string): Periodo {
  if (!atual.de || atual.ate || dia < atual.de) return { de: dia, ate: null }
  return { de: atual.de, ate: dia }
}

/** Depois da aprovação: escolher o período no calendário, perguntar ao cliente pelo WhatsApp e confirmar. */
export function AgendamentoDoOrcamento({ orcamento: o, aoMudar }: { orcamento: Orcamento; aoMudar: (o: Orcamento) => void }) {
  const [escolhendo, setEscolhendo] = useState(!o.agendadoDe)
  const [periodo, setPeriodo] = useState<Periodo>({ de: null, ate: null })
  // Só o início escolhido = serviço de um dia (fim = início).
  const de = periodo.de
  const ate = periodo.ate ?? periodo.de

  const confirmar = useMutation({
    mutationFn: () => apiOrcamentos.agendar(o.id, de!, ate!),
    onSuccess: (atualizado) => {
      aoMudar(atualizado) // invalida ['orcamentos']: o calendário recolore
      setEscolhendo(false)
      setPeriodo({ de: null, ate: null })
    },
  })
  const cancelar = useMutation({
    mutationFn: () => apiOrcamentos.cancelarAgendamento(o.id),
    onSuccess: (atualizado) => {
      aoMudar(atualizado)
      setEscolhendo(true)
    },
  })

  if (o.agendadoDe && o.agendadoAte && !escolhendo)
    return (
      <section className="mt-4 rounded-2xl border border-green-200 bg-green-50 p-4">
        <p className="flex items-center gap-2 font-semibold text-green-900">
          <CalendarCheck className="size-5 shrink-0" aria-hidden /> Agendado: {descreverPeriodo(o.agendadoDe, o.agendadoAte)}
        </p>
        <div className="mt-3 flex flex-wrap gap-2">
          <button type="button" onClick={() => setEscolhendo(true)} className={`${classeDoBotaoSecundario} h-11 text-sm`}>
            <RefreshCw className="size-4" aria-hidden /> Reagendar
          </button>
          <button
            type="button"
            disabled={cancelar.isPending}
            onClick={() => window.confirm('Cancelar o agendamento deste orçamento?') && cancelar.mutate()}
            className="flex h-11 items-center gap-2 rounded-lg px-3 text-sm font-medium text-red-700 hover:bg-red-50 disabled:opacity-60"
          >
            <CalendarX className="size-4" aria-hidden /> Cancelar agendamento
          </button>
        </div>
        <AlertaDeErro erro={cancelar.error} />
      </section>
    )

  return (
    <section className="mt-4">
      <h2 className="text-sm font-semibold tracking-wide text-slate-500 uppercase">{o.agendadoDe ? 'Reagendar' : 'Agendar o serviço'}</h2>
      <p className="mt-1 text-sm text-slate-600">
        {!de ? 'Toque no dia em que o carro chega.' : !periodo.ate ? 'Agora toque no dia da entrega (ou confirme, se for um dia só).' : 'Pergunte ao cliente e confirme.'}
      </p>
      <div className="mt-2">
        <CalendarioDaAgenda
          modo="escolher"
          periodo={periodo}
          aoTocarDia={(dia) => setPeriodo((atual) => proximoPeriodo(atual, dia.data))}
          mesInicial={o.agendadoDe ? { ano: Number(o.agendadoDe.slice(0, 4)), mes: Number(o.agendadoDe.slice(5, 7)) } : undefined}
        />
      </div>

      {de && ate && (
        <p className="mt-3 font-medium" aria-live="polite">
          {descreverPeriodo(de, ate)} <span className="text-sm font-normal text-slate-500">({diasNoPeriodo(de, ate)} {diasNoPeriodo(de, ate) === 1 ? 'dia' : 'dias'})</span>
        </p>
      )}

      <div className="mt-3 grid gap-2 sm:grid-cols-2">
        {/* Só texto (sem arquivo): o link wa.me já abre a conversa do cliente com a pergunta escrita.
            Sem dia escolhido é um <button> desabilitado (um <a> sem href não é link nem botão). */}
        {de && ate ? (
          <a
            href={`https://wa.me/${o.clienteWhatsApp}?text=${encodeURIComponent(mensagemDoPeriodo(o, de, ate))}`}
            target="_blank"
            rel="noreferrer"
            className="flex h-12 items-center justify-center gap-2 rounded-lg bg-green-700 px-4 font-semibold text-white hover:bg-green-800"
          >
            <MessageCircle className="size-5" aria-hidden /> Perguntar ao cliente
          </a>
        ) : (
          <button type="button" disabled className="flex h-12 items-center justify-center gap-2 rounded-lg bg-green-700 px-4 font-semibold text-white opacity-50">
            <MessageCircle className="size-5" aria-hidden /> Perguntar ao cliente
          </button>
        )}
        <button type="button" disabled={!de || confirmar.isPending} onClick={() => confirmar.mutate()} className={classeDoBotaoPrincipal}>
          {confirmar.isPending ? <LoaderCircle className="size-5 animate-spin" aria-hidden /> : <CalendarCheck className="size-5" aria-hidden />}
          Confirmar agendamento
        </button>
      </div>
      {o.agendadoDe && o.agendadoAte && (
        <button type="button" onClick={() => { setEscolhendo(false); setPeriodo({ de: null, ate: null }) }} className="mt-2 h-11 text-sm text-slate-600">
          ← Manter {descreverPeriodo(o.agendadoDe, o.agendadoAte)}
        </button>
      )}
      <AlertaDeErro erro={confirmar.error} />
    </section>
  )
}
