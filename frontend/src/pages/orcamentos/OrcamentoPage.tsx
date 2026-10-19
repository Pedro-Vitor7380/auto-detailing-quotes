import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Check, Copy, FileText, LoaderCircle, Send, Trash2, X } from 'lucide-react'
import { useCallback, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { apiOrcamentos, chavesOrcamentos, type Orcamento } from '../../api/orcamentos'
import { AlertaDeErro, Carregando, classeDoBotaoSecundario } from '../../components/formulario'
import { SeloDeStatus } from '../../components/SeloDeStatus'
import { formatarData } from '../../lib/formatacao'
import { EnviarPeloWhatsApp } from './EnviarPeloWhatsApp'
import { FormularioDoOrcamento, ResumoDoOrcamento } from './FormularioDoOrcamento'

/** /orcamentos/novo (sem id) ou /orcamentos/:id. */
export function OrcamentoPage() {
  const { id } = useParams()
  const navegar = useNavigate()
  const queryClient = useQueryClient()
  const orcamento = useQuery({
    queryKey: chavesOrcamentos.orcamento(id ?? ''),
    queryFn: () => apiOrcamentos.obter(id!),
    enabled: !!id,
  })

  const guardar = useCallback(
    (o: Orcamento) => {
      queryClient.setQueryData(chavesOrcamentos.orcamento(o.id), o)
      void queryClient.invalidateQueries({ queryKey: chavesOrcamentos.todos, refetchType: 'all' })
    },
    [queryClient],
  )

  const voltar = (
    <Link to="/orcamentos" className="-ml-1 inline-flex h-11 items-center gap-1 text-sm font-medium text-slate-600">
      <ArrowLeft className="size-4" aria-hidden />
      Orçamentos
    </Link>
  )

  if (!id)
    return (
      <section className="mx-auto max-w-3xl">
        {voltar}
        <h1 className="text-2xl font-bold">Novo orçamento</h1>
        <FormularioDoOrcamento
          aoSalvar={(o) => {
            guardar(o)
            // replace: o "voltar" do celular não reabre o formulário vazio.
            navegar(`/orcamentos/${o.id}`, { replace: true })
          }}
        />
      </section>
    )

  return (
    <section className="mx-auto max-w-3xl">
      {voltar}
      {orcamento.isPending ? (
        <Carregando />
      ) : orcamento.isError ? (
        <AlertaDeErro erro={orcamento.error} />
      ) : (
        <Detalhe key={orcamento.data.id} orcamento={orcamento.data} aoMudar={guardar} />
      )}
    </section>
  )
}

function Detalhe({ orcamento: o, aoMudar }: { orcamento: Orcamento; aoMudar: (o: Orcamento) => void }) {
  const navegar = useNavigate()
  const queryClient = useQueryClient()
  const [temAlteracoes, setTemAlteracoes] = useState(false)

  const acao = useMutation({
    mutationFn: (fazer: () => Promise<Orcamento | void>) => fazer(),
  })

  async function executar(fazer: () => Promise<Orcamento>, depois?: (r: Orcamento) => void) {
    const resultado = await acao.mutateAsync(fazer).catch(() => null)
    if (resultado) (depois ?? aoMudar)(resultado)
  }

  async function excluir() {
    if (!window.confirm(`Excluir o rascunho ${o.numero}?`)) return
    const ok = await acao.mutateAsync(() => apiOrcamentos.excluir(o.id)).then(() => true, () => false)
    if (!ok) return
    queryClient.removeQueries({ queryKey: chavesOrcamentos.orcamento(o.id) })
    await queryClient.invalidateQueries({ queryKey: chavesOrcamentos.todos, refetchType: 'all' })
    navegar('/orcamentos', { replace: true })
  }

  const pdf = useMutation({
    mutationFn: async () => {
      // A aba é aberta ANTES de baixar: o navegador só deixa abrir abas no instante do clique
      // (depois de um "await", ele trata como pop-up e bloqueia). No celular, baixar é melhor que outra aba.
      const noComputador = window.matchMedia('(pointer: fine)').matches
      const aba = noComputador ? window.open('', '_blank') : null
      try {
        // fetchQuery: usa o PDF que o botão do WhatsApp já preparou (cache); só baixa se ainda não tiver.
        const arquivo = await queryClient.fetchQuery({
          queryKey: chavesOrcamentos.pdf(o.id),
          queryFn: () => apiOrcamentos.pdf(o.id),
          staleTime: Infinity,
        })
        const url = URL.createObjectURL(arquivo)
        if (aba) aba.location.href = url
        else {
          const link = Object.assign(document.createElement('a'), { href: url, download: `${o.numero}.pdf` })
          link.click()
        }
        // Libera a memória do arquivo depois de um tempo (a aba/download já o leu).
        setTimeout(() => URL.revokeObjectURL(url), 60_000)
      } catch (erro) {
        aba?.close()
        throw erro
      }
    },
  })

  const bloqueado = acao.isPending || temAlteracoes
  const emAberto = o.status === 'Rascunho' || o.status === 'Enviado'

  return (
    <>
      <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
        <h1 className="text-2xl font-bold">{o.numero}</h1>
        <SeloDeStatus status={o.status} />
      </div>
      <p className="text-sm text-slate-500">
        Criado em {formatarData(o.criadoEm)}
        {o.aprovadoEm && ` · aprovado em ${formatarData(o.aprovadoEm)}`}
        {o.recusadoEm && ` · recusado em ${formatarData(o.recusadoEm)}`}
      </p>

      {/* A ação principal, grande e no alto: é o que se faz no pátio depois de montar o orçamento */}
      <EnviarPeloWhatsApp orcamento={o} bloqueado={bloqueado} aoMudar={aoMudar} />

      {/* Ações de status: rolam na horizontal no celular, em vez de quebrar em várias linhas */}
      <div className="-mx-4 mt-3 flex gap-2 overflow-x-auto px-4 pb-1">
        {/* PDF bloqueado com alterações pendentes: sairia com os valores antigos, diferentes dos da tela */}
        <button type="button" disabled={bloqueado || pdf.isPending || o.itens.length === 0} onClick={() => pdf.mutate()} className={`${classeDoBotaoSecundario} h-11 shrink-0 px-3 text-sm`}>
          {pdf.isPending ? <LoaderCircle className="size-4 animate-spin" aria-hidden /> : <FileText className="size-4" aria-hidden />} PDF
        </button>
        {o.status === 'Rascunho' && (
          <button type="button" disabled={bloqueado} onClick={() => executar(() => apiOrcamentos.enviar(o.id))} className={`${classeDoBotaoSecundario} h-11 shrink-0 px-3 text-sm`}>
            <Send className="size-4" aria-hidden /> Marcar como enviado
          </button>
        )}
        {emAberto && (
          <>
            <button type="button" disabled={bloqueado} onClick={() => executar(() => apiOrcamentos.aprovar(o.id))} className="flex h-11 shrink-0 items-center gap-2 rounded-lg bg-green-700 px-3 text-sm font-semibold text-white hover:bg-green-800 disabled:opacity-60">
              <Check className="size-4" aria-hidden /> Aprovado
            </button>
            <button type="button" disabled={bloqueado} onClick={() => executar(() => apiOrcamentos.recusar(o.id))} className={`${classeDoBotaoSecundario} h-11 shrink-0 px-3 text-sm`}>
              <X className="size-4" aria-hidden /> Recusado
            </button>
          </>
        )}
        <button
          type="button"
          disabled={bloqueado}
          onClick={() => executar(() => apiOrcamentos.duplicar(o.id), (copia) => {
            aoMudar(copia)
            navegar(`/orcamentos/${copia.id}`)
          })}
          className={`${classeDoBotaoSecundario} h-11 shrink-0 px-3 text-sm`}
        >
          <Copy className="size-4" aria-hidden /> Duplicar
        </button>
        {o.status === 'Rascunho' && (
          <button type="button" disabled={bloqueado} onClick={excluir} className="flex h-11 shrink-0 items-center gap-2 rounded-lg px-3 text-sm font-medium text-red-600 hover:bg-red-50 disabled:opacity-60">
            <Trash2 className="size-4" aria-hidden /> Excluir
          </button>
        )}
      </div>
      {temAlteracoes && <p className="mt-1 text-sm text-amber-800">Salve as alterações para enviar, gerar o PDF ou mudar o status.</p>}
      <div className="mt-2">
        <AlertaDeErro erro={acao.error ?? pdf.error} />
      </div>

      {o.podeSerEditado ? (
        <FormularioDoOrcamento orcamento={o} aoSalvar={aoMudar} aoMudarPendencia={setTemAlteracoes} />
      ) : (
        <>
          <p className="mt-3 rounded-lg bg-slate-100 px-3 py-2 text-sm text-slate-600">
            Orçamento {o.status.toLowerCase()}: não pode ser alterado. Use <strong>Duplicar</strong> para criar um novo a partir dele.
          </p>
          <ResumoDoOrcamento orcamento={o} />
        </>
      )}
    </>
  )
}
