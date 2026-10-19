import { useMutation, useQuery } from '@tanstack/react-query'
import { Check, LoaderCircle, MessageCircle } from 'lucide-react'
import { useState } from 'react'
import { apiOrcamentos, chavesOrcamentos, type Orcamento } from '../../api/orcamentos'
import { AlertaDeErro } from '../../components/formulario'
import { formatarData, formatarMoeda, formatarPlaca } from '../../lib/formatacao'

interface Props {
  orcamento: Orcamento
  bloqueado: boolean
  aoMudar: (o: Orcamento) => void
}

/** Texto que acompanha o PDF. Curto: no celular do cliente, ele aparece como prévia da mensagem. */
function mensagemDoOrcamento(o: Orcamento): string {
  const primeiroNome = o.clienteNome.split(' ')[0]
  return [
    `Olá, ${primeiroNome}! Segue o orçamento ${o.numero} para o ${o.modelo} (placa ${formatarPlaca(o.placa)}).`,
    `Total: ${formatarMoeda(o.total)}, válido até ${formatarData(o.validade)}.`,
    'Qualquer dúvida, é só responder esta mensagem.',
  ].join('\n')
}

/**
 * Celular: abre o "compartilhar" do sistema com o PDF anexado (Web Share API) — escolhe-se o WhatsApp e o contato.
 * Computador (não compartilha arquivos): baixa o PDF e abre o WhatsApp Web na conversa do cliente, com o texto pronto.
 * Depois do envio, um rascunho passa a "Enviado".
 */
export function EnviarPeloWhatsApp({ orcamento: o, bloqueado, aoMudar }: Props) {
  const [aviso, setAviso] = useState<string | null>(null)

  // O PDF é preparado ASSIM QUE a tela abre. O navegador só deixa abrir o "compartilhar" no instante do
  // toque; se o app fosse baixar o PDF depois do toque (até ~25 s com a API acordando), o toque "venceria"
  // e o navegador recusaria. Depois de cada salvamento, o cache de ['orcamentos'] é invalidado e o PDF é refeito.
  const pdf = useQuery({
    queryKey: chavesOrcamentos.pdf(o.id),
    queryFn: () => apiOrcamentos.pdf(o.id),
    enabled: o.itens.length > 0,
    staleTime: Infinity, // só muda quando o orçamento muda (e aí é invalidado)
  })

  const marcarEnviado = useMutation({
    mutationFn: () => apiOrcamentos.enviar(o.id),
    onSuccess: aoMudar,
  })

  function depoisDeEnviar() {
    if (o.status === 'Rascunho') marcarEnviado.mutate()
  }

  // Sem "async" e sem "await" antes do share: a chamada precisa acontecer DENTRO do toque.
  function enviar() {
    setAviso(null)
    const arquivo = new File([pdf.data!], `${o.numero}.pdf`, { type: 'application/pdf' })
    const mensagem = mensagemDoOrcamento(o)
    const dados = { files: [arquivo], text: mensagem }

    if (navigator.canShare?.(dados)) {
      navigator.share(dados).then(depoisDeEnviar, (erro: unknown) => {
        // AbortError = a pessoa fechou o "compartilhar" sem escolher. Não é erro, e o status não muda.
        if (!(erro instanceof DOMException && erro.name === 'AbortError'))
          setAviso('Não foi possível abrir o compartilhamento. Use o botão PDF e envie o arquivo pelo WhatsApp.')
      })
      return
    }

    // Computador: baixa o PDF e abre a conversa no WhatsApp Web (wa.me), com a mensagem já escrita.
    const url = URL.createObjectURL(arquivo)
    Object.assign(document.createElement('a'), { href: url, download: arquivo.name }).click()
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
    window.open(`https://wa.me/${o.clienteWhatsApp}?text=${encodeURIComponent(mensagem)}`, '_blank', 'noopener')
    setAviso(`O PDF foi baixado (${arquivo.name}). Arraste-o para a conversa do WhatsApp que abriu.`)
    depoisDeEnviar()
  }

  const preparando = pdf.isPending && o.itens.length > 0

  return (
    <div className="mt-3">
      <button
        type="button"
        onClick={enviar}
        disabled={bloqueado || !pdf.data || marcarEnviado.isPending}
        className="flex h-12 w-full items-center justify-center gap-2 rounded-lg bg-green-700 px-5 font-semibold text-white hover:bg-green-800 active:scale-[0.98] disabled:opacity-60"
      >
        {preparando || marcarEnviado.isPending ? (
          <LoaderCircle className="size-5 animate-spin" aria-hidden />
        ) : (
          <MessageCircle className="size-5" aria-hidden />
        )}
        {preparando ? 'Preparando o PDF...' : 'Enviar pelo WhatsApp'}
      </button>
      {o.itens.length === 0 && <p className="mt-1 text-sm text-slate-500">Adicione serviços para enviar o orçamento.</p>}
      {aviso && (
        <p role="status" className="mt-2 flex items-start gap-2 rounded-lg bg-green-50 px-3 py-2 text-sm text-green-900">
          <Check className="mt-0.5 size-4 shrink-0" aria-hidden /> {aviso}
        </p>
      )}
      <div className="mt-2">
        <AlertaDeErro erro={pdf.error ?? marcarEnviado.error} />
      </div>
    </div>
  )
}
