// Datas como texto "2026-10-27" (o mesmo formato da API). Trabalhar com o texto evita o problema de
// new Date("2026-10-27") ser meia-noite em UTC, que no Brasil ainda é o dia 26 (passo 19).

const doisDigitos = (n: number) => String(n).padStart(2, '0')

export function isoDe(ano: number, mes: number, dia: number): string {
  return `${ano}-${doisDigitos(mes)}-${doisDigitos(dia)}`
}

/** Primeiro e último dia do mês: o período que o calendário pede à API. */
export function periodoDoMes(ano: number, mes: number): { de: string; ate: string } {
  const ultimoDia = new Date(ano, mes, 0).getDate() // dia 0 do mês seguinte = último dia deste mês
  return { de: isoDe(ano, mes, 1), ate: isoDe(ano, mes, ultimoDia) }
}

/** Dia da semana (0 = domingo) do dia 1º: quantas casas vazias vêm antes, na grade que começa no domingo. */
export function diaDaSemanaDoPrimeiro(ano: number, mes: number): number {
  return new Date(ano, mes - 1, 1).getDay()
}

export function nomeDoMes(ano: number, mes: number): string {
  const nome = new Date(ano, mes - 1, 1).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })
  return nome.charAt(0).toUpperCase() + nome.slice(1)
}

/** "2026-10-27" → "terça-feira, 27/10" */
export function diaPorExtenso(iso: string): string {
  const [ano, mes, dia] = iso.split('-').map(Number)
  const semana = new Date(ano, mes - 1, dia).toLocaleDateString('pt-BR', { weekday: 'long' })
  return `${semana}, ${doisDigitos(dia)}/${doisDigitos(mes)}`
}

/** "terça-feira" pede "a"; "sábado" e "domingo" pedem "o" ("Temos a terça-feira..."). */
export function artigoDoDia(iso: string): 'a' | 'o' {
  const [ano, mes, dia] = iso.split('-').map(Number)
  const semana = new Date(ano, mes - 1, dia).getDay()
  return semana === 0 || semana === 6 ? 'o' : 'a'
}

/** Soma (ou subtrai) meses; new Date resolve a virada de ano sozinho. */
export function mudarMes(ano: number, mes: number, quantos: number): { ano: number; mes: number } {
  const d = new Date(ano, mes - 1 + quantos, 1)
  return { ano: d.getFullYear(), mes: d.getMonth() + 1 }
}

/** Quantos dias tem o período, contando o primeiro e o último ("22" a "26" = 5 dias). */
export function diasNoPeriodo(de: string, ate: string): number {
  const dia = (iso: string) => Date.UTC(Number(iso.slice(0, 4)), Number(iso.slice(5, 7)) - 1, Number(iso.slice(8)))
  return Math.round((dia(ate) - dia(de)) / 86_400_000) + 1
}

/** "terça-feira, 27/10" (um dia) ou "terça-feira, 22/10 → sábado, 26/10" (período). */
export function descreverPeriodo(de: string, ate: string): string {
  return de === ate ? diaPorExtenso(de) : `${diaPorExtenso(de)} → ${diaPorExtenso(ate)}`
}
