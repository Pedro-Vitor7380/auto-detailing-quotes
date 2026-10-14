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

### O que ficou para depois: filtro automático por oficina
- O plano prevê um **filtro global** (`HasQueryFilter`) para cada consulta só enxergar os dados da oficina do usuário logado.
- Ele depende de saber **quem está logado** (passo 7) e só faz sentido quando existirem dados da oficina para proteger (serviços, clientes). Ficou para a semana 2.

---

## Passo 6 — Criar a oficina e o usuário (seed)

### Seed
- **O que é:** inserir os dados iniciais de que o sistema precisa para funcionar. Aqui, a primeira oficina e o primeiro usuário.
- **Por que um comando de terminal, e não uma tela de "cadastre-se":** o app tem um único usuário. Uma tela pública de cadastro deixaria **qualquer pessoa na internet** criar uma conta. Com o comando, **só quem tem acesso ao banco** cria usuários.
- O mesmo comando vai criar o usuário no banco de **produção** (passo 11): basta rodá-lo com a connection string do Neon.

### Como usar
```bash
cd backend
dotnet run --project src/Orcamentos.Api -- criar-usuario
```
- O `--` separa os argumentos do `dotnet run` dos argumentos **da nossa aplicação**. Tudo depois do `--` chega no `args` do `Program.cs`.
- Se ainda não existir oficina, o comando pergunta o nome e cria. Se já existir, usa a existente.
- A senha é digitada **sem aparecer na tela** (aparecem `*`).
- Termina com **código de saída** `0` (sucesso) ou `1` (erro). É assim que scripts e automações sabem se um comando funcionou.

### Modo comando no `Program.cs`
- Se o primeiro argumento for `criar-usuario`, o `Program.cs` executa o comando e **encerra**, sem subir o servidor web.
- Ele reaproveita **toda a configuração da API** (connection string, injeção de dependência, hasher). Não é preciso um projeto separado só para isso.
- **Escopo manual (`CreateScope`):** o `AppDbContext` é *Scoped* (um por requisição HTTP). Fora de uma requisição, criamos o escopo na mão e o descartamos com `using` no final.

### Hash de senha com `PasswordHasher`
- **Regra de ouro:** nunca invente sua própria criptografia. Usamos o `PasswordHasher` do ASP.NET Core Identity, testado e revisado por especialistas.
- **Como funciona:** algoritmo **PBKDF2-SHA512** com **100.000 iterações** e um **salt** aleatório.
  - **Salt:** um valor aleatório misturado à senha antes do hash. Duas pessoas com a mesma senha ficam com hashes **diferentes**, o que impede ataques com tabelas prontas de hashes conhecidos.
  - **Iterações:** o hash é calculado 100.000 vezes de propósito. Para conferir **uma** senha no login, isso leva milissegundos. Para quem tenta **bilhões** de senhas num banco vazado, fica caro demais.
- O hash fica guardado assim: `AQAAAAIAAYag...`. O começo diz a versão do algoritmo, e o resto contém o salt e o hash.
- **Onde:** `backend/src/Orcamentos.Infrastructure/Seguranca/HasherDeSenha.cs`
- **Mudança no plano:** o hasher estava previsto para o passo 7 (login), mas o seed já precisa dele. Por isso entrou aqui.
- **`AddSingleton`:** o hasher não guarda estado, então uma única instância para a aplicação inteira basta.

### Transação no `SaveChangesAsync`
- O `Add` só **marca** o que deve ser salvo. Nada vai para o banco até o `SaveChangesAsync()`.
- Quando ele é chamado, a oficina nova e o usuário são gravados **juntos, numa transação**: ou salva tudo, ou nada. Nunca fica uma oficina sem usuário por causa de um erro no meio.

### Testar de verdade revela problemas
- Rodamos o comando em 4 cenários (sucesso, e-mail repetido, senha curta/confirmação errada, e-mail inválido) e encontramos **3 problemas**, que foram corrigidos:
  1. **Logs de SQL no meio das perguntas.** A 1ª tentativa de correção (`SetMinimumLevel`) **não funcionou**, porque os níveis do `appsettings.Development.json` têm prioridade sobre ele. A correção certa foi `AddFilter`. **Lição:** confirme se a correção funcionou, em vez de supor.
  2. **E-mail inválido só era recusado depois de digitar a senha duas vezes.** Agora é validado logo após ser digitado.
  3. **A mensagem final mostrava o e-mail como digitado, não como foi gravado.**
