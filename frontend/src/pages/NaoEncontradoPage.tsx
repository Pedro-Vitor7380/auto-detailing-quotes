import { Link } from 'react-router'

export function NaoEncontradoPage() {
  return (
    <section className="py-12 text-center">
      <h1 className="text-2xl font-bold">Página não encontrada</h1>
      <p className="mt-2 text-slate-500">O endereço acessado não existe.</p>
      <Link to="/" className="mt-6 inline-block font-medium text-marca-700 underline">
        Voltar ao início
      </Link>
    </section>
  )
}
