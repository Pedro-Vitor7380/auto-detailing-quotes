import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { ChevronRight, Plus, Search } from 'lucide-react'
import { Link, useSearchParams } from 'react-router'
import { apiOrcamentos, chavesOrcamentos, type StatusDoOrcamento } from '../../api/orcamentos'
import { AlertaDeErro, Carregando, classeDoBotaoPrincipal, classeDoCampo } from '../../components/formulario'
import { SeloDeStatus } from '../../components/SeloDeStatus'
import { formatarData, formatarMoeda, formatarPlaca } from '../../lib/formatacao'
import { useValorAtrasado } from '../../lib/useValorAtrasado'

// O mesmo limite da API (OrcamentosEndpoints.LimiteDaLista).
const LIMITE_DA_LISTA = 50
const FILTROS: { status?: StatusDoOrcamento; rotulo: string }[] = [
  { rotulo: 'Todos' },
  { status: 'Rascunho', rotulo: 'Rascunhos' },
  { status: 'Enviado', rotulo: 'Enviados' },
  { status: 'Aprovado', rotulo: 'Aprovados' },
  { status: 'Recusado', rotulo: 'Recusados' },
  { status: 'Expirado', rotulo: 'Expirados' },
]

export function OrcamentosPage() {
  // Filtro e busca ficam na URL (?status=Enviado&busca=joao): ao abrir um orçamento e voltar,
  // a lista volta exatamente como estava. E dá para mandar o link de uma lista filtrada.
  const [parametros, setParametros] = useSearchParams()
  const status = (parametros.get('status') as StatusDoOrcamento | null) ?? undefined
  const busca = parametros.get('busca') ?? ''
  const termo = useValorAtrasado(busca.trim())

  function mudar(chave: 'status' | 'busca', valor?: string) {
    setParametros(
      (atuais) => {
        if (valor) atuais.set(chave, valor)
        else atuais.delete(chave)
        return atuais
      },
      { replace: true }, // digitar na busca não enche o histórico do "voltar"
    )
  }

  const filtros = { status, busca: termo || undefined }
  const orcamentos = useQuery({
    queryKey: chavesOrcamentos.lista(filtros),
    queryFn: () => apiOrcamentos.listar(filtros),
    placeholderData: keepPreviousData,
  })

  return (
    <section>
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">Orçamentos</h1>
        <Link to="/orcamentos/novo" className={`${classeDoBotaoPrincipal} h-11 px-3 text-sm`}>
          <Plus className="size-5" aria-hidden />
          Novo
        </Link>
      </div>

      <div className="relative mt-4">
        <Search className="pointer-events-none absolute top-1/2 left-3 size-5 -translate-y-1/2 text-slate-400" aria-hidden />
        <input
          type="search"
          placeholder="Cliente, placa ou número"
          aria-label="Buscar orçamento por cliente, placa ou número"
          value={busca}
          onChange={(e) => mudar('busca', e.target.value)}
          className={`${classeDoCampo} mt-0 pl-10`}
        />
      </div>

      <div className="-mx-4 mt-3 flex gap-2 overflow-x-auto px-4 pb-1">
        {FILTROS.map((f) => (
          <button
            key={f.rotulo}
            type="button"
            onClick={() => mudar('status', f.status)}
            aria-pressed={status === f.status}
            className={`h-9 shrink-0 rounded-full border px-4 text-sm font-medium ${
              status === f.status ? 'border-grafite-900 bg-grafite-900 text-marca-100' : 'border-slate-300 bg-white text-slate-700'
            }`}
          >
            {f.rotulo}
          </button>
        ))}
      </div>

      {orcamentos.isPending ? (
        <Carregando />
      ) : orcamentos.isError ? (
        <div className="mt-6">
          <AlertaDeErro erro={orcamentos.error} />
        </div>
      ) : orcamentos.data.length === 0 ? (
        <div className="mt-8 text-center text-slate-500">
          {termo || status ? (
            <p>Nenhum orçamento encontrado.</p>
          ) : (
            <>
              <p>Nenhum orçamento ainda.</p>
              <Link to="/orcamentos/novo" className={`${classeDoBotaoPrincipal} mx-auto mt-4 w-fit`}>
                <Plus className="size-5" aria-hidden /> Fazer o primeiro
              </Link>
            </>
          )}
        </div>
      ) : (
        <ul
          className={`mt-4 divide-y divide-slate-100 overflow-hidden rounded-xl border border-slate-200 bg-white transition-opacity ${
            orcamentos.isPlaceholderData ? 'opacity-60' : ''
          }`}
        >
          {orcamentos.data.map((o) => (
            <li key={o.id}>
              <Link to={`/orcamentos/${o.id}`} className="flex items-center gap-3 px-4 py-3 hover:bg-slate-50 active:bg-slate-100">
                {/* 3 linhas, cada uma com a largura toda: no celular de 360 px nada quebra nem é cortado */}
                <div className="min-w-0 flex-1">
                  <div className="flex items-baseline justify-between gap-2">
                    <span className="font-mono text-sm font-semibold">{o.numero}</span>
                    <span className="font-semibold">{formatarMoeda(o.total)}</span>
                  </div>
                  <div className="mt-0.5 flex items-center justify-between gap-2">
                    <p className="min-w-0 truncate font-medium">{o.clienteNome}</p>
                    <SeloDeStatus status={o.status} />
                  </div>
                  <p className="truncate text-sm text-slate-500">
                    <span className="font-mono">{formatarPlaca(o.placa)}</span> {o.modelo} ·{' '}
                    {o.status === 'Enviado' ? `vale até ${formatarData(o.validade)}` : formatarData(o.criadoEm)}
                  </p>
                </div>
                <ChevronRight className="size-5 shrink-0 text-slate-400" aria-hidden />
              </Link>
            </li>
          ))}
        </ul>
      )}

      {orcamentos.data?.length === LIMITE_DA_LISTA && (
        <p className="mt-3 text-center text-sm text-slate-500">
          Mostrando os {LIMITE_DA_LISTA} mais recentes. Use a busca ou os filtros para encontrar os mais antigos.
        </p>
      )}
    </section>
  )
}
