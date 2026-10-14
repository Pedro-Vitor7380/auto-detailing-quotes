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

Rodar:

```bash
cd backend
dotnet build                          # compila todos os projetos
dotnet run --project src/Orcamentos.Api  # sobe a API em http://localhost:5080
```

O terminal fica ocupado enquanto a API roda (`Ctrl+C` para parar). Com ela no ar, abra no navegador:

- Documentação interativa: http://localhost:5080/scalar
- Health check: http://localhost:5080/api/health

> As instruções do front-end serão adicionadas a cada etapa.

## Fluxo de trabalho (Git)

O projeto segue o **GitHub Flow**: a `main` está sempre funcionando. Cada mudança é feita numa branch (`feat/...`, `fix/...`) e entra na `main` por Pull Request.