- Os problemas 2 e 3 tinham a mesma causa: o comando **repetia** a regra de normalizar o e-mail. A solução foi o método `Usuario.NormalizarEmail(...)`, a **fonte única** da regra, usada pela entidade e pelo comando. Se a regra mudar, muda num lugar só.
- **Testando com entrada simulada:** `printf "resposta1\nresposta2\n" | dotnet run ...` envia as respostas como se fossem digitadas. Por isso o código checa `Console.IsInputRedirected`: sem teclado, não dá para esconder a senha.

### Pegadinha: "o arquivo é bloqueado por Orcamentos.Api"
- **Sintoma:** `dotnet build` falha com `MSB3027: não foi possível copiar ... O arquivo é bloqueado por: "Orcamentos.Api (número)"`.
- **Causa:** a API está **rodando** em algum terminal e usando as DLLs que o build quer substituir. O Windows não deixa sobrescrever um arquivo em uso.
- **Solução:** vá ao terminal da API e aperte `Ctrl+C`, depois rode o build de novo. **Não é erro no código.**

---

## Passo 7 — Login com JWT

### Autenticação × Autorização
- **Autenticação:** *quem é você?* O login confere e-mail e senha e entrega o token.
- **Autorização:** *você pode fazer isso?* A cada requisição, a API decide se o dono do token pode acessar aquele endpoint.
- No pipeline: `app.UseAuthentication()` lê o token e descobre o usuário, e `app.UseAuthorization()` aplica as regras de acesso. **A ordem importa:** primeiro identificar, depois autorizar.

### JWT (JSON Web Token)
- **O que é:** um "crachá digital" em texto, com 3 partes separadas por ponto: `cabeçalho.conteúdo.assinatura`.
  - **Cabeçalho:** o algoritmo da assinatura (HMAC-SHA256).
  - **Conteúdo (*payload*):** as **claims**, ou seja, as informações do crachá.
  - **Assinatura:** um código calculado com a **chave secreta** da API.
- **Como a API confia no token:** ela recalcula a assinatura com a chave secreta. Se alguém alterar **qualquer** caractere do conteúdo, a assinatura não bate mais e o token é recusado. **Testamos isso:** um token com os últimos 5 caracteres trocados recebeu **401**.
- **Stateless:** a API não guarda sessão nenhuma. O token carrega tudo, e a assinatura prova que ele é legítimo. Isso é ótimo para hospedagem grátis, que pode reiniciar a API a qualquer momento sem derrubar o login de ninguém.
- **⚠️ O conteúdo NÃO é criptografado, só assinado.** Qualquer um pode **ler** o conteúdo de um JWT (decodificamos o nosso no teste e apareceram o e-mail, o nome e o `oficina_id`). Por isso **nunca** coloque senha ou dado sensível nas claims.

### Claims do nosso token
| Claim | Significado | Exemplo |
|---|---|---|
| `sub` (*subject*) | Id do usuário | `01a13b07-...` |
| `email`, `name` | e-mail e nome | |
| `oficina_id` | oficina do usuário (claim **nossa**) | `01a13b00-...` |
| `iss` (*issuer*) | quem emitiu | `orcamentos-api` |
| `aud` (*audience*) | para quem foi emitido | `orcamentos-app` |
| `exp` | quando expira (segundos desde 1970) | |
| `iat`, `nbf` | quando foi emitido / válido a partir de | |

- `MapInboundClaims = false`: sem isso, o .NET renomeia `sub` para um nome longo e antigo (`http://schemas.xmlsoap.org/...`). Assim os nomes ficam iguais aos do token.

