import { useEffect, useState } from 'react'

/**
 * Devolve o valor só depois que ele para de mudar por `atraso` ms (técnica chamada "debounce").
 * Na busca, evita uma chamada à API a cada letra: "joão" seriam 4 chamadas; assim, é 1.
 */
export function useValorAtrasado<T>(valor: T, atraso = 300): T {
  const [atrasado, setAtrasado] = useState(valor)

  useEffect(() => {
    const temporizador = setTimeout(() => setAtrasado(valor), atraso)
    // Se o valor mudar antes do tempo, o React chama esta "limpeza" e o temporizador anterior é cancelado.
    return () => clearTimeout(temporizador)
  }, [valor, atraso])

  return atrasado
}
