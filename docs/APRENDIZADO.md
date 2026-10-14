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

---

## Passo 3 — Estrutura do back-end .NET

### Solution (`Orcamentos.slnx`) e projetos (`.csproj`)
- **Projeto (`.csproj`):** uma unidade de código que compila para uma DLL ou um executável. Lista os pacotes e os outros projetos de que depende.
- **Solution (`.slnx`):** agrupa os projetos para compilar e abrir todos juntos. O `.slnx` é o formato novo, em XML legível, que virou padrão no .NET 10. O antigo era o `.sln`.
- **Onde:** `backend/`

### Os 4 projetos e quem depende de quem

```
Orcamentos.Api ──────────► Orcamentos.Infrastructure ──► Orcamentos.Domain
     └──────────────────────────────────────────────►    ▲
Orcamentos.Domain.Tests ────────────────────────────────────┘
```

| Projeto | Responsabilidade | Template |
|---|---|---|
| `Orcamentos.Domain` | Entidades e regras de negócio (ex.: "orçamento aprovado não pode ser editado"). **Não depende de ninguém.** | `classlib` |
| `Orcamentos.Infrastructure` | Detalhes técnicos: banco de dados (EF Core) e PDF. | `classlib` |
| `Orcamentos.Api` | Recebe as requisições HTTP, faz a autenticação e junta tudo. É o executável. | `webapi` |
| `Orcamentos.Domain.Tests` | Testes automatizados das regras do Domínio. | `xunit` |

- **Por que separar:** a regra de negócio é a parte mais valiosa e a que menos deveria mudar. Se um dia trocarmos o PostgreSQL por outro banco, ou a API por outra tecnologia, o Domínio continua igual.
- **O compilador garante a regra:** o Domínio não tem referência para a Infraestrutura, então é **impossível** usar o banco dentro dele. O código nem compila.
- Os comandos usados foram `dotnet new <template>`, `dotnet sln add` e `dotnet add reference`.

### `Directory.Build.props`
- **O que é:** um arquivo que o MSBuild (o sistema de build do .NET) aplica **automaticamente a todos os projetos** da pasta e das subpastas.
- **Por que usamos:** em vez de repetir `net10.0`, `Nullable` etc. em 4 arquivos, a configuração fica num lugar só.
- **`Nullable` + `WarningsAsErrors=nullable`:** o C# avisa quando algo pode ser `null` e você não tratou. Transformamos esses avisos em **erro**, porque o `NullReferenceException` é um dos bugs mais comuns em C#.

### `Program.cs`: o ponto de partida da API
Ele tem três partes, e essa ordem vale para toda API ASP.NET Core:
1. **Registro de serviços** (`builder.Services.Add...`): avisa ao .NET quais "peças" existem (banco, autenticação, OpenAPI). É a **injeção de dependência**, que veremos mais a fundo no passo 5.
2. **Pipeline / middlewares** (`app.Use...`, `app.Map...`): cada requisição passa por essas etapas **na ordem em que foram escritas**.
3. **Endpoints** (`app.MapGet(...)`): qual código responde a cada URL.

### Minimal APIs
- Um endpoint é definido com uma linha, `app.MapGet("/api/health", () => ...)`, em vez de uma classe *Controller*.
- É o estilo moderno, recomendado pela Microsoft para APIs novas e mais simples de ler. Quando houver muitos endpoints, vamos agrupá-los por assunto em `Features/`.

### OpenAPI e Scalar
- **OpenAPI:** um documento JSON (`/openapi/v1.json`) que **descreve** a API: as rotas, os parâmetros e as respostas. É gerado automaticamente a partir do código.
- **Scalar:** lê esse documento e monta uma **página interativa** (`/scalar`) para testar a API pelo navegador. Ele substitui o Swagger UI, que saiu do template padrão do .NET.

