import { LogOut } from 'lucide-react'
import { NavLink, Outlet } from 'react-router'
import { useAuth } from '../auth/contexto'
import { itensDeNavegacao } from './navegacao'

/**
 * Casca do app: envolve todas as páginas.
 * - Celular (< 768px): barra superior com o nome do app + barra de navegação fixa embaixo.
 * - Computador (md: >= 768px): menu lateral fixo à esquerda.
 * O conteúdo de cada página aparece no <Outlet />.
 */
export function AppLayout() {
  const { usuario, sair } = useAuth()

  return (
    <div className="min-h-dvh md:flex">
      {/* Menu lateral: só aparece a partir de md (computador/tablet) */}
      <aside className="hidden md:flex md:w-60 md:shrink-0 md:flex-col md:border-r md:border-slate-200 md:bg-white">
        <div className="flex items-center gap-3 px-5 py-5">
          <img src="/icons/logo-256.png" alt="" width={40} height={40} className="size-10" />
          <span className="text-lg font-bold text-grafite-900">Orçamentos</span>
        </div>
        <nav className="flex flex-col gap-1 px-3">
          {itensDeNavegacao.map(({ rota, rotulo, icone: Icone }) => (
            <NavLink
              key={rota}
              to={rota}
              end={rota === '/'}
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors ${
                  isActive ? 'bg-marca-50 text-marca-700' : 'text-slate-600 hover:bg-slate-100'
                }`
              }
            >
              <Icone className="size-5" aria-hidden />
              {rotulo}
            </NavLink>
          ))}
        </nav>

        {/* mt-auto: empurra este bloco para o fim do menu lateral */}
        <div className="mt-auto border-t border-slate-200 p-4">
          <p className="truncate text-sm font-medium">{usuario?.nome}</p>
          <p className="truncate text-xs text-slate-500">{usuario?.email}</p>
          <button
            onClick={sair}
            className="mt-3 flex items-center gap-2 text-sm font-medium text-slate-600 hover:text-red-600"
          >
            <LogOut className="size-4" aria-hidden />
            Sair
          </button>
          <p className="mt-3 text-xs text-slate-500">Versão {__VERSAO_DO_APP__}</p>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        {/* Barra superior: só no celular */}
        <header className="sticky top-0 z-10 bg-grafite-900 px-4 pt-[env(safe-area-inset-top)] md:hidden">
          <div className="flex h-14 items-center justify-between">
            <div className="flex items-center gap-2">
              <img src="/icons/logo-256.png" alt="" width={32} height={32} className="size-8" />
              <span className="text-lg font-bold text-marca-400">Orçamentos</span>
            </div>
            <button
              onClick={sair}
              aria-label="Sair"
              className="-mr-2 flex size-11 items-center justify-center rounded-lg text-marca-100 active:bg-grafite-800"
            >
              <LogOut className="size-5" aria-hidden />
            </button>
          </div>
        </header>

        {/* pb-24 no celular: espaço para o conteúdo não ficar escondido atrás da barra inferior */}
        <main className="mx-auto w-full max-w-5xl flex-1 px-4 pt-4 pb-24 md:px-8 md:py-8">
          <Outlet />
        </main>
      </div>

      {/* Barra de navegação inferior: só no celular, ao alcance do polegar */}
      <nav className="fixed inset-x-0 bottom-0 z-10 border-t border-slate-200 bg-white pb-[env(safe-area-inset-bottom)] md:hidden">
        <ul className="grid grid-cols-4">
          {itensDeNavegacao.map(({ rota, rotulo, icone: Icone }) => (
            <li key={rota}>
              <NavLink
                to={rota}
                end={rota === '/'}
                className={({ isActive }) =>
                  `flex h-16 flex-col items-center justify-center gap-1 text-xs font-medium ${
                    isActive ? 'text-marca-700' : 'text-slate-500'
                  }`
                }
              >
                <Icone className="size-6" aria-hidden />
                {rotulo}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
    </div>
  )
}
