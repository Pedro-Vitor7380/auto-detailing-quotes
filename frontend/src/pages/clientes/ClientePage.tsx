import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, Car, Check, LoaderCircle, MessageCircle, Pencil, Plus, Trash2, X } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router'
import { apiClientes, chavesClientes, type Cliente, type Veiculo } from '../../api/clientes'
import {
  AlertaDeErro,
  Campo,
  Carregando,
  classeDoBotaoPrincipal,
  classeDoBotaoSecundario,
  classeDoCampo,
} from '../../components/formulario'
import { formatarPlaca, formatarWhatsApp } from '../../lib/formatacao'
import { esquemaDoCliente, esquemaDoVeiculo, type ValoresDoCliente, type ValoresDoVeiculo } from './esquemas'

/** Depois de qualquer alteração: guarda o cliente devolvido pela API e marca as buscas como desatualizadas. */
function useAoAlterarCliente() {
  const queryClient = useQueryClient()
  return (cliente: Cliente) => {
    queryClient.setQueryData(chavesClientes.cliente(cliente.id), cliente)
    void queryClient.invalidateQueries({ queryKey: chavesClientes.todas, refetchType: 'all' })
  }
}

export function ClientePage() {
  const { id } = useParams()
  const cliente = useQuery({ queryKey: chavesClientes.cliente(id!), queryFn: () => apiClientes.obter(id!) })

  return (
    <section className="mx-auto max-w-xl">
      <Link to="/clientes" className="-ml-1 inline-flex h-11 items-center gap-1 text-sm font-medium text-slate-600">
        <ArrowLeft className="size-4" aria-hidden />
        Clientes
      </Link>

      {cliente.isPending ? (
        <Carregando />
      ) : cliente.isError ? (
        <div className="mt-4">
          <AlertaDeErro erro={cliente.error} />
        </div>
      ) : (
        <>
          <div className="flex items-start justify-between gap-3">
            <h1 className="min-w-0 text-2xl font-bold break-words">{cliente.data.nome}</h1>
            {/* wa.me: link oficial do WhatsApp; abre a conversa com o número, no app ou no navegador.
                green-700: branco sobre green-600 teria contraste 3,3:1 (abaixo de 4,5:1); sobre green-700 é 5,0:1 */}
            <a
              href={`https://wa.me/${cliente.data.whatsApp}`}
              target="_blank"
              rel="noreferrer"
              className="flex h-11 shrink-0 items-center gap-2 rounded-lg bg-green-700 px-3 text-sm font-semibold text-white hover:bg-green-800"
            >
              <MessageCircle className="size-5" aria-hidden />
              WhatsApp
            </a>
          </div>
          <DadosDoCliente cliente={cliente.data} />
          <Veiculos cliente={cliente.data} />
          <ExcluirCliente cliente={cliente.data} />
        </>
      )}
    </section>
  )
}

function DadosDoCliente({ cliente }: { cliente: Cliente }) {
  const aoAlterar = useAoAlterarCliente()
  const [salvo, setSalvo] = useState(false)
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<ValoresDoCliente>({
    resolver: zodResolver(esquemaDoCliente),
    defaultValues: { nome: cliente.nome, whatsApp: formatarWhatsApp(cliente.whatsApp) },
  })

  const salvar = useMutation({
    mutationFn: (v: ValoresDoCliente) => apiClientes.atualizar(cliente.id, v),
    onSuccess: (atualizado) => {
      setSalvo(true)
      aoAlterar(atualizado)
      // Os valores salvos viram o novo "ponto de partida": o botão volta a ficar desabilitado (isDirty = false).
      reset({ nome: atualizado.nome, whatsApp: formatarWhatsApp(atualizado.whatsApp) })
    },
  })

  return (
    <form
      onSubmit={handleSubmit((v) => salvar.mutate(v))}
      noValidate
      className="mt-4 space-y-4 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
    >
      <Campo id="nome" rotulo="Nome" erro={errors.nome?.message}>
        <input id="nome" {...register('nome')} aria-invalid={!!errors.nome} className={classeDoCampo} />
      </Campo>
      <Campo id="whatsApp" rotulo="WhatsApp" erro={errors.whatsApp?.message}>
        <input id="whatsApp" type="tel" {...register('whatsApp')} aria-invalid={!!errors.whatsApp} className={classeDoCampo} />
      </Campo>
      <AlertaDeErro erro={salvar.error} />
      <div className="flex items-center justify-end gap-3">
        {salvo && !isDirty && (
          <span role="status" className="flex items-center gap-1 text-sm text-green-700">
            <Check className="size-4" aria-hidden /> Salvo
          </span>
        )}
        {/* isDirty: só habilita quando algo foi alterado */}
        <button type="submit" disabled={!isDirty || salvar.isPending} className={classeDoBotaoPrincipal}>
          {salvar.isPending && <LoaderCircle className="size-5 animate-spin" aria-hidden />}
          Salvar dados
        </button>
      </div>
    </form>
  )
}

