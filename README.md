# Oficina — App de Orçamentos para Estética Automotiva

Aplicativo web (PWA, mobile-first) para criar orçamentos de serviços de estética automotiva, gerar PDF e enviar ao cliente pelo WhatsApp.

> Projeto real em uso numa oficina e também de estudo/portfólio.
> Definição completa do produto: [PROJETO.md](PROJETO.md) · Diário de aprendizado: [docs/APRENDIZADO.md](docs/APRENDIZADO.md)

## Stack

- **Back-end:** C# / ASP.NET Core (.NET 10), Entity Framework Core, PostgreSQL
- **Front-end:** React + Vite + TypeScript, Tailwind CSS, PWA
- **Infra:** Docker (banco local), deploy gratuito (Neon, Render, Vercel)

## Estrutura

```
backend/    API em ASP.NET Core
frontend/   aplicação React
docs/       documentação e diário de aprendizado
```

## Como rodar

### Pré-requisitos
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js 24+](https://nodejs.org/)

### 1. Banco de dados (PostgreSQL)

```bash
cp .env.example .env      # depois edite o .env e defina uma senha
docker compose up -d      # sobe o PostgreSQL em segundo plano
docker compose ps         # deve mostrar "healthy"
```

### 2. API (back-end)

Configuração inicial (uma vez só):

```bash
cd backend
dotnet tool restore   # instala o dotnet-ef na versão do projeto
# connection string do banco local (use a senha definida no .env)
dotnet user-secrets set "ConnectionStrings:Banco" "Host=localhost;Port=5432;Database=oficina;Username=oficina;Password=<senha>" --project src/Orcamentos.Api
# chave que assina os tokens de login (gera 48 bytes aleatórios)
dotnet user-secrets set "Jwt:Chave" "$(openssl rand -base64 48)" --project src/Orcamentos.Api
# cria as tabelas
dotnet ef database update --project src/Orcamentos.Infrastructure --startup-project src/Orcamentos.Api
# cria a oficina e o seu usuário (pergunta os dados no terminal)
dotnet run --project src/Orcamentos.Api -- criar-usuario
```

Outros comandos de administração (também perguntam os dados no terminal):

```bash
dotnet run --project src/Orcamentos.Api -- alterar-senha    # troca a senha de um usuário
dotnet run --project src/Orcamentos.Api -- alterar-oficina  # nome, CNPJ, telefone e endereço (dados do PDF)
dotnet run --project src/Orcamentos.Api -- importar-catalogo docs/catalogo-inicial.json  # carga inicial do catálogo
dotnet run --project src/Orcamentos.Api -- definir-logo docs/marca/logo-pdf.png  # logo do PDF (ou --remover)
```

Para rodá-los contra o banco de produção, veja `docs/APRENDIZADO.md` (passo 12).

Rodar:

```bash
cd backend
dotnet build                          # compila todos os projetos
dotnet run --project src/Orcamentos.Api  # sobe a API em http://localhost:5080
```

O terminal fica ocupado enquanto a API roda (`Ctrl+C` para parar). Com ela no ar, abra no navegador:

- Documentação interativa: http://localhost:5080/scalar
- Health check: http://localhost:5080/api/health

### 3. App (front-end)

```bash
cd frontend
npm install   # só na primeira vez (ou quando mudarem as dependências)
npm run dev   # sobe o app em http://localhost:5173
```

- **No celular:** com o celular no mesmo Wi-Fi do PC, abra o endereço da linha `Network:` mostrada pelo `npm run dev` (ex.: `http://192.168.x.x:5173`).
- `npm run build`: checa os tipos e gera a versão de produção em `dist/` (com o service worker do PWA).
- `npm run preview`: serve o build de produção em http://localhost:5173. É assim que se testa o PWA (instalar o app), porque com o `dev` o service worker não existe.
- `npm run lint`: verifica o código.

## Produção

```
Vercel (app) ──► Render (API, Docker) ──► Neon (PostgreSQL)
```

O Render e a Vercel publicam automaticamente a cada merge na `main`.

### Variáveis de ambiente

**Render (API):**

| Variável | Valor |
|---|---|
| `ConnectionStrings__Banco` | connection string do Neon (formato .NET) |
| `Jwt__Chave` | 48 bytes aleatórios (`openssl rand -base64 48`), **diferente** da de desenvolvimento |
| `Cors__OrigensPermitidas__0` | endereço do app na Vercel (ex.: `https://xxx.vercel.app`) |

**Vercel (app):**

| Variável | Valor |
|---|---|
| `VITE_API_URL` | endereço da API no Render (ex.: `https://xxx.onrender.com`) |

### Migrations e usuário em produção

As migrations **não** rodam sozinhas no deploy. Depois de criar ou alterar entidades, aplique a partir do PC, apontando para o Neon (veja `docs/APRENDIZADO.md`, passo 11b).

## Fluxo de trabalho (Git)

O projeto segue o **GitHub Flow**: a `main` está sempre funcionando. Cada mudança é feita numa branch (`feat/...`, `fix/...`) e entra na `main` por Pull Request.
