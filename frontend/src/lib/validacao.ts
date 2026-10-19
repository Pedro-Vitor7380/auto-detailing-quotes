// As mesmas regras do Domínio (Cliente.cs e Veiculo.cs), para o formulário avisar na hora.
// A API continua sendo a garantia: estas funções só melhoram a experiência de quem digita.

/** "abc-1d23" → "ABC1D23", ou null se não for uma placa antiga (ABC1234) ou Mercosul (ABC1D23). */
export function normalizarPlaca(texto: string): string | null {
  const limpa = texto.replace(/[^A-Za-z0-9]/g, '').toUpperCase()
  return /^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$/.test(limpa) ? limpa : null
}

/** "(11) 99999-8888" → "5511999998888", ou null se não for um número brasileiro com DDD. */
export function normalizarWhatsApp(texto: string): string | null {
  let numeros = texto.replace(/\D/g, '')
  const temCodigoDoPais = texto.trimStart().startsWith('+')
  if (!temCodigoDoPais && (numeros.length === 10 || numeros.length === 11)) numeros = '55' + numeros
  const valido = numeros.startsWith('55') && (numeros.length === 12 || (numeros.length === 13 && numeros[4] === '9'))
  return valido ? numeros : null
}
