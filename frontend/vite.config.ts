import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'
import { VitePWA } from 'vite-plugin-pwa'

// Configuração de rede compartilhada entre "npm run dev" e "npm run preview".
const servidor = {
  port: 5173,
  strictPort: true, // se a porta estiver ocupada, dá erro em vez de usar outra (o celular espera a 5173)
  host: true, // aceita conexões da rede local, não só do próprio PC: permite testar no celular
  // Repassa /api/... para a API rodando NO PC. Assim o celular chama o Vite (no PC), e não
  // "localhost:5080" (que, no celular, seria o próprio celular).
  proxy: {
    '/api': 'http://localhost:5080',
  },
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    VitePWA({
      registerType: 'autoUpdate', // nova versão publicada = o app se atualiza sozinho na próxima abertura
      includeAssets: ['icons/favicon-64.png', 'icons/apple-touch-icon.png'],
      // Manifesto: o "RG" do app instalado (nome, ícone, cores, como abre).
      manifest: {
        name: 'CJ Estética Automotiva — Orçamentos',
        short_name: 'Orçamentos',
        description: 'Criação e envio de orçamentos da CJ Estética Automotiva.',
        lang: 'pt-BR',
        start_url: '/',
        scope: '/',
        display: 'standalone', // abre em tela cheia, sem a barra de endereço do navegador
        orientation: 'portrait',
        theme_color: '#373334',
        background_color: '#373334',
        icons: [
          { src: '/icons/icon-192.png', sizes: '192x192', type: 'image/png', purpose: 'any' },
          { src: '/icons/icon-512.png', sizes: '512x512', type: 'image/png', purpose: 'any' },
          { src: '/icons/icon-maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      // Service worker (Workbox): guarda os arquivos do app no celular para abrir rápido.
      workbox: {
        globPatterns: ['**/*.{js,css,html,png,svg}'],
        // Navegação para qualquer rota (/clientes, /orcamentos...) entrega o index.html do cache...
        navigateFallback: '/index.html',
        // ...exceto a API: dados sempre vêm do servidor, nunca do cache.
        navigateFallbackDenylist: [/^\/api\//],
      },
    }),
  ],
  server: servidor,
  preview: servidor,
})
