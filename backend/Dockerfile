# Receita do container da API (usado pelo Render).
# Build local: docker build -t orcamentos-api ./backend

# ===== Etapa 1: compilar (imagem grande, com o SDK) =====
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primeiro só os arquivos de projeto: enquanto as dependências não mudarem,
# o Docker reaproveita (cache) o "dotnet restore" e o build fica bem mais rápido.
COPY Directory.Build.props Orcamentos.slnx ./
COPY src/Orcamentos.Domain/Orcamentos.Domain.csproj src/Orcamentos.Domain/
COPY src/Orcamentos.Infrastructure/Orcamentos.Infrastructure.csproj src/Orcamentos.Infrastructure/
COPY src/Orcamentos.Api/Orcamentos.Api.csproj src/Orcamentos.Api/
RUN dotnet restore src/Orcamentos.Api/Orcamentos.Api.csproj

# Depois o código, e a publicação em modo Release.
COPY src/ src/
RUN dotnet publish src/Orcamentos.Api/Orcamentos.Api.csproj -c Release -o /app --no-restore

# ===== Etapa 2: rodar (imagem pequena, só com o runtime) =====
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Roda com um usuário comum, não como administrador (root): menos estrago se algo for invadido.
USER $APP_UID

ENTRYPOINT ["dotnet", "Orcamentos.Api.dll"]
