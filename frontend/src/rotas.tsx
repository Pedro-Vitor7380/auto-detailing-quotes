import { createBrowserRouter } from 'react-router'
import { RotaProtegida } from './auth/RotaProtegida'
import { EmConstrucao } from './components/EmConstrucao'
import { AppLayout } from './layouts/AppLayout'
import { InicioPage } from './pages/InicioPage'
import { LoginPage } from './pages/LoginPage'
import { NaoEncontradoPage } from './pages/NaoEncontradoPage'

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
          { path: 'servicos', element: <EmConstrucao titulo="Serviços" previsao="semana 2" /> },
          { path: '*', element: <NaoEncontradoPage /> },
        ],
      },
    ],
  },
])
