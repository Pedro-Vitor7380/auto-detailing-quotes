import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Car, LoaderCircle, MessageSquareText, Minus, PenLine, Plus, Trash2 } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useFieldArray, useForm, useWatch, type Control } from 'react-hook-form'
import { z } from 'zod'
import { apiCatalogo, chavesCatalogo, type Servico } from '../../api/catalogo'
import { apiOrcamentos, type Orcamento, type OrcamentoParaSalvar, type TipoDeDesconto } from '../../api/orcamentos'
import { AlertaDeErro, Campo, classeDoBotaoPrincipal, classeDoBotaoSecundario, classeDoCampo } from '../../components/formulario'
import {
  descreverVeiculo,
  formatarData,
  formatarDecimal,
  formatarFaixa,
  formatarMoeda,
  formatarPlaca,
  hojeIso,
  lerValorEmReais,
  somarDias,
} from '../../lib/formatacao'
import { calcularTotais } from './calculos'
import { SeletorDeCliente, type Selecao } from './SeletorDeCliente'
import { SeletorDeServicos } from './SeletorDeServicos'

// ===== Esquema do formulário (as mesmas regras do Domínio: Orcamento.cs / ItemOrcamento.cs) =====

const valorEmReais = (mensagem: string) => z.string().trim().refine((v) => lerValorEmReais(v) !== null, mensagem)

const esquemaDoItem = z.object({
  id: z.string().optional(), // existe = item já salvo; não existe = item novo
  servicoId: z.string().optional(), // existe = do catálogo; não existe = avulso
  descricao: z.string().trim().min(1, 'Descreva o item.').max(150, 'Máximo de 150 caracteres.'),
  quantidade: z.number().int().min(1, 'Mínimo 1.'),
  valor: valorEmReais('Valor inválido.'),
  precoCatalogo: z.number().nullable(),
  observacao: z.string().trim().max(300, 'Máximo de 300 caracteres.'),
})

const esquema = z
  .object({
    clienteId: z.string().min(1, 'Escolha o cliente e o veículo.'),
    veiculoId: z.string().min(1, 'Escolha o cliente e o veículo.'),
    itens: z.array(esquemaDoItem),
    descontoTipo: z.enum(['Nenhum', 'Valor', 'Percentual']),
    descontoValor: z.string().trim(),
    validade: z.string().min(1, 'Informe a validade.'),
    observacoes: z.string().trim().max(1000, 'Máximo de 1000 caracteres.'),
  })
  .superRefine((v, ctx) => {
    if (v.descontoTipo === 'Nenhum') return
    const desconto = lerValorEmReais(v.descontoValor)
    const subtotal = calcularTotais(paraCalculo(v.itens), 'Nenhum', 0).subtotal
    const erro =
      desconto === null ? 'Valor inválido.'
        : v.descontoTipo === 'Percentual' && desconto > 100 ? 'Máximo de 100%.'
          : v.descontoTipo === 'Valor' && desconto > subtotal ? 'Maior que o subtotal.'
            : null
    if (erro) ctx.addIssue({ code: 'custom', message: erro, path: ['descontoValor'] })
  })

type Valores = z.infer<typeof esquema>
type ItemDoFormulario = Valores['itens'][number]

function paraCalculo(itens: ItemDoFormulario[]) {
  return itens.map((i) => ({ quantidade: i.quantidade, valorUnitario: lerValorEmReais(i.valor) ?? 0 }))
}

/** Orçamento da API → valores do formulário. Também usado depois de salvar (os itens novos ganham id). */
function paraValores(o?: Orcamento): Valores {
  return {
    clienteId: o?.clienteId ?? '',
    veiculoId: o?.veiculoId ?? '',
    itens: (o?.itens ?? []).map((i) => ({
      id: i.id,
      servicoId: i.servicoId ?? undefined,
      descricao: i.descricao,
      quantidade: i.quantidade,
      valor: formatarDecimal(i.valorUnitario),
      precoCatalogo: i.precoCatalogo,
      observacao: i.observacaoEstado ?? '',
    })),
    descontoTipo: o?.descontoTipo ?? 'Nenhum',
    descontoValor: o && o.descontoTipo !== 'Nenhum' ? formatarDecimal(o.descontoValor) : '',
    validade: o?.validade ?? somarDias(hojeIso(), 7),
    observacoes: o?.observacoes ?? '',
  }
}

