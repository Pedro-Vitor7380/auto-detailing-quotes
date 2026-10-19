# Projeto: App de Orçamentos para Estética Automotiva

> Documento de definição do projeto: **o que** o app é, **para quem** é, **o que** faz e **com o quê** será construído.
> As premissas abaixo foram definidas em entrevista em 13/10/2026. Itens marcados com **[A DEFINIR]** ainda dependem de decisão.

---

## 1. Visão geral

Aplicativo web para **criar, enviar e acompanhar orçamentos** de uma oficina de estética automotiva.

O uso principal é **no celular, no pátio, ao lado do carro**: o dono avalia o veículo, monta o orçamento, gera um PDF e envia ao cliente pelo WhatsApp.

O projeto tem **dois objetivos ao mesmo tempo**:
1. **Uso real** na oficina do próprio desenvolvedor.
2. **Portfólio e estudo**, com destaque para um **back-end próprio em C#/.NET**.

## 2. Premissas definidas

| Tema | Decisão |
|---|---|
| Escopo | **Uma oficina agora**, com a estrutura de dados preparada para **várias oficinas depois** (multi-tenant). |
| Usuários | **1 pessoa** (o dono). Perfis/permissões ficam para depois. |
| Desenvolvedor | O próprio dono, nível **intermediário**. |
| Dispositivo principal | **Celular, no pátio**. Também deve funcionar no computador e em qualquer tamanho de tela. |
| Internet | **Sempre online**. Não precisa funcionar offline. |
| Envio ao cliente | **PDF pelo WhatsApp**. O cliente aprova pela conversa e o dono marca o orçamento como aprovado no app. |
| Preço | **Faixa de preço por serviço** (mínimo e máximo), com **ajuste manual por item** conforme o estado do veículo. O item entra no orçamento pelo **mínimo**. Não varia pelo porte. *(Revisado em 19/10/2026: a lista real veio em faixas.)* |
| Catálogo | **~36 serviços**, organizados em categorias. Quando o preço depende da peça (martelinho, retoque, pintura, película), cada peça vira um serviço próprio. *(Revisado em 19/10/2026.)* |
| Pacotes/combos | **Não** existem. |
| Fotos do veículo | **Não** fazem parte do processo. |
| Depois da aprovação | **Só marcar como aprovado**. O app não controla a execução nem a agenda. |
| Relatório | **Faturamento previsto** (soma dos orçamentos aprovados). |
| Validade padrão | **7 dias**. |
| Desconto | **No total** do orçamento (em R$ ou %). |
| Login | **Login simples** (e-mail e senha). |
| Identidade visual | Logo da **CJ Estética Automotiva** (`docs/marca/logo-original.jpg`). Cores do app extraídas dela: **dourado** (`#e0b65a`, `#8a6618`) e **grafite** (`#373334`). Botões dourados levam texto grafite, porque branco sobre dourado não tem contraste suficiente. |
| Prazo do MVP | **1 mês**. |
| Se o prazo apertar | **Corta-se escopo**. O prazo e a arquitetura são mantidos. |
| Custo de hospedagem | **Zero**, só planos gratuitos. |

## 3. Funcionar em qualquer tela

- **Mobile-first**: o layout é desenhado primeiro para o celular (360–480px) e se adapta a tablet (768–1024px) e desktop (1280px ou mais).
- **PWA**: pode ser instalado na tela inicial do celular, sem loja de apps.
- Botões e campos fáceis de tocar (mínimo de ~44px), pensados para uso com uma mão no pátio.
- No celular, navegação por barra inferior; no desktop, menu lateral.
- Navegadores: Chrome, Edge, Safari (iOS) e Firefox, nas versões atuais.

## 4. Funcionalidades do MVP

Ordem de prioridade: se o prazo apertar, os últimos itens saem primeiro.

1. **Login**: e-mail e senha de um único usuário.
2. **Catálogo de serviços**
   - Nome, descrição, categoria, **faixa de preço (mínimo e máximo)**, tempo estimado.
   - Serviços com faixa muito larga viram **variantes** (ex.: "PPF parcial" e "PPF completo").
   - Ativar e desativar serviços.
3. **Clientes e veículos**
   - Cliente: **nome** e **WhatsApp**.
   - Veículo: **placa** e **modelo**. Um cliente pode ter vários veículos.
   - Busca por nome ou placa.
