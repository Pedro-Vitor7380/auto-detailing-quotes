import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Check, Pencil, Plus, Trash2, X } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { apiCatalogo, chavesCatalogo, type Categoria } from '../../api/catalogo'
import { AlertaDeErro, Carregando, classeDoBotaoPrincipal, classeDoCampo } from '../../components/formulario'

export function CategoriasPage() {
  const queryClient = useQueryClient()
  const categorias = useQuery({ queryKey: chavesCatalogo.categorias, queryFn: apiCatalogo.listarCategorias })
  const [nome, setNome] = useState('')

  const atualizarListas = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: chavesCatalogo.categorias }),
      queryClient.invalidateQueries({ queryKey: chavesCatalogo.servicos }), // o nome da categoria aparece nos serviços
    ])

  const criar = useMutation({
    mutationFn: apiCatalogo.criarCategoria,
    onSuccess: async () => {
      setNome('')
      await atualizarListas()
    },
  })

  function aoCriar(evento: FormEvent) {
    evento.preventDefault()
    if (nome.trim()) criar.mutate(nome)
  }

  return (
    <section className="mx-auto max-w-xl">
      <Link to="/servicos" className="-ml-1 inline-flex h-11 items-center gap-1 text-sm font-medium text-slate-600">
        <ArrowLeft className="size-4" aria-hidden />
        Serviços
      </Link>
      <h1 className="text-2xl font-bold">Categorias</h1>

      <form onSubmit={aoCriar} className="mt-4 flex gap-2">
        <input
          value={nome}
          onChange={(e) => setNome(e.target.value)}
          placeholder="Nova categoria"
          aria-label="Nome da nova categoria"
          maxLength={80}
          className={`${classeDoCampo} mt-0`}
        />
        <button type="submit" disabled={criar.isPending || !nome.trim()} className={`${classeDoBotaoPrincipal} shrink-0 px-4`}>
          <Plus className="size-5" aria-hidden />
          <span className="sr-only sm:not-sr-only">Adicionar</span>
        </button>
      </form>
      <div className="mt-2">
        <AlertaDeErro erro={criar.error} />
      </div>

      {categorias.isPending ? (
        <Carregando />
      ) : categorias.isError ? (
        <div className="mt-6">
          <AlertaDeErro erro={categorias.error} />
        </div>
      ) : categorias.data.length === 0 ? (
        <p className="mt-8 text-center text-slate-500">Nenhuma categoria ainda.</p>
      ) : (
        <ul className="mt-4 divide-y divide-slate-100 overflow-hidden rounded-xl border border-slate-200 bg-white">
          {categorias.data.map((c) => (
            <LinhaDaCategoria key={c.id} categoria={c} aoMudar={atualizarListas} />
          ))}
        </ul>
      )}
    </section>
  )
}

/** Uma linha da lista. Cada linha tem o seu próprio estado de edição e as suas mutações. */
function LinhaDaCategoria({ categoria, aoMudar }: { categoria: Categoria; aoMudar: () => Promise<unknown> }) {
  const [editando, setEditando] = useState(false)
  const [nome, setNome] = useState(categoria.nome)

  const renomear = useMutation({
    mutationFn: () => apiCatalogo.renomearCategoria(categoria.id, nome),
    onSuccess: async () => {
      setEditando(false)
      await aoMudar()
    },
  })
  const excluir = useMutation({
    mutationFn: () => apiCatalogo.excluirCategoria(categoria.id, categoria.quantidadeDeServicos > 0),
    onSuccess: aoMudar,
  })

  const temServicos = categoria.quantidadeDeServicos > 0
  const botaoIcone = 'flex size-11 shrink-0 items-center justify-center rounded-lg text-slate-500 hover:bg-slate-100 disabled:opacity-40'

  return (
    <li className="px-4 py-2">
      {editando ? (
        <form
          onSubmit={(e) => {
            e.preventDefault()
            renomear.mutate()
          }}
          className="flex items-center gap-1"
        >
          <input
            value={nome}
            onChange={(e) => setNome(e.target.value)}
            aria-label="Novo nome da categoria"
            maxLength={80}
            autoFocus
            className={`${classeDoCampo} mt-0 h-11`}
          />
          <button type="submit" disabled={renomear.isPending} aria-label="Salvar nome" className={botaoIcone}>
            <Check className="size-5 text-green-700" aria-hidden />
          </button>
          <button
            type="button"
            onClick={() => {
              setEditando(false)
              setNome(categoria.nome)
              renomear.reset()
            }}
            aria-label="Cancelar"
            className={botaoIcone}
          >
            <X className="size-5" aria-hidden />
          </button>
        </form>
      ) : (
        <div className="flex items-center gap-1">
          <div className="min-w-0 flex-1">
            <p className="truncate font-medium">{categoria.nome}</p>
            <p className="text-sm text-slate-500">
              {categoria.quantidadeDeServicos} {categoria.quantidadeDeServicos === 1 ? 'serviço' : 'serviços'}
            </p>
          </div>
          <button onClick={() => setEditando(true)} aria-label={`Renomear ${categoria.nome}`} className={botaoIcone}>
            <Pencil className="size-5" aria-hidden />
          </button>
          <button
            onClick={() => {
              // Antes, o botão ficava desabilitado quando havia serviços, sem explicar nada (no celular não há
              // "passar o mouse" para ver a dica): parecia quebrado. Agora a confirmação diz o que vai acontecer.
              const pergunta = temServicos
                ? `Excluir a categoria "${categoria.nome}" e os seus ${categoria.quantidadeDeServicos} serviço(s)?

Serviços que já estão em orçamentos não podem ser excluídos.`
                : `Excluir a categoria "${categoria.nome}"?`
              if (window.confirm(pergunta)) excluir.mutate()
            }}
            disabled={excluir.isPending}
            aria-label={`Excluir ${categoria.nome}`}
            className={botaoIcone}
          >
            <Trash2 className="size-5" aria-hidden />
          </button>
        </div>
      )}
      <AlertaDeErro erro={renomear.error ?? excluir.error} />
    </li>
  )
}
