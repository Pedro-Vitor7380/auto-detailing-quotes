import type { StatusDoOrcamento } from '../api/orcamentos'

// Cor por status. Texto escuro sobre fundo claro: contraste acima de 4,5:1 em todos.
const estilos: Record<StatusDoOrcamento, { rotulo: string; classe: string }> = {
  Rascunho: { rotulo: 'Rascunho', classe: 'bg-slate-100 text-slate-700' },
  Enviado: { rotulo: 'Enviado', classe: 'bg-blue-100 text-blue-800' },
  Aprovado: { rotulo: 'Aprovado', classe: 'bg-green-100 text-green-800' },
  Recusado: { rotulo: 'Recusado', classe: 'bg-red-100 text-red-800' },
  Expirado: { rotulo: 'Expirado', classe: 'bg-amber-100 text-amber-900' },
}

export function SeloDeStatus({ status }: { status: StatusDoOrcamento }) {
  const { rotulo, classe } = estilos[status]
  return <span className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-semibold ${classe}`}>{rotulo}</span>
}