function paraApi(v: Valores, validadeOriginal?: string): OrcamentoParaSalvar {
  return {
    clienteId: v.clienteId,
    veiculoId: v.veiculoId,
    itens: v.itens.map((i) => ({
      id: i.id,
      servicoId: i.id ? undefined : i.servicoId,
      descricao: i.descricao,
      quantidade: i.quantidade,
      valorUnitario: lerValorEmReais(i.valor)!,
      observacaoEstado: i.observacao || undefined,
    })),
    descontoTipo: v.descontoTipo,
    descontoValor: v.descontoTipo === 'Nenhum' ? 0 : lerValorEmReais(v.descontoValor)!,
    // Num orçamento novo, sem mexer na validade, a API usa o padrão (hoje + 7, no fuso de São Paulo).
    validade: !validadeOriginal && v.validade === somarDias(hojeIso(), 7) ? undefined : v.validade,
    observacoes: v.observacoes || undefined,
  }
}

interface Props {
  orcamento?: Orcamento
  aoSalvar: (o: Orcamento) => void
  /** Avisa a tela se há alterações não salvas (os botões de status ficam bloqueados até salvar). */
  aoMudarPendencia?: (temAlteracoes: boolean) => void
}

export function FormularioDoOrcamento({ orcamento, aoSalvar, aoMudarPendencia }: Props) {
  const [selecao, setSelecao] = useState<Selecao | null>(
    orcamento
      ? {
          cliente: { id: orcamento.clienteId, nome: orcamento.clienteNome, whatsApp: orcamento.clienteWhatsApp },
          veiculo: { id: orcamento.veiculoId, placa: orcamento.placa, modelo: orcamento.modelo, cor: orcamento.cor, anoFabricacao: orcamento.anoFabricacao },
        }
      : null,
  )
  const [escolhendoServicos, setEscolhendoServicos] = useState(false)
  // O catálogo serve para mostrar a faixa (mín.–máx.) de cada item; vem do cache se já foi aberto.
  const catalogo = useQuery({ queryKey: chavesCatalogo.servicos, queryFn: apiCatalogo.listarServicos })
  const faixas = useMemo(() => new Map((catalogo.data ?? []).map((s) => [s.id, s])), [catalogo.data])

  const {
    control,
    register,
    handleSubmit,
    setValue,
    reset,
    formState: { errors, isDirty },
  } = useForm<Valores>({ resolver: zodResolver(esquema), defaultValues: paraValores(orcamento) })

  // useFieldArray: a lista de itens dentro do formulário (adicionar, remover, alterar um item).
  const { fields, append, remove, update } = useFieldArray({ control, name: 'itens', keyName: 'chave' })
  // useWatch: "assiste" aos campos e re-renderiza quando mudam. É o que deixa o total ao vivo.
  const itens = useWatch({ control, name: 'itens' })
  const descontoTipo = useWatch({ control, name: 'descontoTipo' })
  const descontoValor = useWatch({ control, name: 'descontoValor' })
  const totais = calcularTotais(paraCalculo(itens), descontoTipo, lerValorEmReais(descontoValor) ?? 0)

  useEffect(() => aoMudarPendencia?.(isDirty), [isDirty, aoMudarPendencia])

  const salvar = useMutation({
    mutationFn: (v: Valores) => {
      const dados = paraApi(v, orcamento?.validade)
      return orcamento ? apiOrcamentos.salvar(orcamento.id, dados) : apiOrcamentos.criar(dados)
    },
    onSuccess: (salvo) => {
      // IMPORTANTE: recarrega o formulário com a resposta. Os itens novos agora têm id; sem isto,
      // o próximo "Salvar" os enviaria de novo como novos, e eles duplicariam.
      reset(paraValores(salvo))
      aoSalvar(salvo)
    },
  })

  function selecionar(s: Selecao) {
    setSelecao(s)
    setValue('clienteId', s.cliente.id, { shouldDirty: true, shouldValidate: true })
    setValue('veiculoId', s.veiculo.id, { shouldDirty: true, shouldValidate: true })
  }

  function adicionarServico(s: Servico) {
    // Tocar de novo no mesmo serviço soma 1 na quantidade (ex.: martelinho em 2 portas), em vez de repetir a linha.
    const indice = itens.findIndex((i) => !i.id && i.servicoId === s.id)
    if (indice >= 0) update(indice, { ...itens[indice], quantidade: itens[indice].quantidade + 1 })
    else
      append({
        servicoId: s.id,
        descricao: s.nome,
        quantidade: 1,
        valor: formatarDecimal(s.precoMinimo), // entra pelo mínimo da faixa
        precoCatalogo: s.precoMinimo,
        observacao: '',
      })
  }

  const noOrcamento = itens.reduce<Record<string, number>>((mapa, i) => {
    if (i.servicoId) mapa[i.servicoId] = (mapa[i.servicoId] ?? 0) + i.quantidade
    return mapa
  }, {})

  return (
    // pb-40: espaço para o conteúdo não ficar escondido atrás da barra fixa do total.
    <form
      onSubmit={handleSubmit((v) => salvar.mutate(v))}
      // Enter ("Ir" no teclado do celular) num campo enviaria o orçamento sem querer (ex.: ao buscar um cliente).
      // Aqui, só o botão Salvar salva.
      onKeyDown={(e) => e.key === 'Enter' && e.target instanceof HTMLInputElement && e.preventDefault()}
      noValidate
      className="pb-40 md:pb-28"
    >
      {/* ===== Cliente e veículo ===== */}
      <section className="mt-4">
        <h2 className="text-sm font-semibold tracking-wide text-slate-500 uppercase">Cliente e veículo</h2>
        <div className="mt-2">
          {selecao ? (
            <div className="flex items-center gap-3 rounded-xl border border-slate-200 bg-white px-4 py-3">
              <Car className="size-6 shrink-0 text-slate-400" aria-hidden />
              <div className="min-w-0 flex-1">
                <p className="truncate font-medium">{selecao.cliente.nome}</p>
                <p className="truncate text-sm text-slate-500">
                  <span className="font-mono font-semibold">{formatarPlaca(selecao.veiculo.placa)}</span> {descreverVeiculo(selecao.veiculo)}
                </p>
              </div>
              <button
                type="button"
                onClick={() => {
                  setSelecao(null)
                  setValue('clienteId', '', { shouldDirty: true })
                  setValue('veiculoId', '', { shouldDirty: true })
                }}
                className="h-11 shrink-0 px-2 text-sm font-medium text-marca-700"
              >
                Trocar
              </button>
            </div>
          ) : (
            <SeletorDeCliente aoSelecionar={selecionar} />
          )}
          {errors.clienteId && <p className="mt-1 text-sm text-red-600">{errors.clienteId.message}</p>}
        </div>
      </section>

      {/* ===== Itens ===== */}
      <section className="mt-6">
        <h2 className="text-sm font-semibold tracking-wide text-slate-500 uppercase">Serviços</h2>
        {fields.length > 0 && (
          <ul className="mt-2 space-y-3">
            {fields.map((campo, indice) => (
              <LinhaDoItem
                key={campo.chave}
                indice={indice}
                control={control}
                register={register}
                aoFormatarValor={(texto) => setValue(`itens.${indice}.valor`, texto)}
                erros={errors.itens?.[indice]}
                faixa={campo.servicoId ? faixas.get(campo.servicoId) : undefined}
                aoMudarQuantidade={(q) => setValue(`itens.${indice}.quantidade`, q, { shouldDirty: true })}
                aoRemover={() => remove(indice)}
              />
            ))}
          </ul>
        )}
        <div className="mt-3 grid grid-cols-2 gap-2">
          <button type="button" onClick={() => setEscolhendoServicos(true)} className={classeDoBotaoPrincipal}>
            <Plus className="size-5" aria-hidden /> Serviço
          </button>
          <button
            type="button"
            onClick={() => append({ descricao: '', quantidade: 1, valor: '', precoCatalogo: null, observacao: '' })}
            className={classeDoBotaoSecundario}
          >
            <PenLine className="size-5" aria-hidden /> Item avulso
          </button>
        </div>
      </section>

      {/* ===== Desconto, validade e observações ===== */}
      <section className="mt-6 space-y-4 rounded-2xl border border-slate-200 bg-white p-4">
        <fieldset>
          <legend className="text-sm font-medium text-slate-700">Desconto no total</legend>
          <div className="mt-1 flex gap-2">
            <div className="grid grid-cols-3 rounded-lg border border-slate-300 p-1" role="radiogroup" aria-label="Tipo de desconto">
              {(['Nenhum', 'Valor', 'Percentual'] as TipoDeDesconto[]).map((tipo) => (
                <label key={tipo} className={`flex h-10 min-w-12 cursor-pointer items-center justify-center rounded-md px-2 text-sm font-medium ${descontoTipo === tipo ? 'bg-grafite-900 text-marca-100' : 'text-slate-600'}`}>
                  <input type="radio" value={tipo} {...register('descontoTipo')} className="sr-only" />
                  {tipo === 'Nenhum' ? 'Sem' : tipo === 'Valor' ? 'R$' : '%'}
                </label>
              ))}
            </div>
            {descontoTipo !== 'Nenhum' && (
              <input
                {...register('descontoValor')}
                aria-label={descontoTipo === 'Valor' ? 'Desconto em reais' : 'Desconto em porcentagem'}
                aria-invalid={!!errors.descontoValor}
                inputMode="decimal"
                placeholder={descontoTipo === 'Valor' ? '50,00' : '10'}
                className={`${classeDoCampo} mt-0 min-w-0 flex-1`}
              />
            )}
          </div>
          {errors.descontoValor && <p className="mt-1 text-sm text-red-600">{errors.descontoValor.message}</p>}
        </fieldset>

        <Campo id="validade" rotulo="Válido até" erro={errors.validade?.message}>
          <input id="validade" type="date" min={hojeIso()} {...register('validade')} className={classeDoCampo} />
        </Campo>

        <Campo id="observacoes" rotulo="Observações (opcional)" erro={errors.observacoes?.message}>
          <textarea id="observacoes" rows={2} {...register('observacoes')} className={`${classeDoCampo} h-auto py-2`} />
        </Campo>
      </section>

      <div className="mt-4">
        <AlertaDeErro erro={salvar.error} />
      </div>

      {/* ===== Barra fixa: total e salvar (acima da barra de navegação no celular) ===== */}
      <div className="fixed inset-x-0 bottom-[calc(4rem+env(safe-area-inset-bottom))] z-20 border-t border-slate-200 bg-white px-4 py-3 shadow-[0_-4px_12px_rgba(0,0,0,0.06)] md:bottom-0 md:left-60">
        <div className="mx-auto flex max-w-3xl items-center gap-3">
          <div className="min-w-0 flex-1">
            {totais.desconto > 0 && (
              <p className="truncate text-xs text-slate-500">
                Subtotal {formatarMoeda(totais.subtotal)} − {formatarMoeda(totais.desconto)}
              </p>
            )}
            <p className="text-2xl font-bold" aria-live="polite">{formatarMoeda(totais.total)}</p>
          </div>
          <button type="submit" disabled={salvar.isPending || (!!orcamento && !isDirty)} className={`${classeDoBotaoPrincipal} shrink-0 px-6`}>
            {salvar.isPending && <LoaderCircle className="size-5 animate-spin" aria-hidden />}
            {salvar.isPending ? 'Salvando' : orcamento && !isDirty ? 'Salvo' : 'Salvar'}
          </button>
        </div>
      </div>

      {escolhendoServicos && (
        <SeletorDeServicos aoFechar={() => setEscolhendoServicos(false)} aoAdicionar={adicionarServico} noOrcamento={noOrcamento} />
      )}
    </form>
  )
}

