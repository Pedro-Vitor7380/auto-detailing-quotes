import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider } from 'react-router'
import { AuthProvider } from './auth/AuthProvider'
import './index.css'
import { rotas } from './rotas'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {/* AuthProvider por fora: qualquer página (inclusive o login) sabe quem está logado */}
    <AuthProvider>
      <RouterProvider router={rotas} />
    </AuthProvider>
  </StrictMode>,
)
