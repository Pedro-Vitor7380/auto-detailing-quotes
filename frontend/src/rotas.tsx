import { createBrowserRouter } from 'react-router'
import { RotaProtegida } from './auth/RotaProtegida'
import { EmConstrucao } from './components/EmConstrucao'
import { AppLayout } from './layouts/AppLayout'
import { InicioPage } from './pages/InicioPage'
import { LoginPage } from './pages/LoginPage'
import { NaoEncontradoPage } from './pages/NaoEncontradoPage'
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
          { path: 'orcamentos', element: <EmConstrucao titulo="Orçamentos" previsao="semana 3" /> },
          { path: 'orcamentos/novo', element: <EmConstrucao titulo="Novo orçamento" previsao="semana 3" /> },
          { path: 'clientes', element: <EmConstrucao titulo="Clientes" previsao="semana 2" /> },
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
