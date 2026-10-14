// Tipos das variáveis de ambiente do Vite (as que começam com VITE_ ficam visíveis no navegador).
interface ImportMetaEnv {
  readonly VITE_API_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
