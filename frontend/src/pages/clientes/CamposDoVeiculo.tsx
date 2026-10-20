import type { FieldErrors, UseFormRegisterReturn } from 'react-hook-form'
import { Campo, classeDoCampo } from '../../components/formulario'
import type { ValoresDoVeiculo } from './esquemas'

interface Props {
  // Uma função "liga este campo ao formulário". Recebe só o nome do campo: assim serve para qualquer
  // formulário que tenha os 4 campos do veículo (o de cliente novo tem também nome e WhatsApp).
  register: (campo: keyof ValoresDoVeiculo) => UseFormRegisterReturn
  erros: FieldErrors<ValoresDoVeiculo>
  /** Deixa os ids únicos quando há mais de um formulário de veículo na tela (ex.: "novo-veiculo", "veiculo-123"). */
  prefixo: string
}

/** Placa, modelo, cor e ano: o mesmo bloco nos 4 formulários que cadastram veículo. */
export function CamposDoVeiculo({ register, erros, prefixo }: Props) {
  const id = (campo: keyof ValoresDoVeiculo) => `${prefixo}-${campo}`
  return (
    <div className="grid grid-cols-[8rem_1fr] gap-3">
      <Campo id={id('placa')} rotulo="Placa" erro={erros.placa?.message}>
        <input id={id('placa')} {...register('placa')} autoCapitalize="characters" placeholder="ABC1D23" aria-invalid={!!erros.placa} className={`${classeDoCampo} uppercase`} />
      </Campo>
      <Campo id={id('modelo')} rotulo="Modelo" erro={erros.modelo?.message}>
        <input id={id('modelo')} {...register('modelo')} placeholder="Ex.: Civic 2019" aria-invalid={!!erros.modelo} className={classeDoCampo} />
      </Campo>
      <Campo id={id('anoFabricacao')} rotulo="Ano (opcional)" erro={erros.anoFabricacao?.message}>
        <input id={id('anoFabricacao')} {...register('anoFabricacao')} inputMode="numeric" maxLength={4} placeholder="2019" aria-invalid={!!erros.anoFabricacao} className={classeDoCampo} />
      </Campo>
      {/* A cor ajuda a oficina a achar a tinta certa (pedido da equipe) */}
      <Campo id={id('cor')} rotulo="Cor (opcional)" erro={erros.cor?.message}>
        <input id={id('cor')} {...register('cor')} autoCapitalize="words" placeholder="Ex.: Prata" aria-invalid={!!erros.cor} className={classeDoCampo} />
      </Campo>
    </div>
  )
}