### Chave secreta e configurações
- **`Jwt:Chave`:** 48 bytes aleatórios (`openssl rand -base64 48`) nos **User Secrets**. Quem tiver essa chave consegue **fabricar tokens válidos**, então ela é tão sensível quanto uma senha de administrador. Em produção, vem da variável de ambiente `Jwt__Chave`.
- **`Emissor`, `Audiencia`, `ExpiracaoHoras`:** **não** são segredos, então ficam no `appsettings.json`.
- **Options pattern (`JwtOpcoes`):** a seção `Jwt` da configuração vira uma classe C# com validações (`[Required]`, `[MinLength(32)]`).
  - `ValidateOnStart()`: se faltar a chave ou ela for curta demais, a **API nem sobe**, com uma mensagem clara. É o mesmo princípio de "falhar cedo" da connection string.

### Expiração: 7 dias (uma escolha consciente)
- O token vale **168 horas**. Depois disso, é preciso fazer login de novo.
- **A troca:** um prazo curto é mais seguro (um token roubado vale por pouco tempo), mas obriga a logar toda hora, e no pátio, com o cliente esperando, isso atrapalha. Para **1 usuário**, 7 dias é um equilíbrio razoável.
- A alternativa profissional são os **refresh tokens** (um token curto, renovado automaticamente por outro mais longo). É mais complexo, e fica como melhoria futura.
- **Limitação do JWT:** não dá para "cancelar" um token antes de ele vencer. Se o usuário for **apagado**, porém, o `/me` responde 401, porque confere no banco.

### Seguro por padrão: `FallbackPolicy`
- `SetFallbackPolicy(... RequireAuthenticatedUser() ...)`: **todo** endpoint exige login, a menos que seja marcado com `.AllowAnonymous()`.
- **Por quê:** o erro mais comum é **esquecer** de proteger um endpoint novo. Aqui, esquecer significa "fica protegido", que é o erro seguro.
- **Anônimos hoje:** `/api/health`, `/api/auth/login`, `/openapi` e `/scalar` (esses dois só existem em desenvolvimento).

### Defesas no login
1. **Mensagem única:** "E-mail ou senha inválidos." vale tanto para e-mail inexistente quanto para senha errada. Se a mensagem fosse "e-mail não encontrado", um atacante descobriria quais e-mails estão cadastrados (**enumeração de usuários**).
2. **Mesmo tempo de resposta:** verificar uma senha leva um tempo perceptível (PBKDF2, 100 mil iterações). Se, para um e-mail inexistente, a API respondesse **na hora**, o tempo revelaria se o e-mail existe (**ataque de temporização**). Por isso, sem usuário, verificamos a senha contra um `HashFalso`. **Medimos:** 42 ms × 44 ms.
3. **401 com Problem Details:** o erro sai no formato padrão da web (RFC 9457): `{"type":..., "title":..., "status":401, "detail":...}`. O front-end vai ler sempre o mesmo formato.

### Minimal APIs organizadas: `MapGroup` e `TypedResults`
- **`MapGroup("/api/auth")`:** agrupa os endpoints com o mesmo prefixo e a mesma tag no Scalar.
- **Arquivo por assunto:** `Features/Auth/` tem tudo do login (configuração, gerador de token e endpoints). Quando surgirem serviços e clientes, cada um terá a sua pasta.
- **`TypedResults` + `Results<Ok<...>, ProblemHttpResult>`:** o tipo de retorno **declara** as respostas possíveis. O compilador confere, e o OpenAPI documenta sozinho.
- **`record`:** um tipo enxuto para dados (`record LoginRequest(string Email, string Senha)`). É ideal para o que entra e sai da API, os **DTOs**.
- **`AsNoTracking()`:** avisa ao EF que só vamos **ler**, sem alterar. Ele não precisa "vigiar" o objeto, o que é mais rápido.

### Testando com o arquivo `.http`
- `# @prompt email` e `# @prompt senha`: o REST Client **pergunta** os valores ao enviar. A senha nunca fica gravada no arquivo, que vai para o Git.
- `# @name login` + `{{login.response.body.token}}`: a requisição `/me` reaproveita o token da resposta do login automaticamente.

### Testes manuais realizados (com um usuário temporário, apagado no final)
| Cenário | Resultado |
|---|---|
| `/api/health` sem token | 200 |
| `/api/auth/me` sem token | 401 |
| Login com senha errada | 401 "E-mail ou senha inválidos." |
| Login com e-mail inexistente | 401, mesma mensagem e mesmo tempo |
| Login com e-mail malformado | 401 |
| Login com e-mail em maiúsculas e com espaços | 200 + token |
| `/me` com token válido | 200 com os dados do usuário |
| `/me` com token adulterado | 401 |
| `/me` com token de um usuário que foi apagado | 401 |

