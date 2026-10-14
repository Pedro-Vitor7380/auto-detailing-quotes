import { Construction } from 'lucide-react'

interface EmConstrucaoProps {
  titulo: string
  previsao: string
}

/** Página provisória, enquanto a funcionalidade real não é construída. */
export function EmConstrucao({ titulo, previsao }: EmConstrucaoProps) {
  return (
    <section>
      <h1 className="text-2xl font-bold">{titulo}</h1>
      <div className="mt-6 flex flex-col items-center gap-3 rounded-xl border border-dashed border-slate-300 bg-white px-6 py-12 text-center text-slate-500">
        <Construction className="size-10" aria-hidden />
        <p>Em construção: previsto para a {previsao}.</p>
      </div>
    </section>
  )
}
