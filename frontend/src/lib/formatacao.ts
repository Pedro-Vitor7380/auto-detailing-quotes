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

/** Para buscas: "Higienização" e "higienizacao" viram o mesmo texto. */
export function normalizarParaBusca(texto: string): string {
  return texto.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase()
}

/** "5511999998888" → "(11) 99999-8888"; "552133334444" → "(21) 3333-4444" */
export function formatarWhatsApp(numero: string): string {
  const local = numero.startsWith('55') ? numero.slice(2) : numero
  const ddd = local.slice(0, 2)
  const resto = local.slice(2)
  const meio = resto.length - 4
  return `(${ddd}) ${resto.slice(0, meio)}-${resto.slice(meio)}`
}

/** Placa antiga com hífen ("ABC-1234"), Mercosul sem ("ABC1D23"), como aparecem nas placas reais. */
export function formatarPlaca(placa: string): string {
  return /^[A-Z]{3}\d{4}$/.test(placa) ? `${placa.slice(0, 3)}-${placa.slice(3)}` : placa
}

/** "2026-10-26" → "26/10/2026". Lê o texto direto: new Date("2026-10-26") seria meia-noite em UTC, e no Brasil viraria o dia 25. */
export function formatarData(iso: string): string {
  const [ano, mes, dia] = iso.slice(0, 10).split('-')
  return `${dia}/${mes}/${ano}`
}

/** Data de hoje no aparelho, no formato "2026-10-19" (o mesmo do campo <input type="date">). */
export function hojeIso(): string {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

/** Soma dias a uma data "2026-10-19". Usa meio-dia para não cair em problemas de horário de verão. */
export function somarDias(iso: string, dias: number): string {
  const d = new Date(`${iso}T12:00:00`)
  d.setDate(d.getDate() + dias)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}
