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

> As instruções do back-end e do front-end serão adicionadas a cada etapa.

## Fluxo de trabalho (Git)

O projeto segue o **GitHub Flow**: a `main` está sempre funcionando. Cada mudança é feita numa branch (`feat/...`, `fix/...`) e entra na `main` por Pull Request.
