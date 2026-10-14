# Diário de Aprendizado

Registro de cada conceito usado no projeto: **o que é**, **por que usamos** e **onde está no código**.
Organizado pelos passos do desenvolvimento.

---

## Passo 1 — Repositório Git e arquivos de base

### Git
- **O que é:** um sistema de controle de versão. Ele guarda "fotografias" (commits) do projeto ao longo do tempo.
- **Por que usamos:** dá para voltar a qualquer versão anterior, ver o que mudou e quando, e trabalhar sem medo de quebrar algo. No GitHub, o histórico também vira portfólio.
- **Comandos básicos:**
  - `git status`: o que mudou desde o último commit.
  - `git add <arquivo>`: marca mudanças para entrar no próximo commit (a "área de preparação", ou *staging*).
  - `git commit -m "mensagem"`: salva a fotografia.
  - `git log --oneline`: lista os commits.
  - `git push`: envia os commits para o GitHub.
- **Branch `main`:** a linha principal do histórico. Criamos o repositório com `git init -b main`.

### `.gitignore`
- **O que é:** a lista de arquivos e pastas que o Git deve **ignorar**.
- **Por que usamos:**
  - Pastas geradas (`bin/`, `obj/`, `node_modules/`, `dist/`) são recriadas a cada build. Versionar isso só incha o repositório.
  - **Segredos** (`.env`, senhas, chaves) nunca podem ir para o GitHub. Uma senha que já foi para um repositório público deve ser considerada vazada, mesmo que o commit seja apagado depois.
- **Onde:** `/.gitignore`

### `.editorconfig`
- **O que é:** regras de formatação (indentação, fim de linha, codificação) que os editores seguem automaticamente.
- **Por que usamos:** mantém o código com o mesmo formato em qualquer editor. Usamos 4 espaços no C#, que é a convenção da Microsoft, e 2 espaços no resto (JS/TS, JSON, YAML).
- **Onde:** `/.editorconfig`

### `.gitattributes` e quebras de linha (LF × CRLF)
- **O problema:** o Windows termina cada linha com dois caracteres invisíveis (`CRLF`), enquanto Linux e macOS usam um só (`LF`). Sem um padrão, o Git pode mostrar que um arquivo inteiro mudou quando só o fim das linhas mudou.
- **A solução:** o `.gitattributes` com `* text=auto eol=lf` faz o repositório usar sempre `LF`. Como os servidores de deploy rodam Linux, isso também evita surpresas por lá.
- **Onde:** `/.gitattributes`

### Commits pequenos e descritivos
- Cada passo do projeto vira **um commit**. A mensagem descreve **o que** mudou e, se não for óbvio, **por quê**.
- Commits pequenos são fáceis de entender, revisar e desfazer.

### Branches e GitHub Flow
- **O que é uma branch:** uma linha paralela de desenvolvimento. Você trabalha nela sem afetar a `main`.
- **Modelo que usamos (GitHub Flow):**
  1. `git switch -c feat/nome-do-passo`: cria a branch e entra nela.
  2. Faz os commits.
  3. `git push -u origin feat/nome-do-passo`: envia a branch para o GitHub.
  4. Abre um **Pull Request (PR)**: um pedido para juntar a branch na `main`, onde dá para revisar o que mudou.
  5. Faz o **merge** do PR e apaga a branch.
  6. No PC: `git switch main` e `git pull` para trazer a `main` atualizada.
- **Por que usamos:** a `main` vai ser publicada automaticamente em produção (passo 11), então ela precisa estar sempre funcionando. A branch isola experimentos, e o PR serve como revisão do passo e como portfólio.
- **Por que NÃO usamos Git Flow** (`develop`, `release`, `hotfix`): foi feito para equipes com versões programadas. Para um dev solo, é burocracia sem ganho.
- **Prefixos de nome:** `feat/` para funcionalidade nova, `fix/` para correção, `docs/` para documentação, `chore/` para manutenção e configuração.

---

## Passo 2 — PostgreSQL com Docker

### Docker, imagem e container
- **Imagem:** um "molde" pronto com um programa e tudo de que ele precisa. Usamos `postgres:17-alpine`, que é o PostgreSQL 17 sobre o Alpine Linux, uma versão bem pequena.
- **Container:** uma instância rodando a partir da imagem, isolada do Windows. É como uma mini-máquina descartável.
- **Por que usamos:** não precisamos instalar o PostgreSQL no Windows, a versão é a mesma em qualquer máquina e dá para apagar e recriar o banco em segundos.

