import { useMutation } from '@tanstack/react-query'
import { CalendarCheck, CalendarX, LoaderCircle, MessageCircle, RefreshCw } from 'lucide-react'
import { useState } from 'react'
import { apiOrcamentos, type Orcamento } from '../../api/orcamentos'
import { CalendarioDaAgenda } from '../../components/CalendarioDaAgenda'
import { AlertaDeErro, classeDoBotaoPrincipal, classeDoBotaoSecundario } from '../../components/formulario'
import { artigoDoDia, diaPorExtenso } from '../../lib/calendario'
import { formatarPlaca } from '../../lib/formatacao'

/** "Olá, Roberta! Temos a terça-feira, 27/10, disponível para o serviço no Fiat Pulse (placa ZAP1B23). Podemos agendar?" */
function mensagemDoDia(o: Orcamento, dia: string): string {
  const primeiroNome = o.clienteNome.split(' ')[0]
  return `Olá, ${primeiroNome}! Temos ${artigoDoDia(dia)} ${diaPorExtenso(dia)}, disponível para o serviço no ${o.modelo} (placa ${formatarPlaca(o.placa)}). Podemos agendar?`
}

/**
 * Depois da aprovação: escolher um dia no calendário, perguntar ao cliente pelo WhatsApp e confirmar.
 * Fluxo combinado com a equipe: o dia escolhido fica azul escuro; ao confirmar, passa a contar na agenda.
 */
export function AgendamentoDoOrcamento({ orcamento: o, aoMudar }: { orcamento: Orcamento; aoMudar: (o: Orcamento) => void }) {
  const [escolhendo, setEscolhendo] = useState(!o.dataAgendada)
  const [dia, setDia] = useState<string | null>(null)

  const confirmar = useMutation({
    mutationFn: (data: string) => apiOrcamentos.agendar(o.id, data),
    onSuccess: (atualizado) => {
      aoMudar(atualizado) // invalida ['orcamentos']: o calendário recolore (o dia passa a contar na lotação)
      setEscolhendo(false)
      setDia(null)
    },
  })
  const cancelar = useMutation({
    mutationFn: () => apiOrcamentos.cancelarAgendamento(o.id),
    onSuccess: (atualizado) => {
      aoMudar(atualizado)
      setEscolhendo(true)
    },
  })

  if (o.dataAgendada && !escolhendo)
    return (
      <section className="mt-4 rounded-2xl border border-green-200 bg-green-50 p-4">
        <p className="flex items-center gap-2 font-semibold text-green-900">
          <CalendarCheck className="size-5" aria-hidden /> Agendado para {diaPorExtenso(o.dataAgendada)}
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
      <h2 className="text-sm font-semibold tracking-wide text-slate-500 uppercase">
        {o.dataAgendada ? 'Reagendar' : 'Agendar o serviço'}
      </h2>
      <p className="mt-1 text-sm text-slate-600">Toque num dia livre, pergunte ao cliente e confirme.</p>
      <div className="mt-2">
        <CalendarioDaAgenda
          modo="escolher"
          selecionado={dia}
          aoTocarDia={(d) => setDia(d.data === dia ? null : d.data)} // tocar de novo desmarca
          mesInicial={o.dataAgendada ? { ano: Number(o.dataAgendada.slice(0, 4)), mes: Number(o.dataAgendada.slice(5, 7)) } : undefined}
        />
      </div>

      {dia && <p className="mt-3 font-medium">Dia escolhido: {diaPorExtenso(dia)}</p>}

      <div className="mt-3 grid gap-2 sm:grid-cols-2">
        {/* Só texto (sem arquivo): o link wa.me já abre a conversa do cliente com a pergunta escrita.
            Sem dia escolhido é um <button> desabilitado: um <a> sem href não é link para o navegador
            nem para leitores de tela, e "desabilitar" um link não existe em HTML. */}
        {dia ? (
          <a
            href={`https://wa.me/${o.clienteWhatsApp}?text=${encodeURIComponent(mensagemDoDia(o, dia))}`}
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
        <button
          type="button"
          disabled={!dia || confirmar.isPending}
          onClick={() => dia && confirmar.mutate(dia)}
          className={classeDoBotaoPrincipal}
        >
          {confirmar.isPending ? <LoaderCircle className="size-5 animate-spin" aria-hidden /> : <CalendarCheck className="size-5" aria-hidden />}
          Confirmar agendamento
        </button>
      </div>
      {o.dataAgendada && (
        <button type="button" onClick={() => { setEscolhendo(false); setDia(null) }} className="mt-2 h-11 text-sm text-slate-600">
          ← Manter {diaPorExtenso(o.dataAgendada)}
        </button>
      )}
      <AlertaDeErro erro={confirmar.error} />
    </section>
  )
}