interface LinhaProps {
  indice: number
  control: Control<Valores>
  register: ReturnType<typeof useForm<Valores>>['register']
  erros?: { descricao?: { message?: string }; valor?: { message?: string }; observacao?: { message?: string } }
  faixa?: Servico
  aoMudarQuantidade: (q: number) => void
  aoFormatarValor: (texto: string) => void
  aoRemover: () => void
}

function LinhaDoItem({ indice, control, register, erros, faixa, aoMudarQuantidade, aoFormatarValor, aoRemover }: LinhaProps) {
  const item = useWatch({ control, name: `itens.${indice}` })
  const [mostrarObservacao, setMostrarObservacao] = useState(!!item.observacao)
  const valor = lerValorEmReais(item.valor)
  const ajuste = item.precoCatalogo !== null && valor !== null ? valor - item.precoCatalogo : null
  const ehAvulso = !item.servicoId
  const prefixo = `item-${indice}`

  return (
    <li className="rounded-xl border border-slate-200 bg-white p-3">
      <div className="flex items-start gap-2">
        {ehAvulso ? (
          <input
            {...register(`itens.${indice}.descricao`)}
            aria-label="Descrição do item avulso"
            aria-invalid={!!erros?.descricao}
            placeholder="Descrição do item"
            className={`${classeDoCampo} mt-0 h-11 flex-1`}
          />
        ) : (
          <p className="flex-1 pt-1 font-medium">{item.descricao}</p>
        )}
        <button type="button" onClick={aoRemover} aria-label={`Remover ${item.descricao || 'item'}`} className="flex size-11 shrink-0 items-center justify-center rounded-lg text-slate-400 hover:bg-slate-100 hover:text-red-600">
          <Trash2 className="size-5" aria-hidden />
        </button>
      </div>
      {erros?.descricao && <p className="text-sm text-red-600">{erros.descricao.message}</p>}

      <div className="mt-2 flex items-end gap-3">
        {/* − e + em vez de digitar: mais fácil com uma mão */}
        <div>
          <span className="block text-xs text-slate-500">Qtd.</span>
          <div className="mt-1 flex items-center rounded-lg border border-slate-300">
            <button type="button" onClick={() => aoMudarQuantidade(Math.max(1, item.quantidade - 1))} disabled={item.quantidade <= 1} aria-label="Diminuir quantidade" className="flex size-11 items-center justify-center text-slate-600 disabled:opacity-30">
              <Minus className="size-4" aria-hidden />
            </button>
            <span className="w-7 text-center font-semibold" aria-live="polite">{item.quantidade}</span>
            <button type="button" onClick={() => aoMudarQuantidade(item.quantidade + 1)} aria-label="Aumentar quantidade" className="flex size-11 items-center justify-center text-slate-600">
              <Plus className="size-4" aria-hidden />
            </button>
          </div>
        </div>
        <div className="min-w-0 flex-1">
          <label htmlFor={`${prefixo}-valor`} className="block text-xs text-slate-500">Valor unitário (R$)</label>
          <input
            id={`${prefixo}-valor`}
            {...register(`itens.${indice}.valor`, {
              // Ao sair do campo, "150" vira "150,00": fica no mesmo formato dos outros itens.
              onBlur: (e) => {
                const lido = lerValorEmReais(e.target.value)
                if (lido !== null) aoFormatarValor(formatarDecimal(lido))
              },
            })}
            inputMode="decimal"
            placeholder="0,00"
            aria-invalid={!!erros?.valor}
            className={`${classeDoCampo} h-11 text-right font-semibold`}
          />
        </div>
      </div>
      {erros?.valor && <p className="mt-1 text-sm text-red-600">{erros.valor.message}</p>}

      <div className="mt-2 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs">
        {faixa && <span className="text-slate-500">Catálogo: {formatarFaixa(faixa.precoMinimo, faixa.precoMaximo)}</span>}
        {ajuste !== null && ajuste !== 0 && (
          <span className={`rounded-full px-2 py-0.5 font-semibold ${ajuste > 0 ? 'bg-amber-100 text-amber-900' : 'bg-blue-100 text-blue-800'}`}>
            {ajuste > 0 ? '+' : '−'}{formatarMoeda(Math.abs(ajuste))} pelo estado
          </span>
        )}
        {valor !== null && item.quantidade > 1 && <span className="text-slate-600">= {formatarMoeda(valor * item.quantidade)}</span>}
      </div>

      {mostrarObservacao ? (
        <div className="mt-2">
          <label htmlFor={`${prefixo}-obs`} className="block text-xs text-slate-500">Estado do veículo</label>
          <input
            id={`${prefixo}-obs`}
            {...register(`itens.${indice}.observacao`)}
            placeholder="Ex.: muitos pelos de animal"
            className={`${classeDoCampo} h-11`}
          />
          {erros?.observacao && <p className="mt-1 text-sm text-red-600">{erros.observacao.message}</p>}
        </div>
      ) : (
        <button type="button" onClick={() => setMostrarObservacao(true)} className="mt-1 flex h-9 items-center gap-1 text-sm text-marca-700">
          <MessageSquareText className="size-4" aria-hidden /> Observação do estado
        </button>
      )}
    </li>
  )
}