### Ambientes: Development × Production
- A variável `ASPNETCORE_ENVIRONMENT` diz em que ambiente a API está rodando.
- O `Program.cs` só mostra o Scalar `if (app.Environment.IsDevelopment())`, porque **não queremos expor a documentação interna em produção**.
- **Na prática:** testando sem o perfil de desenvolvimento, o `/scalar` deu **404**, porque a API rodava como *Production*. Com `Development`, funcionou. Isso mostra que o comportamento muda por configuração, não por código.
- O `launchSettings.json` define o perfil usado pelo `dotnet run`: porta `5080` e ambiente `Development`. **Esse arquivo só existe no desenvolvimento.** Em produção, o ambiente vem de variável de ambiente do servidor.
- **Pegadinha:** a opção `launchBrowser` (abrir o navegador sozinho) **só funciona em IDEs** (Visual Studio, debugger do VS Code). O `dotnet run` pelo terminal **ignora** essa opção. Com `dotnet run`, abra o navegador manualmente em `http://localhost:5080/scalar`.

### A API precisa estar rodando para ser testada
- `dotnet run` **ocupa o terminal** enquanto a API está no ar. A última linha é `Application started. Press Ctrl+C to shut down.` e o terminal parece "travado", mas é assim mesmo: a API está esperando requisições.
- Fechar esse terminal ou apertar `Ctrl+C` **desliga a API**. Aí o navegador e o `.http` dão erro de conexão (`ECONNREFUSED`).
- Para usar comandos enquanto a API roda, abra **outro terminal** (no VS Code, o botão `+` no painel do terminal).

### Por que tiramos o `UseHttpsRedirection`
- O template redireciona HTTP → HTTPS. No nosso caso:
  - em desenvolvimento, rodamos só em HTTP local, o que é mais simples e funciona com o celular na rede (passo 8);
  - em produção, o Render e a Vercel cuidam do HTTPS **na frente** da API. A API recebe a requisição já tratada.
- Ou seja: o HTTPS existe, só não é responsabilidade da API.

### Arquivo `.http`
- `Orcamentos.Api.http` guarda requisições de teste prontas. No VS Code, com a extensão **REST Client**, um clique em "Send Request" envia a requisição. Ele vai substituir o Postman nos testes do login (passo 7).

### `dotnet build`, `dotnet run`, `dotnet test`
- `dotnet build`: compila tudo. **Sempre rode antes de commitar.**
- `dotnet run --project src/Orcamentos.Api`: compila e sobe a API.
- `dotnet test`: roda os testes. Por enquanto diz "nenhum teste disponível", porque eles chegam na semana 3.

---

## Passo 4 — Primeiras entidades do Domínio

### Conflito de nomes: por que os projetos viraram `Orcamentos.*`
- Os projetos se chamavam `Oficina.Api`, `Oficina.Domain`... e queríamos uma **classe** chamada `Oficina`.
- O C# procura os nomes de "dentro para fora". Ao encontrar a palavra `Oficina`, ele acha primeiro o **namespace** `Oficina` e reclama: `error CS0118: "Oficina" é um namespace, mas é usado como um tipo`.
- **Testamos antes de decidir:** criamos um arquivo temporário que usava a classe, vimos o erro acontecer e apagamos o arquivo.
- **Solução escolhida:** renomear os projetos para `Orcamentos.*`, o que o app faz, e manter `Oficina` como nome da entidade, a palavra do negócio.
- **Regra da Microsoft:** não dê a um tipo o mesmo nome de um namespace.
- **`git mv`:** renomeia e avisa o Git, que mostra `R` (*renamed*) e preserva o histórico do arquivo.
- **Commit separado:** a renomeação foi para um commit só dela. Misturar "mudei nomes" com "criei código novo" deixa o histórico difícil de ler.

### Entidade
- **O que é:** um objeto do negócio com **identidade própria**. Dois usuários com o mesmo nome continuam sendo pessoas diferentes, porque o que os distingue é o `Id`.
- **Onde:** `backend/src/Orcamentos.Domain/Entidades/`

### Classe base `Entidade` e herança
- Todas as entidades têm `Id` e `CriadoEm`. Em vez de repetir em cada classe, elas **herdam** de `Entidade` (`class Oficina : Entidade`).
- `abstract`: não dá para criar uma "Entidade" genérica com `new Entidade()`, só as classes concretas (`Oficina`, `Usuario`).