function Veiculos({ cliente }: { cliente: Cliente }) {
  const [adicionando, setAdicionando] = useState(cliente.veiculos.length === 0)

  return (
    <div className="mt-6">
      <h2 className="text-sm font-semibold tracking-wide text-slate-500 uppercase">Veículos</h2>
      {cliente.veiculos.length > 0 && (
        <ul className="mt-2 divide-y divide-slate-100 overflow-hidden rounded-xl border border-slate-200 bg-white">
          {cliente.veiculos.map((v) => (
            <LinhaDoVeiculo key={v.id} cliente={cliente} veiculo={v} />
          ))}
        </ul>
      )}
      {adicionando ? (
        <FormularioDoVeiculo
          cliente={cliente}
          aoTerminar={() => setAdicionando(false)}
          podeCancelar={cliente.veiculos.length > 0}
        />
      ) : (
        <button onClick={() => setAdicionando(true)} className={`${classeDoBotaoSecundario} mt-3 w-full`}>
          <Plus className="size-5" aria-hidden />
          Adicionar veículo
        </button>
      )}
    </div>
  )
}

function LinhaDoVeiculo({ cliente, veiculo }: { cliente: Cliente; veiculo: Veiculo }) {
  const aoAlterar = useAoAlterarCliente()
  const [editando, setEditando] = useState(false)
  const remover = useMutation({ mutationFn: () => apiClientes.removerVeiculo(cliente.id, veiculo.id), onSuccess: aoAlterar })

  if (editando)
    return (
      <li className="p-3">
        <FormularioDoVeiculo cliente={cliente} veiculo={veiculo} aoTerminar={() => setEditando(false)} podeCancelar />
      </li>
    )

  const botaoIcone = 'flex size-11 shrink-0 items-center justify-center rounded-lg text-slate-500 hover:bg-slate-100 disabled:opacity-40'
  return (
    <li className="px-4 py-2">
      <div className="flex items-center gap-2">
        <Car className="size-5 shrink-0 text-slate-400" aria-hidden />
        <div className="min-w-0 flex-1">
          <p className="font-mono font-semibold">{formatarPlaca(veiculo.placa)}</p>
          <p className="truncate text-sm text-slate-500">{veiculo.modelo}</p>
        </div>
        <button onClick={() => setEditando(true)} aria-label={`Editar ${veiculo.placa}`} className={botaoIcone}>
          <Pencil className="size-5" aria-hidden />
        </button>
        <button
          onClick={() => {
            if (window.confirm(`Remover o veículo ${formatarPlaca(veiculo.placa)}?`)) remover.mutate()
          }}
          disabled={remover.isPending}
          aria-label={`Remover ${veiculo.placa}`}
          className={botaoIcone}
        >
          <Trash2 className="size-5" aria-hidden />
        </button>
      </div>
      <AlertaDeErro erro={remover.error} />
    </li>
  )
}

