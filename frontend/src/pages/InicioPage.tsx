import { Plus } from 'lucide-react'
import { Link } from 'react-router'

export function InicioPage() {
  return (
    <section>
      <h1 className="text-2xl font-bold">Início</h1>
      <p className="mt-1 text-slate-500">Bem-vindo! O que vamos orçar hoje?</p>

      {/* Ação principal do app: grande e fácil de tocar no celular */}
      <Link
        to="/orcamentos/novo"
        className="mt-6 flex h-14 w-full items-center justify-center gap-2 rounded-xl bg-marca-600 text-lg font-semibold text-white shadow-sm hover:bg-marca-700 active:scale-[0.98] sm:inline-flex sm:w-auto sm:px-8"
      >
        <Plus className="size-6" aria-hidden />
        Novo orçamento
      </Link>

      <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {['Orçamentos em aberto', 'Aprovados no mês', 'Faturamento previsto'].map((titulo) => (
          <div key={titulo} className="rounded-xl border border-slate-200 bg-white p-5">
            <p className="text-sm text-slate-500">{titulo}</p>
            <p className="mt-1 text-2xl font-bold text-slate-300">—</p>
          </div>
        ))}
      </div>
    </section>
  )
}