4. **Criação do orçamento** (fluxo pensado para o celular)
   - Escolher o cliente e o veículo, ou cadastrar na hora.
   - Adicionar serviços do catálogo, que entram pelo **preço mínimo**, mostrando a faixa como referência.
   - **Ajustar o valor de cada item** conforme o estado do veículo, com uma observação opcional (ex.: "interior com muitos pelos de animal").
   - Adicionar itens avulsos, fora do catálogo.
   - **Desconto no total** (R$ ou %).
   - Validade automática de **7 dias**, que pode ser editada.
   - Prazo estimado de execução (soma dos tempos dos serviços).
5. **PDF do orçamento**
   - **Logo e dados da oficina.**
   - Itens, valores, desconto e total.
   - **Prazo de execução e validade.**
6. **Envio pelo WhatsApp**: botão para compartilhar o PDF ou abrir a conversa com o cliente.
7. **Status e histórico**
   - `Rascunho → Enviado → Aprovado / Recusado / Expirado`.
   - Expira automaticamente após a validade.
   - Lista com filtro por status e busca por cliente ou placa.
   - Duplicar um orçamento.
8. **Faturamento previsto**: total dos orçamentos aprovados no mês. *(É o primeiro item a sair se o prazo apertar.)*

### Fora do MVP (talvez no futuro)

- Várias oficinas, com cadastro e assinatura (SaaS).
- Vários usuários com perfis de acesso.
- Link para o cliente aprovar online.
- Ordem de serviço e agenda.
- Outros relatórios (taxa de aprovação, serviços mais vendidos).
- Lembretes de retorno (ex.: manutenção da vitrificação).

### Fora do escopo (decidido que não entra)

- Funcionamento offline.
- Fotos do veículo.
- Pacotes ou combos.
- Preço por porte do veículo.

## 5. Regras de negócio

- Cada serviço tem uma **faixa de preço** no catálogo (mínimo ≤ máximo; quando o preço é fixo, mínimo = máximo). No orçamento, cada item guarda o **preço de catálogo** (o mínimo) e o **valor ajustado**, para mostrar quanto o estado do carro alterou o preço.
- O valor ajustado **pode sair da faixa**: a faixa é uma referência, não uma trava.
- Mudar o preço no catálogo **não altera** orçamentos já criados.
- O desconto é aplicado **sobre o total** dos itens.
- Cada orçamento tem um **número sequencial por oficina** (ex.: `ORC-2026-0001`).
- A validade padrão é **7 dias** a partir da criação.
- Orçamentos **Enviados** viram **Expirados** quando passa a validade.
- Orçamento **Aprovado** não pode ser editado, apenas duplicado.
- **Faturamento previsto** = soma do total dos orçamentos aprovados no período.

## 6. Modelo de dados (rascunho)

Todas as tabelas de negócio têm `OficinaId`, para que o sistema já nasça preparado para várias oficinas.

```
Oficina       (Id, Nome, CNPJ, Endereco, Telefone, LogoUrl)
Usuario       (Id, OficinaId, Nome, Email, SenhaHash)
Cliente       (Id, OficinaId, Nome, WhatsApp)
Veiculo       (Id, OficinaId, ClienteId, Placa, Modelo)
Categoria     (Id, OficinaId, Nome)
Servico       (Id, OficinaId, CategoriaId, Nome, Descricao, PrecoMinimo, PrecoMaximo,
               TempoEstimadoMin, Ativo)
Orcamento     (Id, OficinaId, Numero, ClienteId, VeiculoId, Status,
               DescontoTipo [Valor|Percentual], DescontoValor,
               Validade, Observacoes, CriadoEm, EnviadoEm, AprovadoEm)
ItemOrcamento (Id, OrcamentoId, ServicoId?, Descricao, Quantidade,
               PrecoCatalogo, ValorUnitario, ObservacaoEstado)
```

## 7. Arquitetura e stack

| Camada | Tecnologia |
|---|---|
| Front-end | **React + Vite + TypeScript**, Tailwind CSS, PWA (vite-plugin-pwa) |
| Back-end | **C# / ASP.NET Core Web API** (.NET LTS) |
| Acesso a dados | Entity Framework Core + Npgsql |
| Banco de dados | **PostgreSQL** |
| Autenticação | ASP.NET Core Identity ou JWT próprio, com senha em hash |
| Geração de PDF | No back-end, com QuestPDF (licença Community, gratuita para pequenas empresas) |
| Documentação da API | Swagger / OpenAPI |

