import { zodResolver } from '@hookform/resolvers/zod'
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Car, ChevronRight, LoaderCircle, Plus, Search, UserPlus } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { apiClientes, chavesClientes, type Cliente, type Veiculo } from '../../api/clientes'
import { AlertaDeErro, Campo, classeDoBotaoPrincipal, classeDoBotaoSecundario, classeDoCampo } from '../../components/formulario'
import { formatarPlaca, formatarWhatsApp } from '../../lib/formatacao'
import { useValorAtrasado } from '../../lib/useValorAtrasado'
import { esquemaDoCliente, esquemaDoVeiculo } from '../clientes/esquemas'

export interface Selecao {
  cliente: Pick<Cliente, 'id' | 'nome' | 'whatsApp'>
  veiculo: Veiculo
}

/**
 * Escolhe cliente e veículo. Três caminhos, todos sem sair da tela do orçamento:
 * buscar um cliente existente, cadastrar um cliente novo (já com o carro) ou adicionar um carro a um cliente.
 */
export function SeletorDeCliente({ aoSelecionar }: { aoSelecionar: (s: Selecao) => void }) {
  const [busca, setBusca] = useState('')
  const [modo, setModo] = useState<'buscar' | 'novoCliente'>('buscar')
  const [clienteEscolhido, setClienteEscolhido] = useState<Cliente | null>(null)
  const termo = useValorAtrasado(busca.trim())

  const clientes = useQuery({
    queryKey: chavesClientes.busca(termo),
    queryFn: () => apiClientes.buscar(termo),
    placeholderData: keepPreviousData,
    enabled: modo === 'buscar' && !clienteEscolhido,
  })

  function escolher(cliente: Cliente) {
    // Um carro só: já seleciona. Mais de um (ou nenhum): mostra as opções.
    if (cliente.veiculos.length === 1) aoSelecionar({ cliente, veiculo: cliente.veiculos[0] })
    else setClienteEscolhido(cliente)
  }

  if (modo === 'novoCliente')
    return <NovoClienteRapido nomeInicial={busca} aoCriar={escolher} aoCancelar={() => setModo('buscar')} />

  if (clienteEscolhido)
    return (
      <EscolhaDoVeiculo
        cliente={clienteEscolhido}
        aoEscolher={(veiculo) => aoSelecionar({ cliente: clienteEscolhido, veiculo })}
        aoVoltar={() => setClienteEscolhido(null)}
      />
    )

  return (
    <div>
      <div className="relative">
        <Search className="pointer-events-none absolute top-1/2 left-3 size-5 -translate-y-1/2 text-slate-400" aria-hidden />
        <input
          type="search"
          placeholder="Buscar por nome ou placa"
          aria-label="Buscar cliente por nome ou placa"
          value={busca}
          onChange={(e) => setBusca(e.target.value)}
          className={`${classeDoCampo} mt-0 pl-10`}
        />
      </div>

      <ul className="mt-2 max-h-72 divide-y divide-slate-100 overflow-y-auto rounded-xl border border-slate-200 bg-white">
        {clientes.isPending ? (
          <li className="flex items-center gap-2 px-4 py-3 text-sm text-slate-500">
            <LoaderCircle className="size-4 animate-spin" aria-hidden /> Buscando...
          </li>
        ) : clientes.isError ? (
          <li className="p-3">
            <AlertaDeErro erro={clientes.error} />
          </li>
        ) : clientes.data.length === 0 ? (
          <li className="px-4 py-3 text-sm text-slate-500">{termo ? 'Nenhum cliente encontrado.' : 'Nenhum cliente cadastrado.'}</li>
        ) : (
          clientes.data.map((c) => (
            <li key={c.id}>
              <button type="button" onClick={() => escolher(c)} className="flex w-full items-center gap-3 px-4 py-3 text-left active:bg-slate-100">
                <div className="min-w-0 flex-1">
                  <p className="truncate font-medium">{c.nome}</p>
                  <p className="truncate text-sm text-slate-500">
                    {c.veiculos.length ? c.veiculos.map((v) => `${formatarPlaca(v.placa)} ${v.modelo}`).join(' · ') : 'Sem veículo'}
                  </p>
                </div>
                <ChevronRight className="size-5 shrink-0 text-slate-400" aria-hidden />
              </button>
            </li>
          ))
        )}
      </ul>

      <button type="button" onClick={() => setModo('novoCliente')} className={`${classeDoBotaoSecundario} mt-3 w-full`}>
        <UserPlus className="size-5" aria-hidden />
        Cliente novo
      </button>
    </div>
  )
}

