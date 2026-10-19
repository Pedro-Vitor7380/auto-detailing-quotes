// Formatação no padrão brasileiro. Intl é a API de internacionalização nativa do navegador.
const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const decimal = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

/** 1200.5 → "R$ 1.200,50" */
export function formatarMoeda(valor: number): string {
  return moeda.format(valor)
}

/** Faixa de preço; se mínimo e máximo forem iguais, mostra um valor só. */
export function formatarFaixa(minimo: number, maximo: number): string {
  return minimo === maximo ? formatarMoeda(minimo) : `${formatarMoeda(minimo)} a ${formatarMoeda(maximo)}`
}

/** 1200.5 → "1.200,50" (para preencher um campo de valor). */
export function formatarDecimal(valor: number): string {
  return decimal.format(valor)
}

/**
 * Lê um valor digitado em reais: aceita "1.200,50", "1200,5", "R$ 80" e "80.50".
 * Devolve null se não for um número.
 */
export function lerValorEmReais(texto: string): number | null {
  let limpo = texto.replace(/R\$|\s/g, '')
  if (limpo.includes(',')) {
    limpo = limpo.replace(/\./g, '').replace(',', '.') // "1.200,50": ponto é milhar, vírgula é decimal
  } else if (/^\d{1,3}(\.\d{3})+$/.test(limpo)) {
    limpo = limpo.replace(/\./g, '') // "1.200": ponto de milhar, sem centavos
  }
  if (!/^\d+(\.\d+)?$/.test(limpo)) return null
  return Number(limpo)
}

/** 90 → "1 h 30", 45 → "45 min", 120 → "2 h" */
export function formatarDuracao(minutos: number): string {
  const horas = Math.floor(minutos / 60)
  const resto = minutos % 60
  if (horas === 0) return `${resto} min`
  return resto === 0 ? `${horas} h` : `${horas} h ${String(resto).padStart(2, '0')}`
}

/** Para buscas: "Higienização" e "higienizacao" viram o mesmo texto. */
export function normalizarParaBusca(texto: string): string {
  return texto.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase()
}