/** Versão só de leitura (orçamento aprovado, recusado ou expirado). */
export function ResumoDoOrcamento({ orcamento: o }: { orcamento: Orcamento }) {
  return (
    <div className="mt-4 space-y-4">
      <div className="rounded-xl border border-slate-200 bg-white px-4 py-3">
        <p className="font-medium">{o.clienteNome}</p>
        <p className="text-sm text-slate-500">
          <span className="font-mono font-semibold">{formatarPlaca(o.placa)}</span> {descreverVeiculo(o)}
        </p>
      </div>
      <ul className="divide-y divide-slate-100 overflow-hidden rounded-xl border border-slate-200 bg-white">
        {o.itens.map((i) => (
          <li key={i.id} className="flex gap-3 px-4 py-3">
            <div className="min-w-0 flex-1">
              <p className="font-medium">{i.quantidade > 1 && `${i.quantidade}× `}{i.descricao}</p>
              {i.observacaoEstado && <p className="text-sm text-slate-500">{i.observacaoEstado}</p>}
            </div>
            <p className="shrink-0 font-semibold">{formatarMoeda(i.total)}</p>
          </li>
        ))}
      </ul>
      <dl className="space-y-1 rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm">
        {o.valorDoDesconto > 0 && (
          <>
            <div className="flex justify-between"><dt className="text-slate-500">Subtotal</dt><dd>{formatarMoeda(o.subtotal)}</dd></div>
            <div className="flex justify-between"><dt className="text-slate-500">Desconto</dt><dd>− {formatarMoeda(o.valorDoDesconto)}</dd></div>
          </>
        )}
        <div className="flex justify-between text-lg font-bold"><dt>Total</dt><dd>{formatarMoeda(o.total)}</dd></div>
        <div className="flex justify-between"><dt className="text-slate-500">Válido até</dt><dd>{formatarData(o.validade)}</dd></div>
      </dl>
      {o.observacoes && <p className="rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm whitespace-pre-line">{o.observacoes}</p>}
    </div>
  )
}