function EscolhaDoVeiculo({ cliente, aoEscolher, aoVoltar }: { cliente: Cliente; aoEscolher: (v: Veiculo) => void; aoVoltar: () => void }) {
  const [adicionando, setAdicionando] = useState(cliente.veiculos.length === 0)
  const queryClient = useQueryClient()
  const { register, handleSubmit, formState: { errors } } = useForm<z.infer<typeof esquemaDoVeiculo>>({
    resolver: zodResolver(esquemaDoVeiculo),
    defaultValues: { placa: '', modelo: '' },
  })

  const adicionar = useMutation({
    mutationFn: (v: z.infer<typeof esquemaDoVeiculo>) => apiClientes.adicionarVeiculo(cliente.id, v),
    onSuccess: (atualizado) => {
      void queryClient.invalidateQueries({ queryKey: chavesClientes.todas, refetchType: 'all' })
      // O veículo novo é o que não existia antes.
      const novo = atualizado.veiculos.find((v) => !cliente.veiculos.some((antigo) => antigo.id === v.id))
      if (novo) aoEscolher(novo)
    },
  })

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-4">
      <p className="font-medium">{cliente.nome}</p>
      <p className="text-sm text-slate-500">{formatarWhatsApp(cliente.whatsApp)}</p>

      {cliente.veiculos.length > 0 && <p className="mt-3 text-sm font-medium text-slate-700">Qual veículo?</p>}
      <div className="mt-2 grid gap-2">
        {cliente.veiculos.map((v) => (
          <button key={v.id} type="button" onClick={() => aoEscolher(v)} className={`${classeDoBotaoSecundario} justify-start`}>
            <Car className="size-5 text-slate-400" aria-hidden />
            <span className="font-mono font-semibold">{formatarPlaca(v.placa)}</span>
            <span className="truncate text-slate-500">{v.modelo}</span>
          </button>
        ))}
      </div>

      {adicionando ? (
        // <div> e não <form>: este seletor fica DENTRO do formulário do orçamento, e formulário dentro de
        // formulário não existe em HTML (o clique enviaria a página inteira). O botão chama handleSubmit direto.
        <div className="mt-3 space-y-3 border-t border-slate-100 pt-3">
          <div className="grid grid-cols-[8rem_1fr] gap-3">
            <Campo id="sel-placa" rotulo="Placa" erro={errors.placa?.message}>
              <input id="sel-placa" {...register('placa')} autoCapitalize="characters" placeholder="ABC1D23" className={`${classeDoCampo} uppercase`} />
            </Campo>
            <Campo id="sel-modelo" rotulo="Modelo" erro={errors.modelo?.message}>
              <input id="sel-modelo" {...register('modelo')} placeholder="Ex.: Civic 2019" className={classeDoCampo} />
            </Campo>
          </div>
          <AlertaDeErro erro={adicionar.error} />
          <button type="button" onClick={handleSubmit((v) => adicionar.mutate(v))} disabled={adicionar.isPending} className={`${classeDoBotaoPrincipal} w-full`}>
            {adicionar.isPending && <LoaderCircle className="size-5 animate-spin" aria-hidden />}
            Adicionar veículo e usar
          </button>
        </div>
      ) : (
        <button type="button" onClick={() => setAdicionando(true)} className="mt-3 flex h-11 items-center gap-1 text-sm font-medium text-marca-700">
          <Plus className="size-4" aria-hidden /> Outro veículo
        </button>
      )}

      <button type="button" onClick={aoVoltar} className="mt-1 h-11 text-sm text-slate-600">
        ← Escolher outro cliente
      </button>
    </div>
  )
}

const esquemaDoClienteRapido = esquemaDoCliente.extend(esquemaDoVeiculo.shape)
type ValoresDoClienteRapido = z.infer<typeof esquemaDoClienteRapido>

/** Cadastro mínimo no pátio: nome, WhatsApp e o carro, tudo obrigatório (o orçamento precisa de um veículo). */
function NovoClienteRapido({ nomeInicial, aoCriar, aoCancelar }: { nomeInicial: string; aoCriar: (c: Cliente) => void; aoCancelar: () => void }) {
  const queryClient = useQueryClient()
  // Aproveita o que já foi digitado na busca: sem números parece um nome; com números, uma placa.
  const pareceNome = !/\d/.test(nomeInicial)
  const { register, handleSubmit, formState: { errors } } = useForm<ValoresDoClienteRapido>({
    resolver: zodResolver(esquemaDoClienteRapido),
    defaultValues: { nome: pareceNome ? nomeInicial.trim() : '', whatsApp: '', placa: pareceNome ? '' : nomeInicial.trim(), modelo: '' },
  })

  const criar = useMutation({
    mutationFn: (v: ValoresDoClienteRapido) =>
      apiClientes.criar({ nome: v.nome, whatsApp: v.whatsApp, veiculos: [{ placa: v.placa, modelo: v.modelo }] }),
    onSuccess: (cliente) => {
      void queryClient.invalidateQueries({ queryKey: chavesClientes.todas, refetchType: 'all' })
      aoCriar(cliente)
    },
  })

  return (
    // <div> e não <form>: fica dentro do formulário do orçamento (ver EscolhaDoVeiculo).
    <div className="space-y-4 rounded-xl border border-slate-200 bg-white p-4">
      <p className="font-medium">Cliente novo</p>
      <Campo id="rap-nome" rotulo="Nome" erro={errors.nome?.message}>
        <input id="rap-nome" {...register('nome')} autoCapitalize="words" className={classeDoCampo} />
      </Campo>
      <Campo id="rap-whatsApp" rotulo="WhatsApp" erro={errors.whatsApp?.message}>
        <input id="rap-whatsApp" type="tel" {...register('whatsApp')} placeholder="(11) 99999-8888" className={classeDoCampo} />
      </Campo>
      <div className="grid grid-cols-[8rem_1fr] gap-3">
        <Campo id="rap-placa" rotulo="Placa" erro={errors.placa?.message}>
          <input id="rap-placa" {...register('placa')} autoCapitalize="characters" placeholder="ABC1D23" className={`${classeDoCampo} uppercase`} />
        </Campo>
        <Campo id="rap-modelo" rotulo="Modelo" erro={errors.modelo?.message}>
          <input id="rap-modelo" {...register('modelo')} placeholder="Ex.: Civic 2019" className={classeDoCampo} />
        </Campo>
      </div>
      <AlertaDeErro erro={criar.error} />
      <div className="flex gap-2">
        <button type="button" onClick={aoCancelar} className={`${classeDoBotaoSecundario} flex-1`}>
          Voltar
        </button>
        <button type="button" onClick={handleSubmit((v) => criar.mutate(v))} disabled={criar.isPending} className={`${classeDoBotaoPrincipal} flex-[2]`}>
          {criar.isPending && <LoaderCircle className="size-5 animate-spin" aria-hidden />}
          Cadastrar e usar
        </button>
      </div>
    </div>
  )
}
