import { useQuery } from '@tanstack/react-query'
import { Check, Plus, Search, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { apiCatalogo, chavesCatalogo, type Servico } from '../../api/catalogo'
import { AlertaDeErro, Carregando, classeDoBotaoPrincipal, classeDoCampo } from '../../components/formulario'
import { formatarFaixa, normalizarParaBusca } from '../../lib/formatacao'

interface Props {
  aoFechar: () => void
  aoAdicionar: (servico: Servico) => void
  /** Quantas vezes cada serviço já está no orçamento (para marcar na lista). */
  noOrcamento: Record<string, number>
}

/** Tela cheia por cima do orçamento: no celular, é mais fácil do que uma lista espremida no meio do formulário. */
export function SeletorDeServicos({ aoFechar, aoAdicionar, noOrcamento }: Props) {
  // Mesma chave da tela de Serviços: se o catálogo já foi aberto, vem do cache na hora.
  const servicos = useQuery({ queryKey: chavesCatalogo.servicos, queryFn: apiCatalogo.listarServicos })
  const [busca, setBusca] = useState('')
  const [categoria, setCategoria] = useState<string | null>(null)

  // Esc fecha (computador) e a página de trás não rola enquanto o seletor está aberto.
  useEffect(() => {
    const aoTeclar = (e: KeyboardEvent) => e.key === 'Escape' && aoFechar()
    document.addEventListener('keydown', aoTeclar)
    document.body.style.overflow = 'hidden'
    return () => {
      document.removeEventListener('keydown', aoTeclar)
      document.body.style.overflow = ''
    }
  }, [aoFechar])

  const ativos = useMemo(() => (servicos.data ?? []).filter((s) => s.ativo), [servicos.data])
  const categorias = useMemo(() => [...new Set(ativos.map((s) => s.categoriaNome))], [ativos])
  const filtrados = useMemo(() => {
    const termo = normalizarParaBusca(busca.trim())
    return ativos.filter((s) => (!categoria || s.categoriaNome === categoria) && normalizarParaBusca(s.nome).includes(termo))
  }, [ativos, busca, categoria])

  const totalAdicionados = Object.values(noOrcamento).reduce((a, b) => a + b, 0)

  return (
    <div role="dialog" aria-modal="true" aria-label="Adicionar serviços" className="fixed inset-0 z-30 flex flex-col bg-slate-50 md:inset-x-auto md:right-0 md:w-[28rem] md:border-l md:border-slate-200 md:shadow-xl">
      <div className="flex items-center gap-2 border-b border-slate-200 bg-white px-4 pt-[max(0.75rem,env(safe-area-inset-top))] pb-3">
        <h2 className="flex-1 text-lg font-bold">Adicionar serviços</h2>
        <button type="button" onClick={aoFechar} aria-label="Fechar" className="flex size-11 items-center justify-center rounded-lg text-slate-500 hover:bg-slate-100">
          <X className="size-6" aria-hidden />
        </button>
      </div>

      <div className="border-b border-slate-200 bg-white px-4 pb-3">
        <div className="relative mt-3">
          <Search className="pointer-events-none absolute top-1/2 left-3 size-5 -translate-y-1/2 text-slate-400" aria-hidden />
          <input type="search" placeholder="Buscar serviço" aria-label="Buscar serviço" value={busca} onChange={(e) => setBusca(e.target.value)} className={`${classeDoCampo} mt-0 pl-10`} />
        </div>
        <div className="-mx-4 mt-3 flex gap-2 overflow-x-auto px-4">
          {[null, ...categorias].map((c) => (
            <button
              key={c ?? 'todas'}
              type="button"
              onClick={() => setCategoria(c)}
              aria-pressed={categoria === c}
              className={`h-9 shrink-0 rounded-full border px-4 text-sm font-medium ${categoria === c ? 'border-grafite-900 bg-grafite-900 text-marca-100' : 'border-slate-300 bg-white text-slate-700'}`}
            >
              {c ?? 'Todas'}
            </button>
          ))}
        </div>
      </div>

      <div className="flex-1 overflow-y-auto px-4 py-3">
        {servicos.isPending ? (
          <Carregando />
        ) : servicos.isError ? (
          <AlertaDeErro erro={servicos.error} />
        ) : filtrados.length === 0 ? (
          <p className="mt-8 text-center text-slate-500">Nenhum serviço encontrado.</p>
        ) : (
          <ul className="divide-y divide-slate-100 overflow-hidden rounded-xl border border-slate-200 bg-white">
            {filtrados.map((s) => {
              const vezes = noOrcamento[s.id] ?? 0
              return (
                <li key={s.id}>
                  <button type="button" onClick={() => aoAdicionar(s)} className="flex w-full items-center gap-3 px-4 py-3 text-left active:bg-marca-50">
                    <div className="min-w-0 flex-1">
                      <p className="font-medium">{s.nome}</p>
                      <p className="text-sm text-slate-500">
                        {formatarFaixa(s.precoMinimo, s.precoMaximo)}
                      </p>
                    </div>
                    {vezes > 0 ? (
                      <span className="flex shrink-0 items-center gap-1 rounded-full bg-green-100 px-2 py-1 text-xs font-semibold text-green-800">
                        <Check className="size-3.5" aria-hidden /> {vezes > 1 ? `${vezes}×` : 'Adicionado'}
                      </span>
                    ) : (
                      <Plus className="size-6 shrink-0 text-marca-700" aria-hidden />
                    )}
                  </button>
                </li>
              )
            })}
          </ul>
        )}
      </div>

      <div className="border-t border-slate-200 bg-white px-4 pt-3 pb-[max(0.75rem,env(safe-area-inset-bottom))]">
        <button type="button" onClick={aoFechar} className={`${classeDoBotaoPrincipal} w-full`}>
          {totalAdicionados > 0 ? `Pronto (${totalAdicionados} no orçamento)` : 'Voltar ao orçamento'}
        </button>
      </div>
    </div>
  )
}
