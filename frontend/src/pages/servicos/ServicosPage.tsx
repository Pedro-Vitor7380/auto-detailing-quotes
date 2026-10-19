import { useQuery } from '@tanstack/react-query'
import { ChevronRight, Plus, Search, Tags } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link } from 'react-router'
import { apiCatalogo, chavesCatalogo, type Servico } from '../../api/catalogo'
import { AlertaDeErro, Carregando, classeDoBotaoPrincipal, classeDoCampo } from '../../components/formulario'
import { formatarFaixa, normalizarParaBusca } from '../../lib/formatacao'

export function ServicosPage() {
  // useQuery: busca na API, guarda em cache e devolve o estado (carregando, erro, dados).
  const servicos = useQuery({ queryKey: chavesCatalogo.servicos, queryFn: apiCatalogo.listarServicos })
  const categorias = useQuery({ queryKey: chavesCatalogo.categorias, queryFn: apiCatalogo.listarCategorias })

  const [busca, setBusca] = useState('')
  const [categoriaId, setCategoriaId] = useState<string | null>(null)
  const [mostrarInativos, setMostrarInativos] = useState(false)

  // Filtra no próprio aparelho: com dezenas de serviços, é instantâneo e não depende da internet.
  // useMemo: só refaz o filtro quando um destes valores muda, não a cada renderização.
  const grupos = useMemo(() => {
    const termo = normalizarParaBusca(busca.trim())
    const filtrados = (servicos.data ?? []).filter(
      (s) =>
        (mostrarInativos || s.ativo) &&
        (!categoriaId || s.categoriaId === categoriaId) &&
        normalizarParaBusca(s.nome).includes(termo),
    )
    // A API já devolve ordenado por categoria e nome; aqui só agrupamos.
    const porCategoria = new Map<string, Servico[]>()
    for (const servico of filtrados) {
      porCategoria.set(servico.categoriaNome, [...(porCategoria.get(servico.categoriaNome) ?? []), servico])
    }
    return [...porCategoria]
  }, [servicos.data, busca, categoriaId, mostrarInativos])

  const semCategorias = categorias.data?.length === 0

  return (
    <section>
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">Serviços</h1>
        <div className="flex gap-2">
          <Link
            to="/servicos/categorias"
            className="flex h-11 items-center gap-2 rounded-lg border border-slate-300 bg-white px-3 text-sm font-medium text-slate-700 hover:bg-slate-100"
          >
            <Tags className="size-4" aria-hidden />
            Categorias
          </Link>
          {!semCategorias && (
            <Link to="/servicos/novo" className={`${classeDoBotaoPrincipal} h-11 px-3 text-sm`}>
              <Plus className="size-5" aria-hidden />
              Novo
            </Link>
          )}
        </div>
      </div>

      {semCategorias ? (
        <div className="mt-6 rounded-xl border border-dashed border-slate-300 bg-white px-6 py-10 text-center">
          <p className="text-slate-600">Para cadastrar serviços, crie primeiro uma categoria (ex.: "Polimento e proteção").</p>
          <Link to="/servicos/categorias" className={`${classeDoBotaoPrincipal} mx-auto mt-4 w-fit`}>
            Criar categoria
          </Link>
        </div>
      ) : (
        <>
          <div className="relative mt-4">
            <Search className="pointer-events-none absolute top-1/2 left-3 size-5 -translate-y-1/2 text-slate-400" aria-hidden />
            <input
              type="search"
              placeholder="Buscar serviço"
              aria-label="Buscar serviço"
              value={busca}
              onChange={(e) => setBusca(e.target.value)}
              className={`${classeDoCampo} mt-0 pl-10`}
            />
          </div>

          {/* Filtro por categoria: rola na horizontal no celular, em vez de quebrar em várias linhas */}
          <div className="-mx-4 mt-3 flex gap-2 overflow-x-auto px-4 pb-1">
            {[{ id: null, nome: 'Todas' }, ...(categorias.data ?? [])].map((c) => (
              <button
                key={c.id ?? 'todas'}
                onClick={() => setCategoriaId(c.id)}
                aria-pressed={categoriaId === c.id}
                className={`h-9 shrink-0 rounded-full border px-4 text-sm font-medium ${
                  categoriaId === c.id
                    ? 'border-grafite-900 bg-grafite-900 text-marca-100'
                    : 'border-slate-300 bg-white text-slate-700'
                }`}
              >
                {c.nome}
              </button>
            ))}
          </div>

          <label className="mt-3 flex w-fit items-center gap-2 text-sm text-slate-600">
            <input
              type="checkbox"
              checked={mostrarInativos}
              onChange={(e) => setMostrarInativos(e.target.checked)}
              className="size-4 accent-marca-600"
            />
            Mostrar desativados
          </label>

          {servicos.isPending ? (
            <Carregando />
          ) : servicos.isError ? (
            <div className="mt-6">
              <AlertaDeErro erro={servicos.error} />
            </div>
          ) : grupos.length === 0 ? (
            <p className="mt-8 text-center text-slate-500">
              {servicos.data.length === 0 ? 'Nenhum serviço cadastrado ainda.' : 'Nenhum serviço encontrado.'}
            </p>
          ) : (
            grupos.map(([categoria, itens]) => (
              <div key={categoria} className="mt-6">
                <h2 className="text-sm font-semibold tracking-wide text-slate-500 uppercase">{categoria}</h2>
                <ul className="mt-2 divide-y divide-slate-100 overflow-hidden rounded-xl border border-slate-200 bg-white">
                  {itens.map((s) => (
                    <li key={s.id}>
                      <Link to={`/servicos/${s.id}`} className="flex items-center gap-3 px-4 py-3 hover:bg-slate-50 active:bg-slate-100">
                        <div className="min-w-0 flex-1">
                          <p className={`font-medium ${s.ativo ? '' : 'text-slate-400 line-through'}`}>{s.nome}</p>
                          <p className="text-sm text-slate-500">
                            {formatarFaixa(s.precoMinimo, s.precoMaximo)}
                          </p>
                        </div>
                        {!s.ativo && (
                          <span className="shrink-0 rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-500">Desativado</span>
                        )}
                        <ChevronRight className="size-5 shrink-0 text-slate-400" aria-hidden />
                      </Link>
                    </li>
                  ))}
                </ul>
              </div>
            ))
          )}
        </>
      )}
    </section>
  )
}
