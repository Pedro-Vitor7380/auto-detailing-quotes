import { useMutation, useQuery } from '@tanstack/react-query'
import { Bell, BellOff, LoaderCircle, Send } from 'lucide-react'
import { useEffect, useState } from 'react'
import { apiNotificacoes } from '../../api/notificacoes'
import { AlertaDeErro } from '../../components/formulario'

type Estado = 'carregando' | 'sem-suporte' | 'iphone-sem-instalar' | 'bloqueado' | 'inativo' | 'ativo'

const temSuporte = () => 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window
const ehIphone = () => /iPhone|iPad|iPod/.test(navigator.userAgent)
const estaInstalado = () => window.matchMedia('(display-mode: standalone)').matches

/** A chave pública vem em base64url; o navegador quer os bytes (Uint8Array). */
function paraBytes(base64url: string): Uint8Array<ArrayBuffer> {
  const b64 = base64url.replace(/-/g, '+').replace(/_/g, '/').padEnd(Math.ceil(base64url.length / 4) * 4, '=')
  return Uint8Array.from(atob(b64), (c) => c.charCodeAt(0))
}

/** O service worker do app (o PWA, passo 10). Não existe no "npm run dev": só no app publicado ou no "preview". */
const registroDoApp = () => navigator.serviceWorker.getRegistration()

/**
 * "Avisar este celular um dia antes da entrega". Cada aparelho ativa o seu: a notificação vai para
 * todos os aparelhos ativados da oficina. Some se o servidor não tiver as chaves configuradas.
 */
export function AtivarNotificacoes() {
  const chave = useQuery({ queryKey: ['notificacoes', 'chave'], queryFn: apiNotificacoes.chavePublica, staleTime: Infinity })
  // O que dá para saber na hora vem já no estado inicial; só "está inscrito?" precisa esperar (efeito abaixo).
  const [estado, setEstado] = useState<Estado>(() => {
    if (!temSuporte()) return ehIphone() && !estaInstalado() ? 'iphone-sem-instalar' : 'sem-suporte'
    return Notification.permission === 'denied' ? 'bloqueado' : 'carregando'
  })
  const [mensagem, setMensagem] = useState<string | null>(null)

  // Pergunta ao navegador se este aparelho já está inscrito (resposta assíncrona: um sistema externo).
  useEffect(() => {
    if (estado !== 'carregando') return
    void registroDoApp().then(async (registro) => {
      const inscricao = await registro?.pushManager.getSubscription()
      setEstado(inscricao && Notification.permission === 'granted' ? 'ativo' : registro ? 'inativo' : 'sem-suporte')
    })
  }, [estado])

  const ativar = useMutation({
    mutationFn: async () => {
      // Pedir permissão precisa acontecer no toque (como o "compartilhar", passo 22).
      const permissao = await Notification.requestPermission()
      if (permissao !== 'granted') throw new Error(permissao === 'denied' ? 'bloqueado' : 'cancelado')
      const registro = await registroDoApp()
      if (!registro) throw new Error('sem-app')
      const inscricao = await registro.pushManager.subscribe({
        userVisibleOnly: true, // a regra dos navegadores: toda notificação recebida precisa aparecer para a pessoa
        applicationServerKey: paraBytes(chave.data!),
      })
      const { endpoint, keys } = inscricao.toJSON()
      await apiNotificacoes.inscrever({ endpoint: endpoint!, p256dh: keys!.p256dh, auth: keys!.auth })
    },
    onSuccess: () => {
      setEstado('ativo')
      setMensagem('Pronto! Este celular vai receber o aviso às 18h do dia anterior à entrega.')
    },
    onError: (erro) => {
      if (erro instanceof Error && erro.message === 'bloqueado') setEstado('bloqueado')
    },
  })

  const desativar = useMutation({
    mutationFn: async () => {
      const inscricao = await (await registroDoApp())?.pushManager.getSubscription()
      if (inscricao) {
        await apiNotificacoes.desinscrever(inscricao.endpoint)
        await inscricao.unsubscribe()
      }
    },
    onSuccess: () => {
      setEstado('inativo')
      setMensagem(null)
    },
  })

  const testar = useMutation({
    mutationFn: apiNotificacoes.testar,
    onSuccess: (r) => setMensagem(r.enviadas > 0 ? 'Notificação de teste enviada. Ela deve aparecer em alguns segundos.' : 'Nenhum aparelho recebeu. Desative e ative de novo.'),
  })

  // Servidor sem chaves (null) ou ainda carregando: não mostra nada.
  if (!chave.data || estado === 'carregando') return null

  const textos: Record<Exclude<Estado, 'carregando'>, string> = {
    'sem-suporte': 'Este navegador não recebe notificações. No celular, use o app instalado.',
    'iphone-sem-instalar': 'No iPhone, as notificações só funcionam com o app instalado na tela de início (Compartilhar → Adicionar à Tela de Início).',
    bloqueado: 'As notificações estão bloqueadas para este app. Libere nas configurações do navegador (cadeado ao lado do endereço) e volte aqui.',
    inativo: 'Receba neste celular, às 18h, o aviso dos serviços que terminam no dia seguinte.',
    ativo: 'Este celular recebe o aviso dos serviços que terminam no dia seguinte.',
  }

  return (
    <section className="mt-6 rounded-2xl border border-slate-200 bg-white p-4">
      <h2 className="flex items-center gap-2 font-semibold">
        {estado === 'ativo' ? <Bell className="size-5 text-green-700" aria-hidden /> : <BellOff className="size-5 text-slate-400" aria-hidden />}
        Notificações {estado === 'ativo' ? 'ativadas' : 'neste celular'}
      </h2>
      <p className="mt-1 text-sm text-slate-600">{textos[estado]}</p>
      {mensagem && <p role="status" className="mt-2 rounded-lg bg-green-50 px-3 py-2 text-sm text-green-900">{mensagem}</p>}

      <div className="mt-3 flex flex-wrap gap-2">
        {estado === 'inativo' && (
          <button type="button" onClick={() => ativar.mutate()} disabled={ativar.isPending} className="flex h-11 items-center gap-2 rounded-lg bg-grafite-900 px-4 text-sm font-semibold text-marca-100 disabled:opacity-60">
            {ativar.isPending ? <LoaderCircle className="size-4 animate-spin" aria-hidden /> : <Bell className="size-4" aria-hidden />} Ativar notificações
          </button>
        )}
        {estado === 'ativo' && (
          <>
            <button type="button" onClick={() => testar.mutate()} disabled={testar.isPending} className="flex h-11 items-center gap-2 rounded-lg border border-slate-300 px-3 text-sm font-medium text-slate-700 disabled:opacity-60">
              <Send className="size-4" aria-hidden /> Testar
            </button>
            <button type="button" onClick={() => desativar.mutate()} disabled={desativar.isPending} className="flex h-11 items-center gap-2 rounded-lg px-3 text-sm font-medium text-slate-600 hover:bg-slate-100 disabled:opacity-60">
              <BellOff className="size-4" aria-hidden /> Desativar
            </button>
          </>
        )}
      </div>
      <AlertaDeErro erro={ativar.error instanceof Error && ['bloqueado', 'cancelado'].includes(ativar.error.message) ? null : ativar.error ?? testar.error ?? desativar.error} />
    </section>
  )
}