### Docker Compose (`docker-compose.yml`)
- **O que é:** um arquivo que descreve os containers do projeto, para subir tudo com um comando, em vez de digitar um `docker run` enorme.
- **Partes do nosso arquivo:**
  - `image`: qual imagem usar. Fixamos a versão `17` para não mudar sozinha.
  - `environment`: variáveis que configuram o PostgreSQL (nome do banco, usuário, senha e fuso horário).
  - `ports: "5432:5432"`: liga a porta 5432 do Windows à porta 5432 do container. É assim que a API vai alcançar o banco.
  - `volumes`: veja abaixo.
  - `healthcheck`: o Docker roda `pg_isready` a cada 5s para saber se o banco **já aceita conexões**. "Container ligado" não significa "banco pronto".
  - `restart: unless-stopped`: o banco volta sozinho quando o Docker reinicia.
- **Comandos:**
  - `docker compose up -d`: sobe em segundo plano (`-d` = *detached*).
  - `docker compose ps`: mostra o status.
  - `docker compose logs db`: mostra os logs do banco.
  - `docker compose down`: para e remove o container, mas **mantém os dados**.
  - `docker compose down -v`: remove também o volume e **apaga todos os dados**.

### Volume
- **O problema:** o container é descartável. Se for removido, tudo que estava dentro dele se perde.
- **A solução:** o volume `oficina-db-data` guarda os arquivos do banco **fora** do container, gerenciados pelo Docker. Você pode recriar o container que os dados continuam lá.

### Arquivo `.env` e `.env.example`
- **`.env`:** guarda os valores reais (a senha do banco). **Está no `.gitignore`.** O Docker Compose lê esse arquivo automaticamente e substitui `${POSTGRES_PASSWORD}` no `docker-compose.yml`.
- **`.env.example`:** um modelo **sem senha real**, que vai para o Git. Quem clona o projeto copia para `.env` e preenche.
- **Por que usamos:** é um padrão do mercado para separar **configuração** (no Git) de **segredos** (fora do Git).
- A senha local foi gerada com `openssl rand -hex 16` (32 caracteres aleatórios).

### Acessar o banco pelo terminal
- `docker exec -it oficina-db psql -U oficina -d oficina` abre o `psql` (o terminal do PostgreSQL) **dentro do container**, sem precisar instalar nada no Windows.
- Dentro do `psql`: `\dt` lista as tabelas, `\q` sai.

---

## Pergunta — Como o app funciona no celular, se o Docker roda no PC?

**Resposta curta:** o celular **nunca fala com o Docker**. O Docker é só uma ferramenta de **desenvolvimento**. O app passa por duas fases, e em cada uma o celular chega ao sistema de um jeito diferente.

### Fase 1: desenvolvimento (no PC)

Tudo roda no PC. O celular acessa o PC **pela rede Wi-Fi**, como se o PC fosse um servidor:

```
 Celular (no mesmo Wi-Fi)
    │  abre http://<IP-do-PC>:5173
    ▼
┌──────────────────────────── PC ─────────────────────────────┐
│  Front-end React (Vite) ──► API .NET ──► Docker: PostgreSQL │
│       porta 5173            porta 5080      porta 5432      │
└─────────────────────────────────────────────────────────────┘
```

- O celular só conversa com o **front-end**. O front-end chama a **API**, e só a API conversa com o **banco**.
- Funciona apenas com o celular **no mesmo Wi-Fi** e com o **PC ligado**.
- Serve para testar no celular de verdade durante o desenvolvimento (passo 8).
- O IP do PC aparece no comando `ipconfig` (linha "Endereço IPv4", algo como `192.168.x.x`).

### Fase 2: produção (uso real no pátio)

Cada parte vai para um serviço gratuito na internet, e o PC não precisa mais estar ligado:

```
 Celular (4G ou qualquer Wi-Fi)
    │  abre https://<app>.vercel.app
    ▼
 Vercel (front-end) ──► Render (API .NET) ──► Neon (PostgreSQL)
```

- O **Docker do PC sai de cena**. O banco de produção é o Neon, um PostgreSQL na nuvem.
- O app funciona **de qualquer lugar com internet**, e o celular pode "instalá-lo" como PWA.
- O banco local (Docker) e o de produção (Neon) são **separados**: o que é cadastrado em testes no PC não aparece no app real.

### Por que essa arquitetura funciona nas duas fases

A API não sabe onde o banco está. Ela recebe o endereço **por configuração**: no PC, o endereço aponta para o Docker; em produção, aponta para o Neon. O mesmo vale para o front-end e o endereço da API.

**O código é o mesmo, só a configuração muda.** Esse princípio vem de um conjunto de boas práticas para aplicações web chamado *The Twelve-Factor App* (fator III, "Config").
