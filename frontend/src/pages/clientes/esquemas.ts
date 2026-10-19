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

export const esquemaDoVeiculo = z.object({
  placa: z
    .string()
    .trim()
    .min(1, 'Informe a placa.')
    .refine((v) => normalizarPlaca(v) !== null, 'Placa inválida. Ex.: ABC1D23 ou ABC-1234.'),
  modelo: z.string().trim().min(1, 'Informe o modelo.').max(100, 'Máximo de 100 caracteres.'),
})

export type ValoresDoCliente = z.infer<typeof esquemaDoCliente>
export type ValoresDoVeiculo = z.infer<typeof esquemaDoVeiculo>
