import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Car, ChevronRight, Plus, Search } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { apiClientes, chavesClientes } from '../../api/clientes'
import { AlertaDeErro, Carregando, classeDoBotaoPrincipal, classeDoCampo } from '../../components/formulario'
import { formatarPlaca, formatarWhatsApp } from '../../lib/formatacao'
import { useValorAtrasado } from '../../lib/useValorAtrasado'

// O mesmo limite da API (ClientesEndpoints.LimiteDaBusca).
const LIMITE_DA_BUSCA = 50

export function ClientesPage() {
  const [busca, setBusca] = useState('')
  const termo = useValorAtrasado(busca.trim())

  // Diferente dos serviços, a busca roda no servidor: a lista de clientes cresce sem parar.
  const clientes = useQuery({
    queryKey: chavesClientes.busca(termo),
    queryFn: () => apiClientes.buscar(termo),
    // Enquanto a busca nova não chega, continua mostrando o resultado anterior (a lista não "pisca").
    placeholderData: keepPreviousData,
  })

  return (
    <section>
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">Clientes</h1>
        <Link to="/clientes/novo" className={`${classeDoBotaoPrincipal} h-11 px-3 text-sm`}>
          <Plus className="size-5" aria-hidden />
          Novo
        </Link>
      </div>

      <div className="relative mt-4">
        <Search className="pointer-events-none absolute top-1/2 left-3 size-5 -translate-y-1/2 text-slate-400" aria-hidden />
        <input
          type="search"
          placeholder="Nome, placa ou WhatsApp"
          aria-label="Buscar cliente por nome, placa ou WhatsApp"
          value={busca}
          onChange={(e) => setBusca(e.target.value)}
          className={`${classeDoCampo} mt-0 pl-10`}
        />
      </div>

      {clientes.isPending ? (
        <Carregando />
      ) : clientes.isError ? (
        <div className="mt-6">
          <AlertaDeErro erro={clientes.error} />
        </div>
      ) : clientes.data.length === 0 ? (
        <p className="mt-8 text-center text-slate-500">
          {termo ? 'Nenhum cliente encontrado.' : 'Nenhum cliente cadastrado ainda.'}
        </p>
      ) : (
        // Fica um pouco transparente enquanto uma busca nova está a caminho.
        <ul
          className={`mt-4 divide-y divide-slate-100 overflow-hidden rounded-xl border border-slate-200 bg-white transition-opacity ${
            clientes.isPlaceholderData ? 'opacity-60' : ''
          }`}
        >
          {clientes.data.map((c) => (
            <li key={c.id}>
              <Link to={`/clientes/${c.id}`} className="flex items-center gap-3 px-4 py-3 hover:bg-slate-50 active:bg-slate-100">
                <div className="min-w-0 flex-1">
                  <p className="truncate font-medium">{c.nome}</p>
                  <p className="text-sm text-slate-500">{formatarWhatsApp(c.whatsApp)}</p>
                  {c.veiculos.length > 0 && (
                    <div className="mt-1 flex flex-wrap gap-1">
                      {c.veiculos.map((v) => (
                        <span key={v.id} className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-2 py-0.5 text-xs text-slate-600">
                          <Car className="size-3.5" aria-hidden />
                          <span className="font-mono font-semibold">{formatarPlaca(v.placa)}</span> {v.modelo}
                        </span>
                      ))}
                    </div>
                  )}
                </div>
                <ChevronRight className="size-5 shrink-0 text-slate-400" aria-hidden />
              </Link>
            </li>
          ))}
        </ul>
      )}

      {clientes.data?.length === LIMITE_DA_BUSCA && (
        <p className="mt-3 text-center text-sm text-slate-500">
          Mostrando os {LIMITE_DA_BUSCA} primeiros. Digite mais para encontrar quem procura.
        </p>
      )}
    </section>
  )
}