### `EntidadeDaOficina` e `OficinaId` (multi-oficina)
- As entidades que **pertencem** a uma oficina (usuário, cliente, serviço, orçamento) herdam de `EntidadeDaOficina`, que obriga a informar um `OficinaId` válido.
- **Por quê:** quando o app atender várias oficinas, cada dado já sabe a quem pertence. No passo 5, o banco vai usar isso para filtrar os dados automaticamente.
- A própria `Oficina` herda só de `Entidade`, porque ela não pertence a ninguém. Ela **é** a dona.

### Por que `Guid` (e não número sequencial) como `Id`
- `Guid`: um identificador de 128 bits, praticamente impossível de repetir (ex.: `0199e3a4-...`).
- **Vantagens:**
  - **Não é adivinhável.** Com ids 1, 2, 3, alguém poderia tentar `/orcamentos/4` para ver dados de outro. Com Guid isso não funciona. A proteção real continua sendo a autenticação, mas é uma camada a mais.
  - Pode ser **gerado no código**, antes de salvar no banco.
  - Não conflita quando os dados de várias oficinas se misturam.
- **`Guid.CreateVersion7()`** (novo no .NET 9): um Guid que começa pela **data e hora**, então os ids novos ficam "em ordem". Isso deixa os índices do banco mais eficientes que com o `Guid.NewGuid()` totalmente aleatório.
- O **número do orçamento** que o cliente vê (`ORC-2026-0001`) será **outro campo**, sequencial. O `Id` é técnico, o número é para humanos.

### Encapsulamento: `private set` e métodos
- As propriedades têm `{ get; private set; }`: **qualquer um lê, mas só a própria classe altera.**
- Para mudar algo, usa-se um **método** (`AlterarEmail(...)`), e o método **valida** antes de alterar.
- **Por quê:** é impossível existir um `Usuario` com e-mail inválido no sistema. Se a regra estivesse espalhada pelas telas e endpoints, bastaria esquecer em um lugar.
- Isso se chama **invariante**: uma regra que é **sempre** verdadeira para o objeto.

### Construtores
- **Construtor público** `new Usuario(oficinaId, nome, email, senhaHash)`: obriga a informar tudo que é necessário e valida já na criação.
- **Construtor privado vazio** `private Usuario() { }`: o Entity Framework precisa dele para recriar o objeto a partir do banco. É `private` para que o nosso código não consiga criar um usuário vazio.
- **`= null!`:** diz ao compilador que o valor vai ser preenchido (pelo construtor ou pelo EF), mesmo que ele não consiga provar isso sozinho.

