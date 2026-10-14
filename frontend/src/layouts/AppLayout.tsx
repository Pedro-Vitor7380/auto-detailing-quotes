import { NavLink, Outlet } from 'react-router'
import { itensDeNavegacao } from './navegacao'

/**
 * Casca do app: envolve todas as páginas.
 * - Celular (< 768px): barra superior com o nome do app + barra de navegação fixa embaixo.
 * - Computador (md: >= 768px): menu lateral fixo à esquerda.
 * O conteúdo de cada página aparece no <Outlet />.
 */
export function AppLayout() {
  return (
    <div className="min-h-dvh md:flex">
      {/* Menu lateral: só aparece a partir de md (computador/tablet) */}
      <aside className="hidden md:flex md:w-60 md:shrink-0 md:flex-col md:border-r md:border-slate-200 md:bg-white">
        <div className="px-6 py-5 text-lg font-bold text-marca-700">Orçamentos</div>
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
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        {/* Barra superior: só no celular */}
        <header className="sticky top-0 z-10 border-b border-slate-200 bg-white/90 px-4 pt-[env(safe-area-inset-top)] backdrop-blur md:hidden">
          <div className="flex h-14 items-center text-lg font-bold text-marca-700">Orçamentos</div>
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