/** Serve para adicionar (sem `veiculo`) e para editar (com `veiculo`). */
function FormularioDoVeiculo({
  cliente,
  veiculo,
  aoTerminar,
  podeCancelar,
}: {
  cliente: Cliente
  veiculo?: Veiculo
  aoTerminar: () => void
  podeCancelar: boolean
}) {
  const aoAlterar = useAoAlterarCliente()
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ValoresDoVeiculo>({
    resolver: zodResolver(esquemaDoVeiculo),
    defaultValues: { placa: veiculo ? formatarPlaca(veiculo.placa) : '', modelo: veiculo?.modelo ?? '' },
  })

  const salvar = useMutation({
    mutationFn: (v: ValoresDoVeiculo) =>
      veiculo ? apiClientes.atualizarVeiculo(cliente.id, veiculo.id, v) : apiClientes.adicionarVeiculo(cliente.id, v),
    onSuccess: (atualizado) => {
      aoAlterar(atualizado)
      aoTerminar()
    },
  })

  const prefixo = veiculo ? `veiculo-${veiculo.id}` : 'novo-veiculo'
  return (
    <form
      onSubmit={handleSubmit((v) => salvar.mutate(v))}
      noValidate
      className={`space-y-3 ${veiculo ? '' : 'mt-3 rounded-xl border border-slate-200 bg-white p-4'}`}
    >
      <div className="grid grid-cols-[8rem_1fr] gap-3">
        <Campo id={`${prefixo}-placa`} rotulo="Placa" erro={errors.placa?.message}>
          <input
            id={`${prefixo}-placa`}
            {...register('placa')}
            autoCapitalize="characters"
            placeholder="ABC1D23"
            aria-invalid={!!errors.placa}
            className={`${classeDoCampo} uppercase`}
          />
        </Campo>
        <Campo id={`${prefixo}-modelo`} rotulo="Modelo" erro={errors.modelo?.message}>
          <input
            id={`${prefixo}-modelo`}
            {...register('modelo')}
            placeholder="Ex.: Civic 2019"
            aria-invalid={!!errors.modelo}
            className={classeDoCampo}
          />
        </Campo>
      </div>
      <AlertaDeErro erro={salvar.error} />
      <div className="flex justify-end gap-2">
        {podeCancelar && (
          <button type="button" onClick={aoTerminar} className={classeDoBotaoSecundario}>
            <X className="size-4" aria-hidden />
            Cancelar
          </button>
        )}
        <button type="submit" disabled={salvar.isPending} className={classeDoBotaoPrincipal}>
          {salvar.isPending && <LoaderCircle className="size-5 animate-spin" aria-hidden />}
          {veiculo ? 'Salvar veículo' : 'Adicionar'}
        </button>
      </div>
    </form>
  )
}

function ExcluirCliente({ cliente }: { cliente: Cliente }) {
  const navegar = useNavigate()
  const queryClient = useQueryClient()
  const excluir = useMutation({
    mutationFn: () => apiClientes.excluir(cliente.id),
    onSuccess: async () => {
      queryClient.removeQueries({ queryKey: chavesClientes.cliente(cliente.id) })
      await queryClient.invalidateQueries({ queryKey: chavesClientes.todas, refetchType: 'all' })
      navegar('/clientes', { replace: true })
    },
  })

  return (
    <div className="mt-10 border-t border-slate-200 pt-6">
      <AlertaDeErro erro={excluir.error} />
      <button
        onClick={() => {
          if (window.confirm(`Excluir ${cliente.nome} e os veículos dele? Isso não pode ser desfeito.`)) excluir.mutate()
        }}
        disabled={excluir.isPending}
        className="mt-2 flex h-11 items-center gap-2 text-sm font-medium text-red-600 hover:text-red-700 disabled:opacity-60"
      >
        <Trash2 className="size-4" aria-hidden />
        Excluir cliente
      </button>
    </div>
  )
}