---

## Passo 8 — Front-end: a casca responsiva

### As ferramentas
| Ferramenta | Para que serve |
|---|---|
| **Node.js / npm** | O Node roda JavaScript fora do navegador. O npm instala pacotes (como o NuGet do .NET). |
| **Vite** | Cria, roda (`npm run dev`) e empacota (`npm run build`) o front-end. Ao salvar um arquivo, a tela atualiza na hora (*Hot Module Replacement*). |
| **React** | Monta a interface em **componentes**: funções que retornam o "HTML" (JSX) de um pedaço da tela. |
| **TypeScript** | JavaScript **com tipos**. Pega erros antes de rodar, como o compilador do C#. |
| **Tailwind CSS** | Estilos direto no HTML, com classes pequenas (`flex`, `p-4`, `text-lg`). |
| **React Router** | Troca de "página" **sem recarregar** o site (*SPA*, Single Page Application). |
| **lucide-react** | Ícones (casa, documento, pessoas, ferramenta). |
| **oxlint** | Verificador de código (*linter*) que aponta erros comuns e más práticas. |

### Arquivos importantes do projeto `frontend/`
- `package.json`: as dependências e os scripts (`dev`, `build`, `lint`). É o equivalente ao `.csproj`.
- `package-lock.json`: as versões **exatas** instaladas. **Vai para o Git**, para que todo mundo instale o mesmo.
- `node_modules/`: os pacotes baixados (milhares de arquivos). **Não vai para o Git**: é recriado com `npm install`.
- `index.html`: a única página HTML. O React "desenha" tudo dentro de `<div id="root">`.
- `src/main.tsx`: ponto de partida, como o `Program.cs`.
- `vite.config.ts`: a configuração do Vite.

### Componentes, props e JSX
- **Componente:** uma função com nome em PascalCase que retorna JSX: `function InicioPage() { return <section>...</section> }`.
- **JSX:** parece HTML, mas é JavaScript. Diferenças: `className` em vez de `class`, e `{expressao}` para inserir valores.
- **Props:** os parâmetros do componente. `<EmConstrucao titulo="Clientes" previsao="semana 2" />` passa `titulo` e `previsao`, e a `interface EmConstrucaoProps` define os tipos.
- **`.map(...)`:** transforma uma lista de dados numa lista de elementos. Cada item precisa de uma `key` única, para o React saber qual é qual.

### Rotas (`src/rotas.tsx`)
- `createBrowserRouter` define qual componente aparece em cada endereço (`/clientes` → página de clientes).
- **Rotas aninhadas:** todas as páginas são "filhas" do `AppLayout`, e cada uma aparece no `<Outlet />` dele. A casca (menu) é desenhada **uma vez**, e só o miolo troca.
- **`path: '*'`:** qualquer endereço desconhecido mostra "Página não encontrada".
- **`NavLink`:** um link que sabe se está **ativo**. Usamos isso para destacar a aba atual. O `end` na rota `/` evita que "Início" fique ativo em todas as páginas, já que todo endereço começa com `/`.

### Responsividade com Tailwind: *mobile-first*
- Uma classe **sem prefixo** vale para **todas** as telas. Com prefixo (`sm:`, `md:`, `lg:`), passa a valer **a partir daquela largura**.

| Prefixo | A partir de | Exemplo de aparelho |
|---|---|---|
| (nenhum) | 0 px | qualquer celular |
| `sm:` | 640 px | celular deitado |
| `md:` | 768 px | tablet |
| `lg:` | 1024 px | notebook |

- **Exemplos na nossa casca:**
  - Barra inferior: `md:hidden`, que aparece no celular e some a partir do tablet.
  - Menu lateral: `hidden md:flex`, que fica escondido no celular e aparece a partir do tablet.
  - Cards do início: `grid sm:grid-cols-2 lg:grid-cols-3`, ou seja, 1 coluna no celular, 2 no celular deitado e 3 no notebook.
  - Botão "Novo orçamento": `w-full sm:w-auto`, largura total no celular (mais fácil de acertar com o polegar) e largura normal em telas maiores.