Arquitetura: o front-end (SPA) consome uma API REST. O back-end tem **3 projetos**:

- **Orcamentos.Api**: endpoints, autenticação e configuração. Os casos de uso ficam aqui, em pastas `Features/<Assunto>`.
- **Orcamentos.Domain**: entidades e regras de negócio, sem depender de banco nem de framework.
- **Orcamentos.Infrastructure**: acesso ao banco (EF Core) e geração de PDF.

Uma camada "Aplicação" separada foi deixada de fora de propósito, para caber em 1 mês. Se o projeto crescer, os casos de uso saem de `Features/` e viram um projeto próprio.

### Hospedagem gratuita (proposta)

| Parte | Opção gratuita |
|---|---|
| Front-end | Vercel, Netlify ou Cloudflare Pages |
| API .NET | Render (container Docker) ou Azure App Service (plano F1) |
| PostgreSQL | Neon ou Supabase (usado só como banco) |

> **Atenção:** nos planos gratuitos, a API pode "dormir" quando fica sem uso, e a primeira requisição pode levar de 30 a 60s. Com o cliente esperando no pátio, isso incomoda. **[A DEFINIR]** Testar o tempo real de resposta e, se necessário, usar um ping periódico ou migrar para um plano barato.

## 8. Requisitos não funcionais

- **Responsivo** em qualquer tela (seção 3).
- **Rápido no celular**: as telas principais devem carregar em menos de 2s em 4G, com a API já acordada.
- **Segurança**: HTTPS, senhas em hash, todo dado filtrado por `OficinaId`.
- **LGPD**: guardar só o necessário (nome, WhatsApp, placa e modelo) e permitir excluir o cliente.
- **Backup**: usar o backup do provedor do banco, conferindo o que o plano gratuito oferece.
- **Localização**: português (Brasil), moeda em R$, datas em dd/mm/aaaa, fuso America/Sao_Paulo.

## 9. Plano do MVP (1 mês)

| Semana | Entrega |
|---|---|
| 1 | Estrutura dos projetos (API + front), banco e migrations, login, deploy inicial (já publicado desde o início) |
| 2 | Catálogo de serviços, clientes e veículos (CRUD + busca) |
| 3 | Criação de orçamento (itens, ajuste por estado, desconto, validade), status |
| 4 | PDF, envio pelo WhatsApp, histórico/filtros, faturamento previsto, ajustes de responsividade |

## 10. Critérios de sucesso do MVP

- Criar e enviar um orçamento completo **pelo celular, no pátio, em menos de 3 minutos**.
- Todo orçamento fica registrado e pode ser encontrado pela placa ou pelo nome do cliente.
- O PDF tem aparência profissional, com a logo da oficina, e é enviado pelo WhatsApp.
- Custo mensal de hospedagem igual a zero.
- Código organizado e com README, pronto para mostrar como portfólio.

## 11. Pendências

1. ~~**Cores do app**~~: resolvido no passo 10 (dourado e grafite, extraídos da logo).
2. **Lista real de serviços**: recebida em 19/10/2026 (`docs/catalogo-servicos.md`). Separado por peça, com preços e tempos estimados (*). **Provisória:** a lista definitiva será alinhada com o pessoal da oficina, que executa os serviços. Como o catálogo é editável pelo app, isso não muda o código.
3. **Dados da oficina para o PDF**: nome, CNPJ, endereço, telefone e redes sociais.
4. ~~**Hospedagem da API**~~: resolvido no passo 11. Render Free (Docker, Virgínia) + Neon (us-east-1) + Vercel. **"Acordar" medido: 25,6 s** após 21 min parado (depois, 0,2 s por requisição).
   - **[A DEFINIR]** Aceitar o tempo, criar um "despertador" no horário de funcionamento ou pagar o plano do Render (~US$ 7/mês). Veja `docs/APRENDIZADO.md`, passo 11e.
5. **Testes automatizados**: definir quanto testar no MVP (sugestão: testes de unidade das regras de preço e desconto).
6. **Comando `alterar-senha`**, para trocar a senha de um usuário pelo terminal.
7. **Nome da oficina em produção** ficou "CJ Estética Automotiva." (com ponto final). Confirmar se corrige.
