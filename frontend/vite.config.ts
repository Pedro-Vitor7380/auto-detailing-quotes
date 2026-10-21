import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { execSync } from 'node:child_process'
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

// Versão exibida no app: o commit do build + a data. Serve para conferir se o celular já pegou a versão nova.
// Na Vercel, o commit vem da variável VERCEL_GIT_COMMIT_SHA; no PC, do próprio Git.
function versaoDoBuild() {
  let commit = process.env.VERCEL_GIT_COMMIT_SHA
  try {
    commit ??= execSync('git rev-parse HEAD', { stdio: ['ignore', 'pipe', 'ignore'] }).toString().trim()
  } catch {
    commit = 'local'
  }
  const data = new Date().toLocaleString('pt-BR', { timeZone: 'America/Sao_Paulo', dateStyle: 'short', timeStyle: 'short' })
  return `${commit.slice(0, 7)} · ${data}`
}

// https://vite.dev/config/
export default defineConfig({
  // define: o Vite troca __VERSAO_DO_APP__ pelo texto abaixo no código final (como um "localizar e substituir").
  define: { __VERSAO_DO_APP__: JSON.stringify(versaoDoBuild()) },
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
        // Acrescenta ao service worker o código que recebe e mostra as notificações (public/notificacoes-sw.js).
        importScripts: ['notificacoes-sw.js'],
      },
    }),
  ],
  server: servidor,
  preview: servidor,
})
