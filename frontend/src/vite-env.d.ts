// Tipos das variáveis de ambiente do Vite (as que começam com VITE_ ficam visíveis no navegador).
interface ImportMetaEnv {
  readonly VITE_API_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

/** Commit e data do build (definido em vite.config.ts). */
declare const __VERSAO_DO_APP__: string
