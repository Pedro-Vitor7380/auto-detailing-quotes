import { Eye, EyeOff, LoaderCircle } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router'
import { ErroDaApi } from '../api/cliente'
import { useAuth } from '../auth/contexto'

export function LoginPage() {
  const { usuario, entrar } = useAuth()
  const navegar = useNavigate()
  const voltarPara = (useLocation().state as { voltarPara?: string } | null)?.voltarPara ?? '/'

  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [mostrarSenha, setMostrarSenha] = useState(false)
  const [enviando, setEnviando] = useState(false)
  const [erro, setErro] = useState<string | null>(null)

  // Já logado (ex.: abriu /login de novo): vai direto para o app.
  if (usuario) return <Navigate to={voltarPara} replace />

  async function aoEnviar(evento: FormEvent) {
    evento.preventDefault() // impede o navegador de recarregar a página ao enviar o formulário
    setErro(null)
    setEnviando(true)
    try {
      await entrar(email, senha)
      navegar(voltarPara, { replace: true })
    } catch (e) {
      setErro(e instanceof ErroDaApi ? e.message : 'Erro inesperado. Tente novamente.')
    } finally {
      setEnviando(false)
    }
  }

  return (
    <main className="flex min-h-dvh items-center justify-center px-4 py-8">
      <div className="w-full max-w-sm">
        <img src="/icons/logo-256.png" alt="CJ Estética Automotiva" width={128} height={128} className="mx-auto size-32" />
        <h1 className="mt-4 text-center text-2xl font-bold text-grafite-900">Orçamentos</h1>
        <p className="mt-1 text-center text-slate-500">Entre para continuar</p>

        <form onSubmit={aoEnviar} className="mt-8 space-y-4 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
          <div>
            <label htmlFor="email" className="block text-sm font-medium text-slate-700">
              E-mail
            </label>
            {/* text-base (16px): abaixo disso, o iPhone dá zoom automático ao tocar no campo */}
            <input
              id="email"
              type="email"
              inputMode="email"
              autoComplete="email"
              autoCapitalize="none"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="mt-1 h-12 w-full rounded-lg border border-slate-300 px-3 text-base outline-none focus:border-marca-600 focus:ring-2 focus:ring-marca-100"
            />
          </div>

          <div>
            <label htmlFor="senha" className="block text-sm font-medium text-slate-700">
              Senha
            </label>
            <div className="relative mt-1">
              <input
                id="senha"
                type={mostrarSenha ? 'text' : 'password'}
                autoComplete="current-password"
                required
                value={senha}
                onChange={(e) => setSenha(e.target.value)}
                className="h-12 w-full rounded-lg border border-slate-300 pr-12 pl-3 text-base outline-none focus:border-marca-600 focus:ring-2 focus:ring-marca-100"
              />
              <button
                type="button"
                onClick={() => setMostrarSenha((atual) => !atual)}
                aria-label={mostrarSenha ? 'Esconder senha' : 'Mostrar senha'}
                className="absolute inset-y-0 right-0 flex w-12 items-center justify-center text-slate-500"
              >
                {mostrarSenha ? <EyeOff className="size-5" /> : <Eye className="size-5" />}
              </button>
            </div>
          </div>

          {/* role="alert": leitores de tela anunciam o erro assim que ele aparece */}
          {erro && (
            <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">
              {erro}
            </p>
          )}

          <button
            type="submit"
            disabled={enviando}
            className="flex h-12 w-full items-center justify-center gap-2 rounded-lg bg-marca-400 font-semibold text-grafite-900 hover:bg-marca-500 disabled:opacity-60"
          >
            {enviando && <LoaderCircle className="size-5 animate-spin" aria-hidden />}
            {enviando ? 'Entrando...' : 'Entrar'}
          </button>
        </form>
      </div>
    </main>
  )
}
