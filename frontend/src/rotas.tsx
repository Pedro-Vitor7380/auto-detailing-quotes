import { createBrowserRouter } from 'react-router'
import { RotaProtegida } from './auth/RotaProtegida'
import { AppLayout } from './layouts/AppLayout'
import { InicioPage } from './pages/InicioPage'
import { LoginPage } from './pages/LoginPage'
import { ClientePage } from './pages/clientes/ClientePage'
import { ClientesPage } from './pages/clientes/ClientesPage'
import { NovoClientePage } from './pages/clientes/NovoClientePage'
import { NaoEncontradoPage } from './pages/NaoEncontradoPage'
import { OrcamentoPage } from './pages/orcamentos/OrcamentoPage'
import { OrcamentosPage } from './pages/orcamentos/OrcamentosPage'
import { CategoriasPage } from './pages/servicos/CategoriasPage'
import { ServicoFormPage } from './pages/servicos/ServicoFormPage'
import { ServicosPage } from './pages/servicos/ServicosPage'

export const rotas = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    // Tudo aqui dentro exige login. Sem login, RotaProtegida redireciona para /login.
    element: <RotaProtegida />,
    children: [
      {
        // Cada rota filha aparece dentro do <Outlet /> do AppLayout (a casca com o menu).
        path: '/',
        element: <AppLayout />,
        children: [
          { index: true, element: <InicioPage /> },
          { path: 'orcamentos', element: <OrcamentosPage /> },
          { path: 'orcamentos/novo', element: <OrcamentoPage /> },
          { path: 'orcamentos/:id', element: <OrcamentoPage /> },
          { path: 'clientes', element: <ClientesPage /> },
          { path: 'clientes/novo', element: <NovoClientePage /> },
          { path: 'clientes/:id', element: <ClientePage /> },
          { path: 'servicos', element: <ServicosPage /> },
          // "categorias" e "novo" vêm antes de ":id": o React Router escolhe a rota mais específica.
          { path: 'servicos/categorias', element: <CategoriasPage /> },
          { path: 'servicos/novo', element: <ServicoFormPage /> },
          { path: 'servicos/:id', element: <ServicoFormPage /> },
          { path: '*', element: <NaoEncontradoPage /> },
        ],
      },
    ],
  },
])
