import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, LoaderCircle } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router'
import { z } from 'zod'
import { apiClientes, chavesClientes } from '../../api/clientes'
import { AlertaDeErro, Campo, classeDoBotaoPrincipal, classeDoCampo } from '../../components/formulario'
import { esquemaDoCliente, esquemaDoVeiculo } from './esquemas'

// No pátio, o cliente e o carro são cadastrados juntos, numa tela só. O veículo é opcional,
// mas, se começar a preencher, placa e modelo passam a ser obrigatórios.
const esquema = esquemaDoCliente
  .extend({ placa: z.string().trim(), modelo: z.string().trim() })
  .superRefine((v, ctx) => {
    if (!v.placa && !v.modelo) return
    // Reaproveita as regras do veículo; cada erro já vem com o "path" do campo certo (placa ou modelo).
    const veiculo = esquemaDoVeiculo.safeParse(v)
    for (const problema of veiculo.error?.issues ?? []) ctx.addIssue({ code: 'custom', message: problema.message, path: problema.path })
  })

type Valores = z.infer<typeof esquema>

export function NovoClientePage() {
  const navegar = useNavigate()
  const queryClient = useQueryClient()
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<Valores>({
    resolver: zodResolver(esquema),
    defaultValues: { nome: '', whatsApp: '', placa: '', modelo: '' },
  })

  const criar = useMutation({
    mutationFn: (v: Valores) =>
      apiClientes.criar({
        nome: v.nome,
        whatsApp: v.whatsApp,
        veiculos: v.placa ? [{ placa: v.placa, modelo: v.modelo }] : [],
      }),
    onSuccess: (cliente) => {
      // O cliente novo já fica no cache: a tela dele abre sem chamar a API de novo.
      queryClient.setQueryData(chavesClientes.cliente(cliente.id), cliente)
      void queryClient.invalidateQueries({ queryKey: chavesClientes.todas, refetchType: 'all' })
      // replace: o "voltar" do celular não retorna a este formulário já enviado.
      navegar(`/clientes/${cliente.id}`, { replace: true })
    },
  })

  const campo = (nome: keyof Valores) => ({
    id: nome,
    ...register(nome),
    'aria-invalid': !!errors[nome],
    'aria-describedby': errors[nome] ? `${nome}-erro` : undefined,
  })

  return (
    <section className="mx-auto max-w-xl">
      <Link to="/clientes" className="-ml-1 inline-flex h-11 items-center gap-1 text-sm font-medium text-slate-600">
        <ArrowLeft className="size-4" aria-hidden />
        Clientes
      </Link>
      <h1 className="text-2xl font-bold">Novo cliente</h1>

      <form
        onSubmit={handleSubmit((v) => criar.mutate(v))}
        noValidate
        className="mt-6 space-y-5 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
      >
        <Campo id="nome" rotulo="Nome" erro={errors.nome?.message}>
          <input {...campo('nome')} autoComplete="off" autoCapitalize="words" className={classeDoCampo} />
        </Campo>
        <Campo id="whatsApp" rotulo="WhatsApp" erro={errors.whatsApp?.message}>
          {/* type="tel": abre o teclado de telefone no celular */}
          <input {...campo('whatsApp')} type="tel" placeholder="(11) 99999-8888" className={classeDoCampo} />
        </Campo>

        <fieldset className="border-t border-slate-200 pt-4">
          <legend className="text-sm font-semibold text-slate-700">Veículo (opcional)</legend>
          <div className="mt-2 grid grid-cols-[8rem_1fr] gap-3">
            <Campo id="placa" rotulo="Placa" erro={errors.placa?.message}>
              <input {...campo('placa')} autoCapitalize="characters" placeholder="ABC1D23" className={`${classeDoCampo} uppercase`} />
            </Campo>
            <Campo id="modelo" rotulo="Modelo" erro={errors.modelo?.message}>
              <input {...campo('modelo')} placeholder="Ex.: Civic 2019" className={classeDoCampo} />
            </Campo>
          </div>
        </fieldset>

        <AlertaDeErro erro={criar.error} />

        <button type="submit" disabled={criar.isPending} className={`${classeDoBotaoPrincipal} w-full sm:ml-auto sm:w-auto`}>
          {criar.isPending && <LoaderCircle className="size-5 animate-spin" aria-hidden />}
          {criar.isPending ? 'Salvando...' : 'Salvar cliente'}
        </button>
      </form>
    </section>
  )
}