### `RegraDeNegocioException`
- Uma exceção **nossa**, lançada quando uma regra de negócio é violada.
- **Por que uma classe própria:** a API vai poder diferenciar "o usuário digitou algo errado" (responder **400** com a mensagem) de "deu um bug" (responder **500**, sem expor detalhes).
- `class RegraDeNegocioException(string mensagem) : Exception(mensagem);` usa **construtor primário** (C# 12): uma forma curta de declarar o construtor.

### Senha: o Domínio só conhece o **hash**
- O `Usuario` guarda `SenhaHash`, **nunca a senha**.
- **Hash:** uma transformação de mão única. Dá para conferir se uma senha bate com o hash, mas não dá para descobrir a senha a partir dele. Se o banco vazar, as senhas não vazam junto.
- Gerar o hash é um detalhe técnico (biblioteca de criptografia), então fica fora do Domínio, no passo 7.

### Testes automatizados (xUnit)
- **O que é:** código que testa o código. Roda com `dotnet test` em segundos e avisa se uma mudança quebrou uma regra.
- **`[Fact]`:** um teste com um único cenário.
- **`[Theory]` + `[InlineData]`:** o **mesmo** teste rodado com vários dados de entrada. No nosso caso, 4 e-mails inválidos diferentes.
- **Padrão AAA:** *Arrange* (preparar), *Act* (agir), *Assert* (verificar).
- **Nome do teste:** `Metodo_Cenario_ResultadoEsperado`. Quando falhar, o nome já diz o que quebrou.
- **`Assert.Throws<T>(...)`:** verifica que o código **lança** a exceção esperada. Testar o caminho de erro é tão importante quanto testar o caminho feliz.
- **Onde:** `backend/tests/Orcamentos.Domain.Tests/Entidades/`

---

## Passo 5 — Entity Framework Core e a primeira migration

### ORM e Entity Framework Core
- **ORM** (*Object-Relational Mapper*): traduz entre **objetos C#** e **tabelas do banco**. Você escreve `db.Usuarios.Where(u => u.Email == email)` e o EF gera o SQL `SELECT ... WHERE email = @p0`.
- **Por que usamos:** menos SQL escrito à mão, consultas checadas pelo compilador e proteção contra **SQL Injection**, porque os valores sempre vão como parâmetros.
- **Pacotes:**
  - `Npgsql.EntityFrameworkCore.PostgreSQL`: o "tradutor" do EF para o PostgreSQL.
  - `EFCore.NamingConventions`: nomes em `snake_case` (`criado_em` em vez de `CriadoEm`). É o padrão do PostgreSQL, e assim não precisamos usar aspas no SQL.
  - `Microsoft.EntityFrameworkCore.Design`: necessário só em tempo de desenvolvimento, para gerar migrations.

### `AppDbContext`
- Representa uma **sessão com o banco**. Cada `DbSet<T>` vira uma tabela (`Oficinas` → `oficinas`).
- **Onde:** `backend/src/Orcamentos.Infrastructure/Dados/AppDbContext.cs`
- Fica na **Infraestrutura**, e não no Domínio: o Domínio não sabe que existe banco.

### Configurações com `IEntityTypeConfiguration<T>` (Fluent API)
- Uma classe por entidade diz **como** guardá-la: tamanho das colunas, obrigatoriedade, índices e chaves estrangeiras.
- **Por que não usar atributos** (`[MaxLength(150)]`) na própria entidade: isso colocaria detalhes de banco dentro do Domínio. Com classes separadas, o Domínio continua "limpo".
- `ApplyConfigurationsFromAssembly`: encontra **todas** as configurações automaticamente. Ao criar uma entidade nova, basta criar a classe de configuração dela.
- **Onde:** `backend/src/Orcamentos.Infrastructure/Dados/Configuracoes/`

### Chave primária, chave estrangeira e índices
- **Chave primária (`pk_`):** identifica cada linha de forma única. É o nosso `Id`.
- **Chave estrangeira (`fk_`):** `usuarios.oficina_id` **precisa** apontar para uma oficina que existe. O banco recusa um usuário com oficina inexistente.
- **`OnDelete(Restrict)`:** o banco **impede** apagar uma oficina que ainda tem usuários. A alternativa `Cascade` apagaria os usuários junto, o que é perigoso demais para dados de negócio.
- **Índice (`ix_`):** como o índice de um livro, deixa as buscas por aquela coluna rápidas.
- **Índice único em `email`:** além de acelerar o login, o **banco garante** que não existam dois usuários com o mesmo e-mail, mesmo que o código tenha um bug. É **global** (não por oficina) de propósito: no login, a pessoa digita só o e-mail, e o sistema precisa achar **um** usuário.

### Migrations
- **O que é:** um arquivo C# com as instruções para mudar a estrutura do banco. `Up()` aplica a mudança e `Down()` desfaz.
- **Por que usamos:** a estrutura do banco fica **versionada no Git**, junto com o código. Em produção (passo 11), as mesmas migrations criam as mesmas tabelas no Neon.
- **Arquivos gerados** (em `Infrastructure/Dados/Migrations/`):
  - `<data>_Inicial.cs`: a migration em si. **Sempre leia antes de aplicar.**
  - `<data>_Inicial.Designer.cs`: metadados (não editar).
  - `AppDbContextModelSnapshot.cs`: a "foto" atual do modelo. O EF compara essa foto com as classes para saber o que mudou na próxima migration.
- **Tabela `__EFMigrationsHistory`:** o EF anota no banco quais migrations já foram aplicadas, para nunca aplicar a mesma duas vezes.
- **Comandos** (rodar dentro de `backend/`):
  ```bash
  # criar uma migration depois de mudar entidades/configurações
  dotnet ef migrations add NomeDaMudanca --project src/Orcamentos.Infrastructure --startup-project src/Orcamentos.Api --output-dir Dados/Migrations
  # aplicar no banco
  dotnet ef database update --project src/Orcamentos.Infrastructure --startup-project src/Orcamentos.Api
  # desfazer a última migration (só se ainda NÃO foi aplicada)
  dotnet ef migrations remove --project src/Orcamentos.Infrastructure --startup-project src/Orcamentos.Api
  ```
- `--project` diz onde ficam as migrations (Infrastructure). `--startup-project` diz de onde ler a configuração (Api, onde está a connection string).

### Ferramenta local (`dotnet-tools.json`)
- O `dotnet-ef` foi instalado como **ferramenta local**: a versão fica registrada em `backend/dotnet-tools.json`, que vai para o Git.
- Quem clonar o projeto roda `dotnet tool restore` e tem **a mesma versão**. Uma instalação global poderia ter versões diferentes em cada PC.

### Connection string e User Secrets
- **Connection string:** o "endereço" do banco: `Host=localhost;Port=5432;Database=oficina;Username=oficina;Password=...`
- Ela contém a **senha**, então **não pode** ir para o `appsettings.json`, que vai para o Git.
- **User Secrets:** o .NET guarda o valor num arquivo **fora do projeto**, em `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`. Em desenvolvimento, a API lê esse arquivo automaticamente, como se fosse parte do `appsettings`.
- O `<UserSecretsId>` no `.csproj` só diz **qual pasta** usar. Ele **não é** segredo.
- **Comandos:**
  ```bash
  dotnet user-secrets list --project src/Orcamentos.Api              # ver
  dotnet user-secrets set "Chave" "valor" --project src/Orcamentos.Api  # definir
  ```
- **Em produção** não existe User Secrets: o valor vem de uma **variável de ambiente** (`ConnectionStrings__Banco`) configurada no Render. O código é o mesmo, só a configuração muda.
- Se a connection string não estiver configurada, a API **para na hora** com uma mensagem explicando o que fazer. Falhar cedo e com uma mensagem clara é melhor do que um erro confuso depois.

### Injeção de dependência (DI)
- **O problema:** o endpoint precisa de um `AppDbContext`, que precisa de uma connection string, que vem da configuração. Quem monta tudo isso?
- **A solução:** registramos **uma vez** "como criar um `AppDbContext`" (`services.AddDbContext<AppDbContext>(...)`). Depois, qualquer endpoint simplesmente **pede** um `AppDbContext` como parâmetro e o .NET entrega pronto.
- Veja no `/api/health`: `async (AppDbContext db, CancellationToken ct) => ...`. Nós não criamos o `db`, ele é **injetado**.
- **`AddInfraestrutura(...)`:** um método de extensão que agrupa os registros da camada. O `Program.cs` fica com uma linha só, e a Infraestrutura decide os próprios detalhes.
- **Tempo de vida:** o `DbContext` é *Scoped*, ou seja, um novo para **cada requisição HTTP**, descartado no fim.

### `async`/`await` e `CancellationToken`
- Ir ao banco é **lento** para o computador, já que é rede. Com `await`, a API **não fica parada esperando**: ela atende outras requisições e volta quando o banco responde.
- **`CancellationToken`:** se o usuário fechar a página no meio, a consulta é cancelada, em vez de gastar recursos à toa.

### Health check com o banco
- O `/api/health` agora testa a conexão (`CanConnectAsync`):
  - banco ok: **HTTP 200** `{"status":"ok","banco":"ok"}`;
  - banco fora: **HTTP 503** (*Service Unavailable*) `{"status":"erro","banco":"inacessível"}`.
- **Testamos os dois casos**, desligando o container com `docker compose stop db`. Testar só o caminho feliz não prova que o erro é tratado.
- Em produção, o Render pode usar esse endpoint para saber se o serviço está saudável.

### O que ficou para o passo 7: filtro automático por oficina
- O plano prevê um **filtro global** (`HasQueryFilter`) para cada consulta só enxergar os dados da oficina do usuário logado.
- Ele depende de saber **quem está logado**, o que só existe após a autenticação (passo 7). Vamos implementá-lo lá.