- **Por que mobile-first:** o uso principal é no celular, no pátio. Começamos pelo menor e **acrescentamos** para telas maiores.

### Detalhes pensados para o celular
- **Barra inferior:** fica ao alcance do polegar. Na parte de cima da tela, segurando com uma mão, é difícil alcançar.
- **Alvos de toque grandes:** cada aba tem 64 px de altura (`h-16`), e o botão principal tem 56 px (`h-14`). O mínimo recomendado é ~44 px.
- **`pb-24` no conteúdo:** espaço no fim da página para o último item não ficar **escondido atrás** da barra fixa.
- **Áreas seguras (`env(safe-area-inset-bottom)`):** em celulares com "notch" ou com a barrinha de gestos do iPhone, a barra inferior ganha um espaço extra para não ficar embaixo dela. Precisa de `viewport-fit=cover` no `index.html`.
- **`min-h-dvh`:** altura de 100% da tela **visível**. No celular, a barra de endereço do navegador aparece e some, e o `dvh` (*dynamic viewport height*) acompanha isso.

### Cores da marca como "tokens"
- Em `src/index.css`, `@theme` define `marca-50`, `marca-600` etc. Por enquanto, apontam para o azul `sky` do Tailwind.
- O app inteiro usa `bg-marca-600`, e nunca `bg-sky-600` diretamente. Quando as cores saírem da logo, **muda-se só o `index.css`**.

### Testar no celular pela rede (Wi-Fi)
- `server.host: true` no `vite.config.ts` faz o Vite aceitar conexões **de outros aparelhos** da rede, e não só do próprio PC (`localhost`).
- `npm run dev` mostra a linha `Network: http://192.168.x.x:5173`. É esse endereço que se abre no celular.
- **Requisitos:** o celular e o PC na **mesma rede Wi-Fi**, e o firewall do Windows liberando o Node.js.
- `strictPort: true`: se a porta 5173 estiver ocupada, o Vite dá erro, em vez de trocar de porta em silêncio, o que faria o endereço do celular parar de funcionar.

### Como verificamos o layout: a armadilha da captura de tela
- Tiramos capturas com o Edge em modo invisível (*headless*). Na de celular (390 px), o conteúdo **parecia cortado** e a aba "Serviços" sumiu.
- **Antes de "consertar", investigamos:** medimos a largura real que a página recebeu, e ela era **496 px**. O Edge invisível tem uma largura mínima de janela e **recortou** a imagem em 390 px.
- **Solução:** colocar o app dentro de um `<iframe>` de 390 px, que dá à página uma largura real de celular. Aí o layout apareceu correto.
- **Lição:** quando algo parece errado, primeiro confirme se o problema é do **código** ou da **ferramenta de teste**. "Consertar" o layout teria estragado algo que estava certo.

---

## Passo 9 — Tela de login integrada à API

### O problema do `localhost` no celular
- Se o front-end chamasse `http://localhost:5080`, funcionaria no PC, mas **no celular `localhost` é o próprio celular**, onde não há API nenhuma.
- **Em desenvolvimento, a solução é o proxy do Vite** (`server.proxy` no `vite.config.ts`). O app chama só `/api/...`, sem endereço:

```
Celular ──/api/auth/login──► Vite (PC, porta 5173) ──repassa──► API (PC, porta 5080)
```

- O navegador acha que está falando só com o Vite, ou seja, **mesma origem**. Por isso não precisa de CORS em desenvolvimento.
- **Em produção**, o front-end (Vercel) e a API (Render) ficam em endereços diferentes. O endereço da API vem da variável `VITE_API_URL` (veja `frontend/.env.example`).

