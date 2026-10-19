import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, LoaderCircle, Power } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useParams } from 'react-router'
import { z } from 'zod'
import { apiCatalogo, chavesCatalogo, type Categoria, type DadosDoServico, type Servico } from '../../api/catalogo'
import {
  AlertaDeErro,
  Campo,
  Carregando,
  classeDoBotaoPrincipal,
  classeDoBotaoSecundario,
  classeDoCampo,
} from '../../components/formulario'
import { formatarDecimal, lerValorEmReais } from '../../lib/formatacao'

// Esquema do formulário: as MESMAS regras do Domínio (Servico.cs), conferidas antes de enviar.
// Assim o usuário vê o erro na hora, embaixo do campo. A API continua validando: o front pode ser burlado.
const esquema = z
  .object({
    nome: z.string().trim().min(1, 'Informe o nome do serviço.').max(120, 'Máximo de 120 caracteres.'),
    categoriaId: z.string().min(1, 'Escolha a categoria.'),
    descricao: z.string().trim().max(500, 'Máximo de 500 caracteres.'),
    precoMinimo: z
      .string()
      .trim()
      .min(1, 'Informe o preço mínimo.')
      .refine((v) => lerValorEmReais(v) !== null, 'Valor inválido. Ex.: 120,00'),
    // Vazio = preço fixo (igual ao mínimo).
    precoMaximo: z.string().refine((v) => v.trim() === '' || lerValorEmReais(v) !== null, 'Valor inválido. Ex.: 180,00'),
  })
  // Regras que envolvem mais de um campo ficam no objeto inteiro; "path" diz embaixo de qual campo o erro aparece.
  .refine(
    (v) => {
      const minimo = lerValorEmReais(v.precoMinimo)
      const maximo = lerValorEmReais(v.precoMaximo)
      return minimo === null || maximo === null || maximo >= minimo // valor inválido já tem a sua própria mensagem
    },
    { message: 'O máximo não pode ser menor que o mínimo.', path: ['precoMaximo'] },
  )

type Valores = z.infer<typeof esquema>

function paraDadosDaApi(v: Valores): DadosDoServico {
  const minimo = lerValorEmReais(v.precoMinimo)!
  return {
    categoriaId: v.categoriaId,
    nome: v.nome,
    descricao: v.descricao || null,
    precoMinimo: minimo,
    precoMaximo: lerValorEmReais(v.precoMaximo) ?? minimo,
  }
}

function paraValoresDoFormulario(s?: Servico): Valores {
  return {
    nome: s?.nome ?? '',
    categoriaId: s?.categoriaId ?? '',
    descricao: s?.descricao ?? '',
    precoMinimo: s ? formatarDecimal(s.precoMinimo) : '',
    precoMaximo: s ? formatarDecimal(s.precoMaximo) : '',
  }
}

/** Rota /servicos/novo (sem id) ou /servicos/:id (edição). Carrega os dados e mostra o formulário. */
export function ServicoFormPage() {
  const { id } = useParams()
  const categorias = useQuery({ queryKey: chavesCatalogo.categorias, queryFn: apiCatalogo.listarCategorias })
  const servico = useQuery({
    queryKey: chavesCatalogo.servico(id ?? ''),
    queryFn: () => apiCatalogo.obterServico(id!),
    enabled: !!id, // na criação não há o que buscar
  })

  const erro = categorias.error ?? servico.error
  const carregando = categorias.isPending || (!!id && servico.isPending)

  return (
    <section className="mx-auto max-w-xl">
      <Link to="/servicos" className="-ml-1 inline-flex h-11 items-center gap-1 text-sm font-medium text-slate-600">
        <ArrowLeft className="size-4" aria-hidden />
        Serviços
      </Link>
      <h1 className="text-2xl font-bold">{id ? 'Editar serviço' : 'Novo serviço'}</h1>

      {erro ? (
        <div className="mt-6">
          <AlertaDeErro erro={erro} />
        </div>
      ) : carregando ? (
        <Carregando />
      ) : (
        // key: se o id mudar, o React recria o formulário do zero com os novos valores iniciais.
        <Formulario key={id ?? 'novo'} servico={servico.data} categorias={categorias.data!} />
      )}
    </section>
  )
}

