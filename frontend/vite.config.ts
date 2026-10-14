import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    strictPort: true, // se a porta estiver ocupada, dá erro em vez de usar outra (o celular espera a 5173)
    host: true, // aceita conexões da rede local, não só do próprio PC: permite testar no celular
  },
})
