# Catálogo de serviços (rascunho)

> Lista real enviada pelo dono em 19/10/2026. É a **carga inicial** do catálogo, importada em 19/10/2026 (passo 15).
> A versão que o programa lê é `docs/catalogo-inicial.json` ("1 dia" = 8 h de trabalho). Depois da carga, o catálogo é editado **pelo app**; este arquivo não é mais atualizado.
> Valores com **\*** são **estimativas** feitas a partir das faixas informadas. Revise antes de usar com clientes.
> Os **tempos médios** também são estimativas (\*) e servem para calcular o prazo de execução no PDF. Os tempos de vitrificação e PPF não incluem o polimento prévio, que entra como item separado.
> **Provisório:** a lista definitiva virá do pessoal da oficina, que executa os serviços no dia a dia. O catálogo será editável pelo próprio app, então ajustar depois não exige mudar código.

## Decisões (19/10/2026)

- **Faixa de preço**: o catálogo guarda **mínimo e máximo**. No orçamento, o item entra pelo **mínimo** e é ajustado conforme o estado do carro, com a faixa como referência. Um serviço de preço fixo tem mínimo = máximo.
- **Variantes por peça**: quando o preço depende **de onde** é o reparo (martelinho, retoque, pintura, película), cada peça vira um **serviço próprio**, por exemplo "Martelinho de Ouro — Capô". Isso é mais simples de construir do que uma tabela de "peças" e cabe no prazo do MVP.
- **Variantes por nível**: a vitrificação varia pela **durabilidade**, não pela peça.
- **Como as estimativas foram feitas**: os serviços que já tinham faixa foram distribuídos dentro dela pelo tamanho da peça. Os que não tinham valor foram posicionados entre os serviços vizinhos (o retoque fica abaixo da pintura completa; o polimento de 1 etapa fica abaixo do polimento completo).

## Funilaria e pintura

### Martelinho de Ouro (por amassado)
Faixa informada: R$ 120 a R$ 350.

| Serviço | Preço mín. | Preço máx. | Tempo médio |
|---|---:|---:|---|
| Martelinho de Ouro — Porta | R$ 120 | R$ 300\* | 1 h 30\* |
| Martelinho de Ouro — Para-lama | R$ 120 | R$ 280\* | 1 h\* |
| Martelinho de Ouro — Capô | R$ 150\* | R$ 350 | 2 h\* |
| Martelinho de Ouro — Teto | R$ 180\* | R$ 350 | 2 h\* |
| Martelinho de Ouro — Tampa traseira / porta-malas | R$ 130\* | R$ 300\* | 1 h 30\* |
| Martelinho de Ouro — Coluna / soleira | R$ 150\* | R$ 350 | 1 h 30\* |

### Retoque Localizado de Pintura (Smart Repair)
Não tinha valor: todos os preços são estimativas.

| Serviço | Preço mín. | Preço máx. | Tempo médio |
|---|---:|---:|---|
| Retoque Localizado — Para-choque (dianteiro ou traseiro) | R$ 150\* | R$ 350\* | 2 h\* |
| Retoque Localizado — Porta | R$ 180\* | R$ 400\* | 2 h\* |
| Retoque Localizado — Para-lama | R$ 150\* | R$ 350\* | 2 h\* |
| Retoque Localizado — Capô | R$ 200\* | R$ 450\* | 2 h 30\* |
| Retoque Localizado — Teto | R$ 200\* | R$ 450\* | 2 h 30\* |
| Retoque Localizado — Retrovisor | R$ 100\* | R$ 200\* | 1 h\* |

### Pintura de peça
Não tinha valor: todos os preços são estimativas.

| Serviço | Preço mín. | Preço máx. | Tempo médio |
|---|---:|---:|---|
| Pintura — Para-choque (dianteiro ou traseiro) | R$ 450\* | R$ 900\* | 1 dia\* |
| Pintura — Para-lama | R$ 400\* | R$ 800\* | 1 dia\* |
| Pintura — Porta | R$ 500\* | R$ 1.000\* | 1 dia\* |
| Pintura — Capô | R$ 600\* | R$ 1.200\* | 1 dia\* |
| Pintura — Tampa traseira / porta-malas | R$ 500\* | R$ 1.000\* | 1 dia\* |
| Pintura — Teto | R$ 700\* | R$ 1.400\* | 1 dia e meio\* |
| Pintura — Retrovisor | R$ 150\* | R$ 300\* | 4 h\* |

## Lavagem e higienização

| Serviço | Preço mín. | Preço máx. | Tempo médio |
|---|---:|---:|---|
| Lavagem Detalhada / Premium | R$ 120 | R$ 180 | 2 h\* |
| Higienização Interna Completa | R$ 600 | R$ 1.000 | 1 dia\* |
| Limpeza Técnica do Motor | R$ 150 | R$ 350 | 1 h 30\* |

## Polimento e proteção

| Serviço | Preço mín. | Preço máx. | Tempo médio |
|---|---:|---:|---|
| Polimento Técnico — 1 Etapa / Correção Leve | R$ 350\* | R$ 700\* | 4 h\* |
| Polimento Técnico Completo (Múltiplas Etapas) | R$ 800 | R$ 2.500 | 1 dia e meio\* |
| Vitrificação Cerâmica — 1 ano | R$ 600 | R$ 1.200\* | 1 dia\* |
| Vitrificação Cerâmica — 3 anos | R$ 1.500\* | R$ 3.000\* | 2 dias\* |
| Vitrificação Cerâmica — 5 anos | R$ 3.000\* | R$ 5.500 | 2 dias\* |

## Películas

### PPF (Película de Proteção)
Faixa informada: R$ 800 a R$ 15.000.

| Serviço | Preço mín. | Preço máx. | Tempo médio |
|---|---:|---:|---|
| PPF — Faróis (par) | R$ 300\* | R$ 600\* | 2 h\* |
| PPF — Capô | R$ 800 | R$ 2.000\* | 1 dia\* |
| PPF — Frontal (capô, para-choque e para-lamas) | R$ 2.500\* | R$ 5.000\* | 2 dias\* |
| PPF — Completo | R$ 8.000\* | R$ 15.000 | 4 dias\* |

### Insulfilm (Película Solar)
Faixa informada: R$ 250 a R$ 800 (carro completo).

| Serviço | Preço mín. | Preço máx. | Tempo médio |
|---|---:|---:|---|
| Insulfilm — Para-brisa | R$ 150\* | R$ 300\* | 1 h\* |
| Insulfilm — Vidros laterais | R$ 150\* | R$ 350\* | 1 h 30\* |
| Insulfilm — Vidro traseiro | R$ 100\* | R$ 250\* | 1 h\* |
| Insulfilm — Carro completo | R$ 250 | R$ 800 | 3 h\* |

## Restauração

| Serviço | Preço mín. | Preço máx. | Tempo médio |
|---|---:|---:|---|
| Restauração de Faróis (par) | R$ 150 | R$ 450 | 2 h\* |

## Resumo

- **36 serviços** em 5 categorias. A premissa inicial era de 10 a 30; com a busca e as categorias, a tela continua viável no celular.
- **Falta:** validar serviços, preços e tempos com o pessoal da oficina (os itens com \* são estimativas).
