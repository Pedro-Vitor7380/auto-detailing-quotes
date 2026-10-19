import { QueryClientProvider } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider } from 'react-router'
import { queryClient } from './api/queryClient'
import { AuthProvider } from './auth/AuthProvider'
import './index.css'
import { rotas } from './rotas'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {/* QueryClientProvider: o cache das consultas à API. AuthProvider: qualquer página sabe quem está logado */}
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RouterProvider router={rotas} />
      </AuthProvider>
    </QueryClientProvider>
  </StrictMode>,
)
