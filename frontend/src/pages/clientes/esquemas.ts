import { z } from 'zod'
import { normalizarPlaca, normalizarWhatsApp } from '../../lib/validacao'

// Regras do formulário, as mesmas do Domínio (Cliente.cs / Veiculo.cs).
export const esquemaDoCliente = z.object({
  nome: z.string().trim().min(1, 'Informe o nome do cliente.').max(150, 'Máximo de 150 caracteres.'),
  whatsApp: z
    .string()
    .trim()
    .min(1, 'Informe o WhatsApp.')
    .refine((v) => normalizarWhatsApp(v) !== null, 'Informe o DDD e o número, ex.: (11) 99999-8888.'),
})

// Calculado uma vez, quando o arquivo é carregado (não a cada tela desenhada).
const ANO_MAXIMO = new Date().getFullYear() + 1

export const esquemaDoVeiculo = z.object({
  placa: z
    .string()
    .trim()
    .min(1, 'Informe a placa.')
    .refine((v) => normalizarPlaca(v) !== null, 'Placa inválida. Ex.: ABC1D23 ou ABC-1234.'),
  modelo: z.string().trim().min(1, 'Informe o modelo.').max(100, 'Máximo de 100 caracteres.'),
  // Opcionais: ajudam a oficina a achar a tinta certa. O teto do ano é o do ano que vem (modelos novos saem antes).
  cor: z.string().trim().max(40, 'Máximo de 40 caracteres.'),
  anoFabricacao: z
    .string()
    .trim()
    .refine((v) => v === '' || (/^\d{4}$/.test(v) && Number(v) >= 1900 && Number(v) <= ANO_MAXIMO), `Ano entre 1900 e ${ANO_MAXIMO}.`),
})

/** Valores vazios do formulário de veículo. */
export const veiculoVazio = { placa: '', modelo: '', cor: '', anoFabricacao: '' }

/** Formulário → API: texto vazio vira null; o ano vira número. */
export function paraDadosDoVeiculo(v: ValoresDoVeiculo) {
  return { placa: v.placa, modelo: v.modelo, cor: v.cor || null, anoFabricacao: v.anoFabricacao ? Number(v.anoFabricacao) : null }
}

export type ValoresDoCliente = z.infer<typeof esquemaDoCliente>
export type ValoresDoVeiculo = z.infer<typeof esquemaDoVeiculo>