### CORS (Cross-Origin Resource Sharing)
- **Origem** = protocolo + domínio + porta. `https://app.vercel.app` e `https://api.onrender.com` são origens **diferentes**.
- **Regra do navegador:** um site **não pode** ler respostas de outra origem, a menos que essa outra origem **autorize**. Isso impede que um site malicioso use o seu navegador logado para chamar a sua API.
- **Pré-consulta (*preflight*):** antes de um `POST` com JSON ou com token, o navegador pergunta à API com um `OPTIONS`: "posso?". A API responde com `Access-Control-Allow-Origin: <origem permitida>`.
- **Na nossa API:** as origens permitidas vêm da configuração `Cors:OrigensPermitidas`. Em desenvolvimento, é `http://localhost:5173`; em produção, será o endereço da Vercel.
- **Testamos com curl:**
  - `Origin: http://localhost:5173`: a resposta trouxe `Access-Control-Allow-Origin`.
  - `Origin: https://site-malicioso.com`: a resposta veio **sem** o cabeçalho, e o navegador bloquearia.
- **CORS não protege a API de tudo:** quem usa curl ou Postman não passa pelo navegador e ignora o CORS. Quem protege os dados é o **login**. O CORS protege o **usuário**, dentro do navegador.
- `app.UseCors()` fica **antes** da autenticação: a pré-consulta `OPTIONS` não leva token e não pode ser barrada pelo login.

### O cliente da API (`src/api/cliente.ts`)
- Uma função única, `requisicao<T>(caminho, opcoes)`, usada por **todas** as chamadas. Ela:
  1. junta o endereço base (`VITE_API_URL`) com o caminho;
  2. coloca o cabeçalho `Authorization: Bearer <token>` automaticamente;
  3. converte o corpo para JSON;
  4. transforma erros em `ErroDaApi`, com a mensagem pronta (lida do Problem Details da API);
  5. diferencia **"sem conexão"** (o `fetch` lança erro) de **"a API respondeu com erro"** (`resposta.ok` é falso);
  6. se receber **401** usando um token, avisa o app que a sessão acabou.
- **Por que centralizar:** se cada tela fizesse `fetch` do seu jeito, seria fácil esquecer o token ou tratar erros de forma diferente.
- **`<T>` (genérico):** quem chama diz o tipo da resposta (`requisicao<RespostaLogin>(...)`), e o TypeScript passa a conhecer os campos.

### Onde guardar o token: `localStorage` (e a troca envolvida)
- **`localStorage`:** um armazenamento do navegador que **sobrevive** a recarregar e fechar a página.
- **Alternativa mais segura: cookie `HttpOnly`,** que o JavaScript não consegue ler. Mas com o front-end e a API em domínios diferentes (Vercel × Render), cookies entre sites são bloqueados por vários navegadores, principalmente o Safari do iPhone.
- **O risco do `localStorage`:** se o app tiver uma falha de **XSS** (código malicioso injetado na página), esse código poderia ler o token. A defesa é **não injetar HTML vindo de fora**. O React já escapa todo texto por padrão.
- **Conclusão:** para este projeto, `localStorage` + `Bearer` é a escolha pragmática e é muito usada no mercado. Fica registrado que existe uma alternativa mais segura e por que ela não foi usada.
- **`try/catch` em todo acesso:** em modo anônimo ou com armazenamento bloqueado, o `localStorage` pode lançar erro. O app continua funcionando, só não "lembra" o login.

### Por que guardamos também os dados do usuário (não só o token)
- No plano gratuito do Render, a API "dorme" e pode levar **30 a 60 s** para acordar.
- Se o app esperasse a API confirmar o login a cada abertura, você veria uma tela de carregamento no pátio.
- **Solução:** guardar `{ token, expiraEm, usuario }`. O app abre **na hora** com os dados salvos, e confirma com a API (`/api/auth/me`) em **segundo plano**:
  - se a API responder **401** (token adulterado ou usuário apagado), o app desloga;
  - se estiver **sem internet**, mantém a sessão.
- Quando o `expiraEm` passa, a sessão salva é descartada sem nem chamar a API.

### Context API: o usuário logado disponível no app inteiro
- **O problema:** o menu (nome e botão Sair), a tela de login e a proteção de rotas precisam saber quem está logado. Passar isso por *props* de componente em componente ficaria impraticável.
- **Context:** o `AuthProvider` envolve o app inteiro (`main.tsx`), e qualquer componente chama `useAuth()` para obter `{ usuario, entrar, sair }`.
- **Arquivos separados** (`contexto.ts` com o `useAuth` e `AuthProvider.tsx` com o componente): a regra `only-export-components` do lint pede que arquivos de componentes exportem **só componentes**, para o recarregamento instantâneo do Vite funcionar bem.