function Formulario({ servico, categorias }: { servico?: Servico; categorias: Categoria[] }) {
  const navegar = useNavigate()
  const queryClient = useQueryClient()

  // useForm: o React Hook Form guarda os valores e os erros de cada campo, sem um useState por campo.
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<Valores>({ resolver: zodResolver(esquema), defaultValues: paraValoresDoFormulario(servico) })

  // Depois de salvar, busca as listas de novo ANTES de voltar para elas.
  // refetchType 'all': sem ele, só são buscadas as consultas na tela agora; a lista de serviços
  // (que não está aberta) mostraria por um instante o dado antigo ao voltar.
  const atualizarCatalogo = () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: chavesCatalogo.servicos, refetchType: 'all' }),
      queryClient.invalidateQueries({ queryKey: chavesCatalogo.categorias, refetchType: 'all' }),
    ])

  // useMutation: para operações que ALTERAM dados (criar, salvar). Expõe isPending e error para a tela.
  const salvar = useMutation({
    mutationFn: (dados: DadosDoServico) =>
      servico ? apiCatalogo.atualizarServico(servico.id, dados) : apiCatalogo.criarServico(dados),
    onSuccess: async () => {
      await atualizarCatalogo()
      navegar('/servicos')
    },
  })

  const alternarAtivo = useMutation({
    mutationFn: () => apiCatalogo.definirAtivo(servico!.id, !servico!.ativo),
    onSuccess: async () => {
      await atualizarCatalogo()
      navegar('/servicos')
    },
  })

  // Liga cada campo ao formulário e marca aria-invalid (borda vermelha + leitor de tela).
  const campo = (nome: keyof Valores) => ({
    id: nome,
    ...register(nome),
    'aria-invalid': !!errors[nome],
    'aria-describedby': errors[nome] ? `${nome}-erro` : undefined,
  })

  return (
    <form
      onSubmit={handleSubmit((valores) => salvar.mutate(paraDadosDaApi(valores)))}
      noValidate
      className="mt-6 space-y-5 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"
    >
      <Campo id="nome" rotulo="Nome" erro={errors.nome?.message}>
        <input {...campo('nome')} placeholder="Ex.: Martelinho de Ouro — Capô" className={classeDoCampo} />
      </Campo>

      <Campo id="categoriaId" rotulo="Categoria" erro={errors.categoriaId?.message}>
        <select {...campo('categoriaId')} className={classeDoCampo}>
          <option value="">Escolha...</option>
          {categorias.map((c) => (
            <option key={c.id} value={c.id}>
              {c.nome}
            </option>
          ))}
        </select>
      </Campo>

      <fieldset>
        <legend className="text-sm font-medium text-slate-700">Faixa de preço (R$)</legend>
        <div className="mt-1 grid grid-cols-2 gap-3">
          <Campo id="precoMinimo" rotulo="Mínimo" erro={errors.precoMinimo?.message}>
            {/* inputMode="decimal": abre o teclado numérico com vírgula no celular */}
            <input {...campo('precoMinimo')} inputMode="decimal" placeholder="120,00" className={classeDoCampo} />
          </Campo>
          <Campo id="precoMaximo" rotulo="Máximo" erro={errors.precoMaximo?.message}>
            <input {...campo('precoMaximo')} inputMode="decimal" placeholder="Igual ao mínimo" className={classeDoCampo} />
          </Campo>
        </div>
        <p className="mt-1 text-sm text-slate-500">No orçamento, o serviço entra pelo mínimo e você ajusta conforme o carro.</p>
      </fieldset>

      <Campo id="descricao" rotulo="Descrição (opcional)" erro={errors.descricao?.message}>
        <textarea {...campo('descricao')} rows={3} className={`${classeDoCampo} h-auto py-2`} />
      </Campo>

      <AlertaDeErro erro={salvar.error ?? alternarAtivo.error} />

      <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-between">
        {servico ? (
          <button
            type="button"
            onClick={() => alternarAtivo.mutate()}
            disabled={alternarAtivo.isPending}
            className={classeDoBotaoSecundario}
          >
            <Power className="size-4" aria-hidden />
            {servico.ativo ? 'Desativar serviço' : 'Reativar serviço'}
          </button>
        ) : (
          <span />
        )}
        <button type="submit" disabled={salvar.isPending} className={classeDoBotaoPrincipal}>
          {salvar.isPending && <LoaderCircle className="size-5 animate-spin" aria-hidden />}
          {salvar.isPending ? 'Salvando...' : 'Salvar'}
        </button>
      </div>
    </form>
  )
}
