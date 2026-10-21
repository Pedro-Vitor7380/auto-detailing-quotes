import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { BellRing, CalendarClock, Check, MessageCircle } from 'lucide-react'
import { Link } from 'react-router'
import { apiAgenda, chavesAgenda, type Entrega } from '../../api/agenda'
import { apiOrcamentos, chavesOrcamentos } from '../../api/orcamentos'
import { AlertaDeErro } from '../../components/formulario'
import { diaPorExtenso } from '../../lib/calendario'
import { formatarPlaca } from '../../lib/formatacao'

/** "Olá, Roberta! Seu Fiat Pulse (placa ZAP1B23) estará pronto amanhã, sexta-feira, 30/10. Qualquer dúvida, é só chamar." */
function mensagemDeEntrega(e: Entrega): string {
  const primeiroNome = e.clienteNome.split(' ')[0]
  return `Olá, ${primeiroNome}! Seu ${e.modelo} (placa ${formatarPlaca(e.placa)}) estará pronto amanhã, ${diaPorExtenso(e.ate)}. Qualquer dúvida, é só chamar.`
}

/**
 * Os carros com entrega amanhã (pedido da equipe): avisar o cliente ou adiar.
 * Aparece mesmo para quem não ativou as notificações no celular. Some quando não há entregas.
 */
export function TerminamAmanha() {
  const entregas = useQuery({ queryKey: chavesAgenda.entregasDeAmanha, queryFn: apiAgenda.entregasDeAmanha })
  if (!entregas.data?.length) return entregas.error ? <AlertaDeErro erro={entregas.error} /> : null

  return (
    <section className="mt-6 rounded-2xl border border-amber-300 bg-amber-50 p-4">
      <h2 className="flex items-center gap-2 font-semibold text-amber-950">
        <BellRing className="size-5" aria-hidden /> Terminam amanhã ({entregas.data.length})
      </h2>
      <p className="mt-1 text-sm text-amber-900">Precisa adiar ou pode avisar o cliente?</p>
      <ul className="mt-3 space-y-3">
        {entregas.data.map((e) => (
          <ItemDeEntrega key={e.orcamentoId} entrega={e} />
        ))}
      </ul>
    </section>
  )
}

function ItemDeEntrega({ entrega: e }: { entrega: Entrega }) {
  const queryClient = useQueryClient()
  // Registra o aviso: o outro celular vê "Cliente avisado" e não avisa de novo.
  const registrar = useMutation({
    mutationFn: () => apiOrcamentos.avisoDeEntrega(e.orcamentoId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: chavesOrcamentos.todos, refetchType: 'all' }),
  })
  const avisado = !!e.avisadoEm || registrar.isSuccess

  return (
    <li className="rounded-xl bg-white p-3">
      <p className="font-medium">{e.clienteNome}</p>
      <p className="text-sm text-slate-500">
        <span className="font-mono">{formatarPlaca(e.placa)}</span> {e.modelo} · {e.numero}
      </p>
      <div className="mt-2 grid grid-cols-2 gap-2">
        {avisado ? (
          <p className="flex h-11 items-center justify-center gap-1 rounded-lg bg-green-50 text-sm font-semibold text-green-800">
            <Check className="size-4" aria-hidden /> Cliente avisado
          </p>
        ) : (
          <a
            href={`https://wa.me/${e.clienteWhatsApp}?text=${encodeURIComponent(mensagemDeEntrega(e))}`}
            target="_blank"
            rel="noreferrer"
            // O link abre o WhatsApp; o clique também registra o aviso (sem esperar: o WhatsApp já está abrindo).
            onClick={() => registrar.mutate()}
            className="flex h-11 items-center justify-center gap-1 rounded-lg bg-green-700 text-sm font-semibold text-white hover:bg-green-800"
          >
            <MessageCircle className="size-4" aria-hidden /> Avisar cliente
          </a>
        )}
        <Link
          to={`/orcamentos/${e.orcamentoId}`}
          className="flex h-11 items-center justify-center gap-1 rounded-lg border border-slate-300 text-sm font-medium text-slate-700 hover:bg-slate-50"
        >
          <CalendarClock className="size-4" aria-hidden /> Adiar
        </Link>
      </div>
      <AlertaDeErro erro={registrar.error} />
    </li>
  )
}