### Hooks do React usados
| Hook | Para que serve | Onde usamos |
|---|---|---|
| `useState` | um valor que, ao mudar, redesenha o componente | e-mail, senha, erro, "enviando" |
| `useEffect` | executar algo **depois** de desenhar (ex.: chamar a API) | confirmar a sessão ao abrir o app |
| `useCallback` | manter a **mesma** função entre redesenhos | `entrar` e `sair` |
| `useMemo` | manter o **mesmo** objeto entre redesenhos | o valor do contexto |
| `useContext` | ler um Context | dentro do `useAuth` |
| `useNavigate`, `useLocation` | navegar por código / saber o endereço atual | redirecionar após o login |

- **`StrictMode`:** em **desenvolvimento**, o React executa os efeitos **duas vezes** de propósito, para revelar bugs. Por isso o `/api/auth/me` aparece duplicado nos logs. Em produção, roda uma vez só.

### Proteção de rotas (`RotaProtegida`)
- É um componente que fica "em volta" de todas as rotas do app (`rotas.tsx`). Sem usuário, ele redireciona para `/login` com `<Navigate>`.
- Guarda **de onde a pessoa veio** (`state.voltarPara`). Quem tentou abrir `/clientes` volta para `/clientes` depois do login.
- `replace`: substitui o endereço no histórico, para o botão "voltar" do celular não cair num ciclo login → página → login.
- **⚠️ Isso é só conforto visual, não segurança.** Qualquer pessoa pode mexer no JavaScript do navegador. A segurança real está na **API**, que recusa requisições sem token válido (passo 7).

### A tela de login pensada para o celular
- **`text-base` (16 px) nos campos:** abaixo de 16 px, o Safari do iPhone dá **zoom automático** ao tocar no campo, o que é irritante.
- **`type="email"` + `inputMode="email"`:** o celular mostra o teclado com `@` e `.com`.
- **`autoCapitalize="none"`:** o celular não coloca a primeira letra do e-mail em maiúscula.
- **`autoComplete="email"` / `"current-password"`:** o gerenciador de senhas do celular oferece o login salvo.
- **Botão de mostrar a senha (ícone de olho):** digitar senha em tela pequena gera erros, e ver o que foi digitado ajuda.
- **Botão desabilitado e "Entrando..." durante o envio:** evita tocar duas vezes e mostra que algo está acontecendo, o que é importante quando a API está "acordando".
- **`role="alert"`:** leitores de tela anunciam o erro assim que ele aparece (acessibilidade).
- **`evento.preventDefault()`:** por padrão, enviar um `<form>` **recarrega a página**. Isso impede, e nós mesmos fazemos a chamada à API.

### Teste ponta a ponta (E2E) com Playwright
- **O que é:** um script que **controla um navegador de verdade** (o Edge do PC): digita, clica, recarrega e confere o resultado, como um usuário faria.
- **Rodamos contra o IP da rede** (`http://192.168.3.138:5173`), e não contra `localhost`. É o mesmo caminho do celular, então o teste também prova que o **proxy funciona**.
- **8 cenários, todos aprovados:**
  1. sem login, `/clientes` redireciona para `/login`;
  2. senha errada mostra "E-mail ou senha inválidos.";
  3. o login correto volta para `/clientes`, de onde a pessoa veio;
  4. recarregar a página mantém o login;
  5. abrir `/login` já logado leva para o app;
  6. depois de "Sair", as páginas voltam a exigir login;
  7. um **token adulterado** no `localStorage` faz a API responder 401, e o app volta para o login;
  8. no desktop, o nome do usuário aparece no menu lateral.
- O Playwright simula o tamanho de tela **exato** (390 px), sem a limitação de largura mínima do Edge invisível que vimos no passo 8.
- **As capturas de tela revelaram um bug visual:** no desktop, o botão "Novo orçamento" ocupava a largura inteira. Um elemento `flex` se comporta como **bloco**, e um bloco com largura `auto` ocupa a linha toda. A correção foi `sm:inline-flex`.
- O script ficou numa pasta temporária, fora do projeto. Testes E2E **permanentes** no repositório podem vir depois.
