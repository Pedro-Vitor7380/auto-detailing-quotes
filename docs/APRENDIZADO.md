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

---

## Passo 10 — PWA: o app instalável, com a identidade da CJ

### PWA (Progressive Web App)
- **O que é:** um site que pode ser **instalado** como app: ícone na tela inicial, abre em tela cheia (sem a barra do navegador) e abre rápido.
- **Por que usamos:** evita publicar nas lojas da Apple e do Google (caro, demorado e com revisão), e é o mesmo código para celular e computador.
- **Três peças:**
  1. **Manifesto** (`manifest.webmanifest`): o "RG" do app, com nome, ícones, cores e modo de abertura.
  2. **Service worker** (`sw.js`): um script que o navegador roda **em segundo plano**, entre o app e a internet. Ele guarda os arquivos do app no aparelho (*cache*).
  3. **HTTPS:** obrigatório (veja abaixo).
- **`vite-plugin-pwa`** gera o manifesto e o service worker automaticamente no `npm run build`. A configuração fica em `frontend/vite.config.ts`.

### Campos do manifesto
| Campo | Valor | Para que serve |
|---|---|---|
| `name` / `short_name` | "CJ Estética Automotiva — Orçamentos" / "Orçamentos" | O nome curto aparece embaixo do ícone na tela inicial. |
| `display: standalone` | | Abre como app, sem a barra de endereço. |
| `start_url: /` | | Onde o app abre ao tocar no ícone. |
| `theme_color` | `#373334` (grafite) | A cor da barra de status do celular. |
| `background_color` | `#373334` | A cor da tela de abertura (*splash*) enquanto o app carrega. |
| `orientation: portrait` | | O app fica em pé, como é usado no pátio. |
| `icons` | 192, 512 e *maskable* | Os ícones da tela inicial e da tela de abertura. |

### Service worker e cache (Workbox)
- **Pré-cache:** no build, o Workbox lista os arquivos do app (JS, CSS, HTML e ícones: 16 arquivos, ~760 KB) e o service worker os guarda no aparelho. Da 2ª abertura em diante, a "casca" do app carrega **do celular**, e não da internet.
- **`navigateFallback: '/index.html'`:** num SPA, `/clientes` não é um arquivo de verdade. O service worker entrega o `index.html` do cache, e o React Router mostra a página certa.
- **`navigateFallbackDenylist: [/^\/api\//]`:** **a API nunca vem do cache.** Os dados (clientes, orçamentos) sempre vêm do servidor. Como decidido no PROJETO.md, o app **não** funciona offline. Só a casca abre sem internet.
- **`registerType: 'autoUpdate'`:** quando uma versão nova é publicada, o service worker baixa os arquivos novos e o app se atualiza na próxima abertura.
- **⚠️ O service worker só existe no build de produção.** Com `npm run dev`, ele não é gerado. Para testar o PWA, use:
  ```bash
  npm run build && npm run preview
  ```
  O `preview` serve a pasta `dist/` com a mesma porta (5173) e o mesmo proxy `/api` do `dev`.

### HTTPS é obrigatório (e por que o teste no celular fica para o passo 11)
- Service workers só funcionam em **contexto seguro**: páginas com **HTTPS**, ou `localhost`, que é a exceção para quem desenvolve.
- **Verificamos:**

| Endereço | Contexto seguro | Service worker |
|---|---|---|
| `http://localhost:5173` | sim | disponível |
| `http://192.168.3.138:5173` (celular na rede) | **não** | **indisponível** |

- **Consequência:** pelo Wi-Fi local, o celular **não** instala o PWA de verdade. No Android, "Adicionar à tela inicial" cria só um atalho comum. Por isso:
  - **agora**, testamos a instalação **no PC**, pelo `localhost`;
  - **no passo 11**, o app vai para a Vercel com HTTPS, e aí a instalação funciona no celular.
- **Por que essa regra existe:** o service worker fica **entre o app e a internet** e pode alterar qualquer resposta. Numa conexão sem HTTPS, alguém na mesma rede poderia trocar o service worker por um malicioso.

### Ícones: um para cada sistema
| Arquivo | Tamanho | Para quê |
|---|---|---|
| `icon-192.png`, `icon-512.png` | 192/512 | Ícone padrão (`purpose: any`), com a logo redonda e os cantos transparentes. |
| `icon-maskable-512.png` | 512 | **Android:** cada fabricante recorta o ícone num formato (círculo, gota, quadrado arredondado). Só os **80% centrais** são garantidos, então a logo ocupa 78%, sobre fundo grafite. |
| `apple-touch-icon.png` | 180 | **iPhone:** o iOS ignora os ícones do manifesto e usa este, declarado no `index.html`. Ele **não aceita transparência** (vira preto), então tem fundo grafite. |
| `favicon-64.png` | 64 | O ícone da aba do navegador. |
| `logo-256.png` | 256 | A logo exibida na tela de login e na casca. |

- **Como foram gerados:** um script Python com a biblioteca **Pillow**:
  1. abre a logo original (uma página A4 com bordas brancas);
  2. encontra o quadrado da logo (a área "não branca": 1979 × 1979 px);
  3. aplica uma **máscara circular** para os cantos ficarem transparentes. A máscara é desenhada em 4× e reduzida, o que deixa a borda suave (*antialiasing*);
  4. exporta cada tamanho.
- **Conferimos simulando o recorte** de cada sistema (círculo do Android, cantos arredondados do iPhone): a logo ficou inteira em todos.
- A logo original foi movida para `docs/marca/logo-original.jpg`. **Nomes sem espaços** evitam problemas em comandos de terminal.

### Cores da marca e contraste (acessibilidade)
- **Cores extraídas da logo** com o Pillow, pela média dos pixels de cada região: grafite `#373334` e dourado de `#aa7e1f` (escuro) a `#f6d382` (claro).
- **Contraste:** a regra WCAG pede pelo menos **4,5:1** entre o texto e o fundo, para texto normal. Medimos antes de escolher:

| Combinação | Contraste | |
|---|---|---|
| branco sobre dourado médio | 2,50:1 | ❌ ilegível, principalmente **no sol** |
| branco sobre dourado escuro | 3,68:1 | ❌ |
| **grafite sobre dourado claro** `#e0b65a` | 6,53:1 | ✅ botões principais |
| **dourado claro sobre grafite** | 6,53:1 | ✅ o topo do app no celular |
| **dourado escuro `#8a6618` sobre branco** | 5,26:1 | ✅ a aba ativa e os links |

- **Lição:** "trocar o azul pelo dourado" teria deixado o botão principal ilegível. Escolher a cor de **cada combinação** com base no contraste faz o app funcionar bem no pátio.
- Como o app inteiro já usava os tokens `marca-*` (passo 8), a troca de cor foi quase toda num arquivo só, o `src/index.css`. Só os botões mudaram de classe (texto branco → texto grafite).

### Testes do PWA (Playwright, com o build de produção)
| Verificação | Resultado |
|---|---|
| manifesto publicado (`display=standalone`) | ✅ |
| os 3 ícones do manifesto respondem 200 | ✅ |
| service worker ativo, controlando a página | ✅ |
| **sem internet, a tela de login abre** (do cache) | ✅ |
| o navegador considera o app **instalável** | ✅ (com perfil normal) |

- **Pegadinha do teste:** a 1ª verificação de "instalável" falhou com `in-incognito`. O Playwright usa um perfil **anônimo** por padrão, e **navegadores nunca permitem instalar PWAs em janela anônima**. Com um perfil normal, o resultado foi "instalável". De novo: o problema era da **ferramenta de teste**, não do app.

---

## Passo 11 — Deploy: o app na internet

### O desenho de produção
```
Celular (4G ou Wi-Fi) ──HTTPS──► Vercel (app React) ──HTTPS──► Render (API .NET) ──► Neon (PostgreSQL)
```
- **Vercel:** hospeda os arquivos estáticos do front-end (o `dist/`) e entrega o HTTPS pronto.
- **Render:** roda a API num **container Docker**.
- **Neon:** PostgreSQL gerenciado na nuvem, com backup e atualizações por conta deles.
- O **código é o mesmo** do PC. Só a **configuração** muda, via variáveis de ambiente.

### 11a — Preparar o código para produção

#### Docker: imagem, `Dockerfile` e *multi-stage build*
- **`Dockerfile`:** a receita para montar a imagem da API. O Render lê esse arquivo e monta a imagem sozinho a cada deploy.
- **Duas etapas (*multi-stage*):**
  1. **`sdk:10.0`** (grande, com compilador): compila e publica a API (`dotnet publish -c Release`).
  2. **`aspnet:10.0`** (pequena, só com o runtime): recebe **só o resultado** da etapa 1.
  - **Por quê:** a imagem final não carrega o compilador nem o código-fonte. Fica menor (356 MB) e mais segura.
- **Cache de camadas:** copiamos **primeiro só os `.csproj`** e rodamos o `dotnet restore`. Cada instrução do `Dockerfile` vira uma "camada" guardada em cache. Enquanto as dependências não mudam, o Docker reaproveita o restore e só recompila o código.
- **`USER $APP_UID`:** o processo roda como o usuário comum `app` (conferimos com `whoami`), e não como `root` (administrador). Se alguém explorar uma falha da API, tem menos poder dentro do container.
- **`ASPNETCORE_HTTP_PORTS=8080`:** a porta em que a API escuta dentro do container.
- **`.dockerignore`:** o que **não** entra na imagem (`bin/`, `obj/`, testes). É o "`.gitignore` do Docker".
- **Comandos:**
  ```bash
  docker build -t orcamentos-api ./backend              # monta a imagem
  docker run -p 8080:8080 -e "ConnectionStrings__Banco=..." -e "Jwt__Chave=..." orcamentos-api
  ```

#### Configuração por variáveis de ambiente (e o `__`)
- No container não existem User Secrets. A configuração vem de **variáveis de ambiente**.
- O ASP.NET lê variáveis de ambiente como se fossem o `appsettings.json`, trocando `:` por **`__`** (dois sublinhados), porque alguns sistemas não aceitam `:` em nomes de variáveis:

| No `appsettings.json` / User Secrets | Variável de ambiente |
|---|---|
| `ConnectionStrings:Banco` | `ConnectionStrings__Banco` |
| `Jwt:Chave` | `Jwt__Chave` |
| `Cors:OrigensPermitidas[0]` | `Cors__OrigensPermitidas__0` |

- **Prioridade:** a variável de ambiente **sobrescreve** o `appsettings.json`. É assim que o mesmo código usa o banco local no PC e o Neon em produção.

#### Testamos o container no PC **antes** de publicar
| Verificação | Resultado |
|---|---|
| `/api/health` (banco local via `host.docker.internal`) | 200 |
| `/api/auth/me` sem token | 401 |
| login inválido | 401 |
| CORS lendo a origem de `Cors__OrigensPermitidas__0` | ✅ |
| processo rodando como usuário comum | `app` |
| `/scalar` em produção | **401**, não 404 (veja abaixo) |

- **`host.docker.internal`:** dentro do container, `localhost` é **o próprio container**. É o mesmo problema do celular no passo 9! Esse nome especial aponta para o PC, onde está o banco.
- **Por que `/scalar` deu 401, e não 404:** em produção o Scalar não existe, e a `FallbackPolicy` (passo 7) exige login **inclusive para endereços inexistentes**. Quem não está logado recebe 401 para tudo que não é público, **sem descobrir o que existe**. A expectativa estava errada, não o código.

#### Três avisos nos logs do container, analisados um a um
1. **`DataProtection: Storing keys in a directory ... that may not be persisted`:** é a chave que o ASP.NET usa para criptografar **cookies**, e ela se perde quando o container é recriado. **Não nos afeta**, porque usamos JWT, não cookies.
2. **`libgssapi_krb5.so.2: cannot open shared object file`:** o driver do PostgreSQL (Npgsql) tenta, **por padrão**, negociar criptografia via **Kerberos** (GSSAPI), um sistema de login de redes corporativas. A biblioteca não existe na imagem, então ele registrava o erro e seguia sem ela. Funcionava, mas era um **erro falso** em cada conexão, que poluiria os logs de produção.
   - **Correção** (`Infrastructure/DependencyInjection.cs`): `GssEncryptionMode = GssEncryptionMode.Disable`, aplicado **no código**, valendo para qualquer connection string. Depois disso: **0 erros** no log.
3. **`dotnet build` local com 4 erros** (`O arquivo é bloqueado por: Orcamentos.Api`): a API do PC estava rodando (a pegadinha do passo 6). Não era erro de código: a imagem Docker tinha compilado o mesmo código com sucesso. Confirmamos compilando só o projeto alterado.
- **Lição:** um log de produção limpo é valioso. Se ele vive cheio de erros "que não importam", o erro que importa passa despercebido.

#### `vercel.json`: as rotas de um SPA
- Abrir `https://.../clientes` direto, ou recarregar nessa página, faria a Vercel procurar um **arquivo** `clientes`, que não existe: **404**.
- A regra `rewrites` diz: o que não for um arquivo real, entregue o `index.html`, e o React Router mostra a página certa.
- Arquivos que **existem** (`sw.js`, `manifest.webmanifest`, ícones) continuam sendo entregues normalmente, porque a Vercel procura o arquivo antes de aplicar a regra.

### 11b — Banco de produção no Neon

#### Escolhas na criação
- **PostgreSQL 17:** a mesma versão do Docker local. Desenvolvimento e produção iguais evitam surpresas.
- **Região AWS US East 1 (Virgínia):** o Render não tem servidor no Brasil, e o mais próximo que ele oferece também fica na Virgínia. O que mais pesa é a **API perto do banco**, porque cada tela faz várias consultas, e cada consulta paga a distância entre os dois.
- **Conexão direta (sem *pooling*):** o *pooler* do Neon (PgBouncer) é útil com muitas conexões simultâneas. Com 1 usuário, a direta é mais simples e evita incompatibilidades.

#### A connection string de produção
- **Formato .NET** (chave=valor): o Npgsql não entende o formato `postgresql://usuario:senha@host/banco`.
- `SSL Mode=VerifyFull`: além de **criptografar**, o driver **confere o certificado** do servidor, garantindo que é o Neon de verdade e não um impostor no meio do caminho.
- `Channel Binding=Require`: uma proteção extra da senha durante o login no banco.
- Guardada em `backend/.env.producao`, **ignorado pelo Git** (padrão `.env.*` do `.gitignore`, conferido com `git check-ignore`). **Nunca foi colada no chat.**

#### Aplicar migrations em produção a partir do PC
- A connection string entra como **variável de ambiente** só durante o comando. Ela tem **prioridade** sobre o User Secrets, que aponta para o banco local.
- **Primeiro uma consulta só de leitura**, para confirmar o alvo:
  ```bash
  export ConnectionStrings__Banco="$(cat backend/.env.producao)"
  dotnet ef migrations list --project src/Orcamentos.Infrastructure --startup-project src/Orcamentos.Api
  ```
  A resposta `Inicial (Pending)` provou que era o Neon, porque no banco local essa migration já estava aplicada.
- **Depois a aplicação:** `dotnet ef database update ...`.
- **Regra:** antes de qualquer comando que **altera** produção, rode um que só **lê** e confirme que está no lugar certo.

#### Criar o usuário de produção
- Comando `criar-usuario` rodado **pelo próprio usuário**, para a senha nunca passar por outra pessoa:
  ```powershell
  $env:ConnectionStrings__Banco = (Get-Content .env.producao -Raw).Trim()
  dotnet run --project src/Orcamentos.Api -- criar-usuario
  Remove-Item Env:ConnectionStrings__Banco
  ```
- **Sinal de que está no banco certo:** a primeira linha diz "Nenhuma oficina cadastrada", porque o Neon estava vazio. No banco local, diria "Usando a oficina existente".
- **`Remove-Item Env:...` no final:** a variável vale enquanto o terminal estiver aberto. Se ela ficasse definida, um `dotnet run` qualquer, depois, usaria o banco de produção **sem você perceber**.
- **Senha repetida:** a senha de produção ficou igual à local. O risco é baixo (o banco só guarda o hash), mas existe se essa senha for usada em **outros sites**. Ficou anotado: criar um comando `alterar-senha`.

### 11c — API no Render

#### Configuração do serviço
| Campo | Valor | Por quê |
|---|---|---|
| Language | Docker | O Render monta a imagem usando o nosso `Dockerfile`. |
| Root Directory | `backend` | Onde estão o `Dockerfile` e o código da API. |
| Region | Virginia | A mesma região do Neon. |
| Instance | Free | Grátis, mas "dorme" após 15 min sem uso. |
| Health Check Path | `/api/health` | O Render só põe a nova versão no ar se a API **e o banco** responderem. Uma versão quebrada não substitui a que funciona. |
| Auto-Deploy | On Commit | Cada merge na `main` publica sozinho. |

#### Variáveis de ambiente
- `ConnectionStrings__Banco`: a connection string do Neon.
- `Jwt__Chave`: gerada pelo botão **Generate** do Render. **A chave nunca saiu do Render**, nem pelo PC nem pelo chat. É **diferente** da de desenvolvimento: um token gerado no PC não vale em produção.
- `PORT=8080`: diz ao Render em que porta a API escuta (a mesma do `Dockerfile`).

#### "Route GET:/api/health not found": o formato do erro também é informação
- O primeiro teste abriu `https://orcamentos-api.onrender.com` e recebeu `{"message":"Route GET:/api/health not found","error":"Not Found","statusCode":404}`.
- **Esse não é o formato da nossa API.** É o padrão do **Fastify**, um framework de Node.js. A nossa API responderia 401 para um endereço desconhecido.
- **Causa:** o nome `orcamentos-api` já pertencia a outra pessoa no Render, que deu à nossa API o endereço **`orcamentos-api-ukyf.onrender.com`**, com sufixo aleatório. Estávamos abrindo o serviço de outra pessoa.
- **Lição:** sempre confira o endereço **real** que a plataforma atribuiu, e não o que você imagina que ela atribuiu.

### 11d — App na Vercel

- **Root Directory `frontend`**, preset **Vite** detectado sozinho.
- **`VITE_API_URL`:** o endereço da API no Render, **sem barra no final**.
- **⚠️ As variáveis `VITE_` são lidas no BUILD, não ao abrir o app.** O valor é **copiado para dentro** do JavaScript gerado. Mudou a variável na Vercel? É preciso fazer um **novo deploy** para valer.
  - Conferimos isso baixando o JavaScript publicado e procurando o endereço da API dentro dele.
- `http://` é redirecionado para `https://` automaticamente (308).

### 11e — Ligar tudo e testar

#### O bug da barra final no CORS
- **Sintoma:** o login no app publicado dava "erro de conexão".
- **Diagnóstico por eliminação**, testando de fora:

| Suspeita | Teste | Resultado |
|---|---|---|
| API fora do ar | `curl /api/health` | ✅ 200, banco ok |
| Endereço errado no app | procurar o endereço no JavaScript publicado | ✅ correto |
| CORS | `OPTIONS` com `Origin: https://cj-orcamentos.vercel.app` | ❌ **sem** `Access-Control-Allow-Origin` |

- **Para descobrir o que a API aceitava,** enviamos 5 variações da origem. Só `https://cj-orcamentos.vercel.app/`, **com barra**, era aceita. A variável no Render tinha sido salva **com `/` no final**, e o navegador **sempre** envia a origem **sem** a barra.
- **Duas correções:**
  1. **Imediata:** tirar a barra da variável no Render.
  2. **Definitiva, no código:** a API remove espaços e barras finais das origens configuradas (`Program.cs`). Testado no container com `" https://cj-orcamentos.vercel.app/ "`, e funcionou.
- **Lição:** quando um erro vem de **configuração** digitada por humanos, além de corrigir o valor, vale deixar o código **tolerante** ao erro previsível. Ele vai se repetir.
- **Por que o app dizia "erro de conexão":** quando o CORS bloqueia, o navegador **esconde a resposta** do JavaScript. Para o `fetch`, é como se a API nem tivesse respondido, e por isso cai no nosso `catch` de "sem conexão" (passo 9). O detalhe real só aparece no **Console** (F12).

#### Verificações em produção
| Verificação | Resultado |
|---|---|
| CORS liberando só o app da Vercel | ✅ |
| `http` → `https` | ✅ |
| `/clientes` aberto direto | ✅ 200 (rewrite) |
| `/scalar` escondido na API | ✅ 401 |
| **Contexto seguro (HTTPS)** | ✅ diferente do `192.168...` do passo 10 |
| **Service worker ativo e app instalável** | ✅ |
| Navegador → API (login errado mostra a mensagem da API) | ✅ |

#### Teste no celular (feito pelo usuário, no 4G)
- ✅ Instalado na tela inicial com a logo da CJ.
- ✅ Abre em tela cheia, com a barra de status grafite.
- ✅ Login funciona e continua logado depois de fechar e abrir.
- ✅ **Todos os critérios de aceite da semana 1 foram atendidos.**

#### Quanto tempo a API leva para "acordar"
- O Render Free **desliga** a API após 15 min sem requisições. A próxima requisição precisa esperar o container subir de novo (*cold start*).
- **Medição:** depois de 21 minutos sem nenhum acesso:

| Requisição | Tempo |
|---|---|
| 1ª, com a API "dormindo" havia 21 min | **25,6 s** |
| 2ª, logo em seguida | 0,22 s |
| 3ª | 0,22 s |

- **Por que demora:** o Render precisa ligar o container de novo e o .NET precisa iniciar. Parte do tempo também pode vir do **Neon**, que, no plano gratuito, desliga o banco após alguns minutos sem uso, porque o `/api/health` consulta o banco.
- **O que já ameniza (passos 9 e 10):** o app **abre na hora**, porque a casca vem do cache do service worker e a sessão vem do `localStorage`. Só a **primeira chamada à API** depois de um tempo parado espera esses ~25 s. Nesse tempo, o botão mostra "Entrando...", em vez de parecer travado.
- **Opções para o futuro** (decisão em aberto, anotada no PROJETO.md):
  1. **Aceitar:** abrir o app um pouco antes de chegar ao carro.
  2. **"Despertador":** um agendamento (ex.: GitHub Actions) que chama a API a cada ~14 min **no horário de funcionamento** da oficina. Atenção: chamar o `/api/health` também mantém o **banco** acordado e consome as horas gratuitas do Neon. O ideal seria um endpoint que **não** consulte o banco.
  3. **Plano pago do Render** (~US$ 7/mês): a API nunca dorme.
- **Lição:** o risco anotado no PROJETO.md desde o início foi **medido**, em vez de suposto. Agora a decisão pode ser tomada com um número real.

---

# Semana 2

## Passo 12 — Comandos de administração: `alterar-senha` e `alterar-oficina`

### O que foi feito
- **`alterar-senha`**: troca a senha de um usuário pelo terminal. Pede o e-mail e a nova senha duas vezes, sem mostrá-la na tela.
- **`alterar-oficina`**: altera nome, CNPJ, telefone e endereço da oficina, que são os dados do cabeçalho do PDF. Cada pergunta mostra o valor atual entre colchetes; **Enter mantém** e **`-` apaga**.
- As perguntas que os três comandos compartilham saíram do `CriarUsuarioComando` e foram para `Comandos/Terminal.cs`. Isso é uma **refatoração**: o comportamento continua o mesmo, só o código ficou num lugar só. O terceiro comando foi o sinal: com três cópias da mesma leitura de senha, uma correção feita em uma delas seria esquecida nas outras.

### Por que `alterar-senha` não pede a senha atual
Quem roda o comando já tem a **connection string** do banco, ou seja, já tem acesso total aos dados. Pedir a senha antiga não protegeria nada. Isso é um **reset de administrador**. Quando existir uma tela "trocar minha senha" dentro do app, aí sim a senha atual será pedida, porque quem usa o app não tem acesso ao banco.

### Escolher o comando com `switch`
O `Program.cs` olha o primeiro argumento e escolhe a função do comando:
```csharp
Func<IServiceProvider, Task<int>>? comando = args.FirstOrDefault() switch
{
    CriarUsuarioComando.Nome => CriarUsuarioComando.ExecutarAsync,
    AlterarSenhaComando.Nome => AlterarSenhaComando.ExecutarAsync,
    AlterarOficinaComando.Nome => AlterarOficinaComando.ExecutarAsync,
    _ => null,   // nenhum comando: sobe o servidor web normalmente
};
```
- `Func<IServiceProvider, Task<int>>` é o **tipo de uma função**: ela recebe os serviços e devolve, de forma assíncrona, um número (o código de saída). Guardamos **a função em si**, sem executá-la.
- `switch` como **expressão** (C# 8+) devolve um valor. É um jeito mais curto de fazer uma série de `if/else`.
- Um comando novo é **uma linha** a mais aqui.

### Como o EF Core sabe o que atualizar
O `alterar-senha` busca o usuário, chama `usuario.DefinirSenhaHash(...)` e depois `SaveChangesAsync()`. Não existe nenhum "update" escrito à mão, porque o EF **rastreia** (*change tracking*) os objetos que ele mesmo carregou. No `SaveChanges`, ele compara com os valores originais e gera um `UPDATE` só da coluna `senha_hash`.

### Rodar um comando contra a produção
O mesmo comando funciona com o banco do Neon, trocando só a configuração. No Git Bash:
```bash
cd backend
export ConnectionStrings__Banco="$(tr -d '\r\n' < .env.producao)"
dotnet run --project src/Orcamentos.Api --no-launch-profile -- alterar-senha
unset ConnectionStrings__Banco
```
No PowerShell:
```powershell
cd backend
$env:ConnectionStrings__Banco = (Get-Content .env.producao -Raw).Trim()
dotnet run --project src/Orcamentos.Api --no-launch-profile -- alterar-senha
Remove-Item Env:ConnectionStrings__Banco
```
- O `--no-launch-profile` deixa de usar o `launchSettings.json`. Sem ele, o ambiente é `Production` e os **User Secrets** do banco local não são carregados, então não há risco de alterar o banco errado.
- Foi assim que o nome da oficina em produção foi corrigido: de `CJ Estética Automotiva.` para `CJ Estética Automotiva`, sem o ponto final.

### Testes feitos
| Caso | Resultado |
|---|---|
| E-mail que não existe | ✅ "Nenhum usuário com esse e-mail.", código 1, nem pede a senha |
| E-mail com maiúsculas (`Teste.Temp@Exemplo.com`) | ✅ encontra o usuário (mesma normalização do login) |
| Senha curta / confirmação diferente | ✅ pergunta de novo |
| Login com a **senha nova** | ✅ 200 |
| Login com a **senha antiga** | ✅ 401 |
| `alterar-oficina` só com Enter | ✅ nada muda |
| Nome com acento ("Estética") vindo pelo terminal | ✅ gravado corretamente em UTF-8 |

Os testes de senha usaram um **usuário temporário**, apagado no final. O seu usuário não foi tocado.

### Pergunta: dá para usar o mesmo login em vários celulares ao mesmo tempo?
> *"Eles são em dois, e quero que tenha apenas um login, da oficina, mas eles usarão um em cada celular. Tem como usar o mesmo login em dispositivos diferentes ao mesmo tempo?"*

**Sim, e já funciona hoje, sem mudar nada.**

- **Por quê:** o login devolve um **token JWT** (passo 7). O servidor **não guarda** quais tokens existem; ele só confere a assinatura de cada token que chega. Cada celular que faz login recebe **o seu próprio token**, e os dois valem ao mesmo tempo, sem "derrubar" um ao outro. Isso se chama autenticação **sem estado** (*stateless*).
- **O que muda na prática:**
  1. **Sair em um celular não desloga o outro.** O "Sair" só apaga o token **daquele aparelho**.
  2. **Trocar a senha não desloga ninguém na hora.** Os tokens já emitidos continuam válidos até expirar (**7 dias**). Se um dia o login vazar ou alguém sair da equipe, troque a senha **e** saiba que o acesso antigo dura até 7 dias. Por isso o `alterar-senha` avisa no final. Se isso virar um problema, a solução conhecida é um **"carimbo de segurança"** (*security stamp*): um valor salvo no usuário e no token, que muda a cada troca de senha e invalida os tokens antigos. Ficou anotado para o futuro.
  3. **Não dá para saber quem fez o quê.** Com um login só, o app não distingue os dois funcionários. Se isso importar um dia (ex.: "quem fez esse orçamento?"), o caminho é **um usuário por pessoa**. O banco já suporta vários usuários por oficina (`Usuario.OficinaId`), então seria basicamente rodar o `criar-usuario` de novo.
  4. **Os dois editando o mesmo orçamento ao mesmo tempo:** quem salvar por último vence. Com duas pessoas, cada uma cuidando de um carro, o risco é baixo. Por isso não vamos tratar isso no MVP.
- **O e-mail do login** pode ser um e-mail da oficina, em vez do seu pessoal. Se quiser, dá para criar esse usuário com o `criar-usuario`.

## Passo 13 — API do catálogo (categorias e serviços)

### O que foi feito
| Camada | Arquivos | O que faz |
|---|---|---|
| Domínio | `Entidades/Categoria.cs`, `Entidades/Servico.cs` | As regras: nome obrigatório, faixa de preço válida, tempo maior que zero |
| Testes | `CategoriaTests.cs`, `ServicoTests.cs` | 15 testes novos (26 no total) |
| Infraestrutura | `Configuracoes/*`, migration `Catalogo`, `IOficinaAtual.cs`, `AppDbContext.cs` | Tabelas, índices, a regra de preço no banco e o **filtro por oficina** |
| API | `Features/Catalogo/*Endpoints.cs`, `Comum/TratadorDeErros.cs`, `Comum/Problemas.cs` | Os endpoints e as respostas de erro |

### Os endpoints
| Método e caminho | O que faz |
|---|---|
| `GET /api/categorias` | Lista em ordem alfabética, com a quantidade de serviços de cada uma |
| `POST /api/categorias` | Cria (nome repetido → **409**) |
| `PUT /api/categorias/{id}` | Renomeia |
| `DELETE /api/categorias/{id}` | Exclui, mas só se não tiver serviços (**409** com a explicação) |
| `GET /api/servicos?busca=&categoriaId=&incluirInativos=` | Lista, ordenado por categoria e nome |
| `GET /api/servicos/{id}` | Um serviço |
| `POST /api/servicos` / `PUT /api/servicos/{id}` | Cria / altera |
| `POST /api/servicos/{id}/ativar` e `/desativar` | Liga e desliga o serviço nos orçamentos |

**Por que serviço não tem `DELETE`:** na semana 3, cada item de orçamento vai apontar para um serviço. Apagar o serviço quebraria os orçamentos antigos. **Desativar** tira o serviço da lista sem perder o histórico. Essa técnica se chama *soft delete* (exclusão lógica). Categoria pode ser excluída porque nada além dos serviços aponta para ela.

### O filtro global: "cada oficina só vê os seus dados"
Este era o ponto adiado desde o passo 7. Funciona em três peças:

1. **O token sabe a oficina.** Desde o passo 7, o JWT leva a claim `oficina_id`.
2. **`IOficinaAtual`** (Infraestrutura) é uma interface que responde "qual é a oficina desta requisição?". A API a implementa em `OficinaAtualDoToken`, lendo a claim pelo `IHttpContextAccessor`. A Infraestrutura não sabe nada de HTTP: ela só conhece a interface. Isso é **inversão de dependência**: a camada de dentro define o que precisa, e a de fora entrega.
3. **`HasQueryFilter`** no `AppDbContext`:
   ```csharp
   modelBuilder.Entity<Servico>().HasQueryFilter(s => s.OficinaId == OficinaIdAtual);
   ```
   A partir disso, **toda** consulta em `Servicos` recebe `WHERE oficina_id = @oficina` automaticamente, até as que esquecermos de filtrar.

O efeito é que **um dado de outra oficina simplesmente "não existe"** para quem está logado. Nos endpoints, nenhuma consulta filtra por oficina à mão. Se alguém tentar ler ou alterar um id de outra oficina, o `FirstOrDefault` devolve `null` e a resposta é **404**. Respondemos 404, e não 403 ("proibido"), para não confirmar que aquele id existe.

Cuidados que vêm junto:
- **Sem login, o filtro não vê nada** (`OficinaId == null` nunca é verdade). Se falhar, falha "fechado", que é o lado seguro.
- **`Usuario` fica de fora de propósito**, porque o login procura o e-mail **antes** de saber a oficina.
- **Ao criar um serviço, a categoria é conferida** com uma consulta filtrada. Sem isso, a oficina B poderia pendurar um serviço na categoria da oficina A só sabendo o id. Esse teste está na tabela abaixo.
- **Cada entidade nova** (Cliente, Orçamento...) **precisa da sua linha** de `HasQueryFilter`. Isso está comentado no código.

### Regras em dois lugares: Domínio e banco
A regra "preço máximo ≥ mínimo" existe no `Servico.Atualizar` **e** numa *check constraint* do PostgreSQL (`ck_servicos_faixa_de_preco`). O Domínio dá a **mensagem amigável**; o banco é a **última linha de defesa**, caso algum código futuro grave sem passar pelo Domínio. O mesmo vale para os índices únicos de nome, que barram duplicatas mesmo que duas requisições cheguem juntas.

Já as regras que **precisam consultar o banco** (nome repetido, categoria existente) ficam no endpoint, porque o Domínio não acessa o banco.

### Dinheiro: `decimal` e `numeric(10,2)`
- Em C#, **`decimal`**, nunca `double`. O `double` guarda em binário e erra contas simples: `0.1 + 0.2` dá `0.30000000000000004`. O `decimal` guarda em base 10, exato para centavos.
- No banco, **`numeric(10,2)`**: exato, com 2 casas, até R$ 99.999.999,99.
- **Arredondamento:** o `decimal.Round` do .NET usa, por padrão, o "arredondamento bancário" (o meio vai para o número **par**, então 10,005 vira 10,00). Para preço, o esperado é o "da escola" (10,005 vira 10,01), por isso usamos `MidpointRounding.AwayFromZero`. Um teste garante isso.

### Tratador global de erros (`IExceptionHandler`)
Antes, um `RegraDeNegocioException` que escapasse virava **500** ("erro interno"). Agora o `TratadorDeErros` intercepta e responde **400** com a mensagem da regra, no mesmo formato ProblemDetails do login:
```json
{ "status": 400, "title": "Dados inválidos", "detail": "O preço máximo não pode ser menor que o mínimo." }
```
Por isso os endpoints não têm `try/catch`: o Domínio lança, o tratador traduz, e o app mostra o `detail` ao usuário. O `app.UseExceptionHandler()` é o **primeiro** middleware do pipeline, para capturar erros de tudo o que vem depois.

### Pegadinhas deste passo
1. **`OrderBy` depois do `select new ServicoResponse(...)` dá erro.** O EF precisa traduzir a consulta inteira para SQL. Depois que o resultado vira um `record` montado pelo construtor, ele não sabe mais qual coluna corresponde a `CategoriaNome`. A correção foi ordenar **antes** do `select` (`orderby c.Nome, s.Nome`).
2. **JSON quebrado respondia 500.** Em `Development`, o ASP.NET **lança** um `BadHttpRequestException` quando não consegue ler o corpo, para aparecer no log; em produção, ele já responderia 400 sozinho. O tratador agora trata os dois casos do mesmo jeito.
3. **O `curl` do Git Bash enviou "Películas" com a codificação do Windows**, não em UTF-8, e a API recusou o texto. Não era um bug da API. O teste passou a ser feito em Python, que sempre envia UTF-8.

### Testes feitos (script Python, contra a API local)
Para provar o isolamento, o teste criou **duas oficinas** com um usuário temporário em cada uma. Tudo foi apagado no final.

| Caso | Resultado |
|---|---|
| Sem token | ✅ 401 |
| Criar categoria com acento / repetida em minúsculas / sem nome | ✅ 201 / 409 / 400 |
| JSON quebrado / id em formato errado | ✅ 400 / 400 |
| Criar serviço / faixa invertida / nome repetido | ✅ 201 / 400 / 409 |
| Busca "INSUL" acha "Insulfilm…" (sem diferenciar maiúsculas) | ✅ |
| **Oficina B lista categorias e serviços** | ✅ listas vazias |
| **B lê, renomeia ou desativa algo de A** | ✅ 404 |
| **B cria serviço na categoria de A** | ✅ 400 "Categoria não encontrada." |
| Excluir categoria com serviço | ✅ 409 com a explicação |
| Desativar: some da lista padrão e aparece com `incluirInativos=true` | ✅ |
| Atualizar com preço 850,555 | ✅ gravado 850,56 |

### Ficou para depois
- **Busca sem acento** ("higienizacao" achar "Higienização"). Precisa da extensão `unaccent` do PostgreSQL. Com 36 serviços e filtro por categoria, não é urgente.
- **Paginação:** o plano citava, mas 36 serviços cabem numa resposta só (alguns KB). Paginar agora seria complexidade sem benefício. Entra quando alguma lista crescer, provavelmente a de orçamentos.

### Como o passo 13 foi testado (sem o seu login)
O teste pelo VS Code não funcionou do seu lado, então foi feito por script, em duas etapas:
1. **O próprio `Orcamentos.Api.http`**, executado por um script que lê o arquivo e roda as requisições em ordem, substituindo `{{baseUrl}}`, `{{login.response.body.token}}` etc., como o REST Client faria. As 10 requisições responderam o esperado (200, 201, 400, 204...). Isso também prova que o arquivo está correto para você usar depois.
2. **O teste de isolamento**, com duas oficinas: 23 de 23 verificações certas. Na primeira tentativa, uma verificação falhou porque sobrou no banco um serviço da etapa 1, desativado pelo `.http`. Era um problema do teste, que não começava com o banco limpo. Com o banco limpo, tudo passou.

Em todos os testes, o login foi feito com **usuários temporários**, criados com `criar-usuario` e apagados no final. A sua senha nunca é usada.

## Passo 14 — Telas do catálogo

### O que foi feito
| Tela | Rota | O que faz |
|---|---|---|
| Lista de serviços | `/servicos` | Busca, filtro por categoria, "mostrar desativados", agrupado por categoria |
| Novo / editar serviço | `/servicos/novo`, `/servicos/:id` | Formulário com validação; na edição, botão desativar/reativar |
| Categorias | `/servicos/categorias` | Criar, renomear e excluir (excluir só sem serviços) |

### TanStack Query: o "cache" das chamadas à API
Até o passo 9, cada tela chamava a API "na mão" com `useState` + `useEffect`, e precisava cuidar sozinha de carregando, erro e dados. O **TanStack Query** faz isso por nós:
```tsx
const servicos = useQuery({ queryKey: ['servicos'], queryFn: apiCatalogo.listarServicos })
// servicos.isPending, servicos.isError, servicos.data
```
- **`queryKey`** é o nome da gaveta onde a resposta fica guardada. Duas telas que pedem `['categorias']` usam a **mesma** resposta, sem chamar a API duas vezes.
- **`staleTime: 30s`**: durante 30 segundos, o dado é "fresco". Ir para outra tela e voltar mostra a lista **na hora**, sem esperar a API. Isso ajuda muito com a API dormindo no Render.
- **`useMutation`** é para o que **altera** dados (criar, salvar, desativar). Ele dá `isPending` (para o botão mostrar "Salvando...") e `error` (para mostrar a mensagem da API).
- **`invalidateQueries`**: depois de salvar, avisa que a gaveta `['servicos']` está desatualizada, e ela é buscada de novo.
- **`retry`**: falha de rede ou do servidor tenta de novo até 2 vezes; erro 4xx (ex.: 409, nome repetido) não, porque repetir não muda o resultado.
- **Ao sair, o cache é apagado** (`queryClient.clear()`). Sem isso, outra conta logada no mesmo celular veria, por um instante, os dados da anterior.

### React Hook Form + Zod: formulários com validação
- **Zod** descreve as regras num **esquema**: `nome: z.string().trim().min(1, 'Informe o nome do serviço.')`. Regras que envolvem dois campos, como "máximo ≥ mínimo", ficam num `.refine` do objeto, com `path` dizendo embaixo de qual campo o erro aparece.
- **React Hook Form** guarda valores e erros sem um `useState` por campo. `register('nome')` liga o campo ao formulário, e `zodResolver(esquema)` faz o formulário usar o Zod para validar.
- **As mesmas regras ficam no front e na API, de propósito.** No front, elas existem para o usuário ver o erro **na hora**, embaixo do campo, sem esperar a API. Na API (Domínio), elas são a **garantia**, porque o front pode ser burlado (qualquer pessoa pode chamar a API direto). Erros que só o servidor sabe, como "nome repetido", aparecem na caixa vermelha acima do botão.

### Decisões de interface para o pátio
- **Busca no próprio celular:** a lista inteira vem numa chamada só, e o filtro roda no aparelho. Com dezenas de serviços, o resultado é instantâneo a cada letra, sem depender da internet.
- **Busca sem acento:** `normalize('NFD')` separa a letra do acento ("ç" vira "c" + "¸"), e a expressão `\p{Diacritic}` remove os acentos. Assim, "vitrificacao" acha "Vitrificação". Isso resolveu no front o item "busca sem acento" que tinha ficado para depois no passo 13.
- **Preço em reais do jeito brasileiro:** o campo aceita "1.500", "1.500,00", "150" e "R$ 80" (`lerValorEmReais`). `inputMode="decimal"` abre o teclado numérico no celular. **Máximo vazio = preço fixo.**
- **Tempo em horas e minutos**, e não só em minutos: é mais fácil digitar "16 h" do que "960". A API continua recebendo minutos.
- **Excluir categoria com serviços** já vem desabilitado na tela. A API também recusaria, mas assim ninguém tenta à toa.
- **Filtro por categoria** em "pílulas" que rolam para o lado no celular, em vez de quebrar em várias linhas.

### Pegadinhas deste passo
1. **`@hookform/resolvers` 6.0 não instalava** (conflito de dependências opcionais, ERESOLVE). Usamos a **5.9**, que é estável e já suporta o Zod 4. Uma versão nova demais nem sempre é a melhor escolha.
2. **O dado antigo aparecia por um instante.** Depois de desativar um serviço, a lista ainda o mostrava como ativo. O `invalidateQueries` só busca de novo as consultas **na tela agora**, e a lista não estava aberta. Ao voltar, ela mostrava o cache antigo até a busca nova terminar. **Correção:** `refetchType: 'all'`, que busca antes de navegar. O teste automático pegou isso.
3. **O `Intl` usa espaço inseparável** (U+00A0) entre "R$" e o número, para o valor nunca quebrar de linha. Na tela não se nota diferença, mas uma comparação de texto no teste falhou por causa disso.

### Testes feitos (Playwright, no Edge, com a API e o Vite rodando)
Feitos com 360 px de largura, que é um celular pequeno, e depois com 1280 px.

| Caso | Resultado |
|---|---|
| Catálogo vazio orienta a criar categoria primeiro | ✅ |
| Criar 2 categorias / repetida (em minúsculas) mostra o erro da API | ✅ / ✅ |
| Enviar o formulário vazio mostra os 4 erros embaixo dos campos | ✅ |
| Máximo menor que o mínimo é barrado | ✅ |
| Salvar volta para a lista com "R$ 1.500,00 a R$ 3.000,00 · 16 h" | ✅ |
| Preço fixo mostra um valor só; 90 min vira "1 h 30" | ✅ |
| Sem rolagem horizontal em 360 px | ✅ |
| Busca "vitrificacao" (sem acento) / filtro por categoria | ✅ / ✅ |
| Edição vem preenchida ("1.500,00", 16 h) e a alteração aparece na lista | ✅ |
| Desativar some da lista; "Mostrar desativados" traz de volta | ✅ (depois da correção da pegadinha 2) |
| Categoria com serviço: excluir desabilitado; contagem "1 serviço" | ✅ |
| Renomear categoria aparece na lista de serviços | ✅ |
| Depois de sair, nada do catálogo fica na tela | ✅ |

## Passo 15 — Carga inicial do catálogo (`importar-catalogo`)

### O que foi feito
- **`docs/catalogo-inicial.json`**: os 36 serviços do `catalogo-servicos.md`, num formato que um programa lê com segurança. Ler as tabelas de Markdown direto no C# seria frágil: um `|` a mais quebraria tudo. A conversão foi feita uma vez, por script. "1 dia" virou **8 h de trabalho** (480 min), e "1 dia e meio" virou 12 h.
- **`importar-catalogo`**: o comando lê o arquivo, mostra um **resumo** (quantas categorias e quantos serviços são novos) e só grava depois de você responder **"s"**.

```bash
cd backend
dotnet run --project src/Orcamentos.Api -- importar-catalogo docs/catalogo-inicial.json
```

### Rodar de novo não duplica nada (idempotência)
Um comando é **idempotente** quando rodá-lo 1 vez ou 10 vezes dá o mesmo resultado. Este compara pelo **nome**, sem diferenciar maiúsculas:
- Uma categoria ou um serviço que **já existe** é **mantido como está**. Ele não é sobrescrito, porque alguém pode ter ajustado o preço pelo app depois da primeira carga.
- O que **falta** é criado. Se um serviço for apagado do banco, rodar o comando de novo o recria.

### Desligar o filtro global, com cuidado (`IgnoreQueryFilters`)
O filtro do passo 13 descobre a oficina **pelo token**. Um comando de terminal não tem token, então o filtro esconderia **tudo**: o comando acharia que não existe nenhum serviço e duplicaria o catálogo inteiro.

A solução é `IgnoreQueryFilters()`, que desliga o filtro **naquela consulta**, seguida de um `Where(s => s.OficinaId == oficina.Id)` escrito à mão. **Sempre que o filtro for desligado, o filtro manual tem que vir junto.** Sem ele, a consulta enxergaria os dados de todas as oficinas.

### Tudo ou nada (transação)
Todos os serviços passam pelo construtor do Domínio, que valida cada um. Só no final acontece **um único** `SaveChanges`, que é uma transação. Se o 30º serviço tiver um preço inválido, os 29 anteriores também **não** são gravados. Isso evita um catálogo "pela metade", difícil de consertar.

### Pegadinhas deste passo
1. **O caminho relativo do arquivo não era encontrado.** O `dotnet run --project src/Orcamentos.Api` executa o programa **dentro da pasta do projeto**, e não na pasta onde o comando foi digitado. Então `../docs/...` apontava para `src/docs/...`. **Correção:** se não achar na pasta atual, o comando procura nas pastas acima. O arquivo que ele usou aparece no resumo ("Arquivo: C:\...").
2. **O banco recusou uma alteração de teste.** Para testar a regra "não sobrescrever", tentei mudar à mão o preço mínimo da lavagem para R$ 199, acima do máximo de R$ 180. A *check constraint* `ck_servicos_faixa_de_preco` barrou a alteração, mesmo vinda de um SQL escrito direto no banco. É a "última linha de defesa" do passo 13 funcionando.

### Testes feitos (banco local)
| Caso | Resultado |
|---|---|
| Arquivo que não existe | ✅ mensagem com o uso correto, código 1 |
| Responder "n" | ✅ "Cancelado. Nada foi gravado." (0 serviços no banco) |
| Responder "s" | ✅ 5 categorias e 36 serviços |
| Rodar de novo | ✅ "Nada a importar." (36 já existiam) |
| Serviço editado (R$ 150) + serviço apagado, rodar de novo | ✅ o editado continua com R$ 150; o apagado foi recriado (1 novo, 35 mantidos) |
| Arquivo com faixa invertida e uma categoria nova | ✅ erro com a mensagem da regra; **a categoria nova também não foi gravada** |
| JSON quebrado | ✅ erro, nada gravado |
| API depois da carga | ✅ devolve os 36 serviços para o usuário da oficina (o `OficinaId` foi gravado certo) |

### Em produção
1. Rodei antes respondendo **"n"**, para conferir o resumo: oficina "CJ Estética Automotiva", 5 categorias e 36 serviços novos, nada existente.
2. Depois rodei respondendo **"s"**: "Catálogo importado."
3. Na conferência, rodei de novo: "Nada a importar." (36 já existiam).

## Passo 16 — API de clientes e veículos

### Agregado: o Cliente é a "porta de entrada" dos seus veículos
Um **agregado** é um grupo de objetos que muda junto, com uma **raiz** que garante as regras do conjunto. Aqui, a raiz é o `Cliente`, e os `Veiculo`s pertencem a ele:
```csharp
private readonly List<Veiculo> _veiculos = [];
public IReadOnlyCollection<Veiculo> Veiculos => _veiculos.AsReadOnly(); // por fora, só leitura

public Veiculo AdicionarVeiculo(string placa, string modelo) { ... }      // valida e adiciona
public void AtualizarVeiculo(Guid veiculoId, string placa, string modelo) { ... }
public void RemoverVeiculo(Guid veiculoId) { ... }
```
- **Ninguém faz `cliente.Veiculos.Add(...)`**, porque a lista é só leitura. A única forma de adicionar um veículo é `AdicionarVeiculo`, que aplica as regras, como "o mesmo cliente não tem a mesma placa duas vezes".
- **O construtor do `Veiculo` é `internal`**: só o próprio projeto Domínio (ou seja, o `Cliente`) cria veículos.
- **No banco, o EF grava o agregado inteiro**: `db.Clientes.Add(cliente)` já insere os veículos. `HasField("_veiculos")` diz ao EF para ler e gravar direto no campo privado.
- **Excluir o cliente exclui os veículos** (`DeleteBehavior.Cascade`). Isso atende à LGPD do PROJETO.md: dá para apagar tudo de uma pessoa.

### Normalizar na entrada: placa e WhatsApp
Quem digita no pátio escreve de qualquer jeito. O Domínio **normaliza** antes de guardar:

| Digitado | Guardado |
|---|---|
| `abc-1234`, `ABC 1234` | `ABC1234` (placa antiga) |
| `abc1d23` | `ABC1D23` (Mercosul) |
| `(11) 99999-8888`, `+55 11 99999-8888` | `5511999998888` |
| `21 3333-4444` (fixo) | `552133334444` |

- **Placa:** a regra é 3 letras, 1 número, 1 número **ou** letra, e 2 números. É a 5ª posição que diferencia a placa antiga da Mercosul. Usamos `[GeneratedRegex]`, que faz o .NET gerar o código da expressão regular na compilação.
- **WhatsApp:** guardado como o link `wa.me` exige: só números, com o 55. Um número que começa com "+" precisa ser +55. Celular com 9 dígitos precisa começar com 9; foi um teste que pegou o caso "+1 415..." (EUA) sendo aceito como um número brasileiro.
- **A busca usa as mesmas funções** (`Veiculo.NormalizarPlaca`, `Cliente.NormalizarWhatsApp`), então "abc-1d" acha "ABC1D23".

### Regras: no Domínio ou no endpoint?
| Regra | Onde | Por quê |
|---|---|---|
| Placa válida, WhatsApp válido, nome obrigatório | Domínio | Não depende de mais nada |
| Mesma placa duas vezes **no mesmo cliente** | Domínio (`Cliente`) | O cliente conhece os próprios veículos |
| Mesma placa em **outro** cliente da oficina | Endpoint (`PlacaEmUso`) + índice único no banco | Precisa consultar o banco |

A mensagem do conflito diz **de quem** é a placa ("A placa ABC1D23 já está cadastrada para João da Silva."). No pátio, isso ajuda a achar o cadastro certo em vez de criar outro.

### Uma caixa de busca, três campos
`GET /api/clientes?busca=...` procura ao mesmo tempo:
- **no nome, sem diferenciar acentos:** `EF.Functions.Unaccent` usa a extensão `unaccent` do PostgreSQL, ativada pela migration (`HasPostgresExtension("unaccent")`). "joao" acha "João";
- **na placa:** "abc-1d" vira "ABC1D" e acha "ABC1D23";
- **no WhatsApp:** a partir de 4 números.

**Por que a busca de clientes roda no servidor, e a de serviços não (passo 14):** serviços são algumas dezenas e quase não mudam. Clientes **crescem sem parar**: em um ano, podem ser centenas. Por isso, a API devolve **no máximo 50** e filtra no banco. Essa é a "paginação" que tinha ficado para depois, na forma mais simples: um limite.

### Pegadinha: o EF tentou um UPDATE num veículo novo
Nossos ids nascem no C# (`Guid.CreateVersion7()`). Quando um veículo novo é adicionado a um cliente **já salvo**, o EF encontra um objeto novo na lista, mas com o id preenchido. Por padrão, ele interpreta "id preenchido" como "já existe no banco" e manda um `UPDATE`, que não atualiza nenhuma linha.

**Prova:** tirei a configuração temporariamente e tentei adicionar um veículo. Resultado: **erro 500**, com `DbUpdateConcurrencyException: The database operation was expected to affect 1 row(s), but actually affected 0 row(s)`.

**Correção:** `builder.Property(v => v.Id).ValueGeneratedNever()`, que diz ao EF: "o id vem do código, não do banco; objeto novo na lista é INSERT".

### Testes feitos (script Python, duas oficinas)
**44 testes de unidade** (18 novos) e **28 verificações na API**, todas certas:

| Caso | Resultado |
|---|---|
| Criar cliente já com veículo / sem veículo (telefone fixo) | ✅ 201, WhatsApp e placa normalizados |
| WhatsApp sem DDD / placa inválida | ✅ 400 com a mensagem da regra |
| Placa que já é de outro cliente (criar, adicionar, alterar) | ✅ 409 "...cadastrada para João da Silva." |
| Cliente com conflito de placa não fica gravado pela metade | ✅ |
| Busca "joao", "SOUZA", "abc-1d", "3333" | ✅ cada uma acha o cliente certo |
| Mesma placa no mesmo cliente | ✅ 400 |
| Alterar só o modelo, mantendo a placa | ✅ (não conflita consigo mesmo) |
| **Oficina B não vê, não busca, não altera e não exclui nada de A** | ✅ |
| **B pode cadastrar a mesma placa** (outra oficina) | ✅ 201 |
| Remover veículo; excluir cliente leva os veículos junto | ✅ a placa fica livre de novo |

## Passo 17 — Telas de clientes e veículos

### O que foi feito
| Tela | Rota | O que faz |
|---|---|---|
| Lista | `/clientes` | Uma caixa de busca para nome, placa ou WhatsApp, mostrando as placas de cada cliente |
| Novo cliente | `/clientes/novo` | Cliente **e** primeiro veículo numa tela só (no pátio, os dois chegam juntos) |
| Cliente | `/clientes/:id` | Botão **WhatsApp**, dados, veículos (adicionar, editar, remover) e excluir cliente |

### Busca no servidor com "debounce"
Os serviços são filtrados no celular (passo 14), mas a busca de clientes vai à API (passo 16). Se cada letra digitada chamasse a API, "joão" seriam 4 chamadas. O hook **`useValorAtrasado`** espera a pessoa **parar de digitar por 300 ms** e só então libera o valor. Essa técnica se chama *debounce*.
```ts
const termo = useValorAtrasado(busca.trim())   // muda só 300 ms depois da última tecla
useQuery({ queryKey: ['clientes', 'busca', termo], ... })
```
O `useEffect` devolve uma função de **limpeza** (`clearTimeout`). Se outra tecla chega antes dos 300 ms, o React cancela o temporizador anterior.

Com **`placeholderData: keepPreviousData`**, enquanto a busca nova não chega, a lista anterior continua na tela, um pouco transparente. Assim, ela não "pisca" para "Carregando..." a cada letra.

### O link do WhatsApp (`wa.me`)
`https://wa.me/5511999998888` é o link oficial do WhatsApp: no celular, ele abre o app direto na conversa com aquele número. É por isso que o WhatsApp é guardado só com números e com o 55 (passo 16). Na semana 4, o mesmo link vai levar o orçamento ao cliente.

### Atualizar o cache sem buscar de novo (`setQueryData`)
As operações de veículo já **devolvem o cliente atualizado**. Em vez de pedir à API "me mande o cliente de novo", o app coloca a resposta direto no cache:
```ts
queryClient.setQueryData(['clientes', id], clienteAtualizado)
```
A tela se atualiza na hora, sem uma segunda chamada. Isso faz diferença com a API "acordando". As **listas de busca** continuam sendo invalidadas (`refetchType: 'all'`, a lição do passo 14), porque o nome ou as placas podem ter mudado.

### Formulários: `isDirty` e `reset`
- **`isDirty`** (React Hook Form) indica se algo foi alterado desde que o formulário abriu. O botão "Salvar dados" só fica habilitado quando há o que salvar.
- **`reset(novosValores)`**, depois de salvar, torna os valores salvos o novo "ponto de partida", e o botão volta a ficar desabilitado.
- **Primeira versão (descartada):** o formulário era **recriado** a cada mudança no cliente (`key={dataUpdatedAt}`). Isso apagaria uma edição de nome ainda não salva caso a pessoa adicionasse um veículo no meio. O `reset` resolve sem esse efeito colateral.

### Validar dois campos "juntos ou nenhum" (`superRefine`)
No cadastro de cliente novo, o veículo é **opcional**, mas, se a pessoa começar a preencher a placa, o modelo passa a ser obrigatório, e vice-versa. O `superRefine` do Zod só roda as regras do veículo (o mesmo `esquemaDoVeiculo` da tela do cliente) quando um dos dois campos está preenchido, e coloca cada erro embaixo do campo certo.

### Acessibilidade: contraste do botão verde
O botão do WhatsApp começou em `green-600` com texto branco: contraste de **3,3:1**, abaixo do mínimo de 4,5:1 (a mesma regra do dourado, no passo 10). Ficou em **`green-700`, com 5,0:1**.

### Placa como na placa real
O banco guarda `ABC1234`, mas a tela mostra **`ABC-1234`** para a placa antiga e **`ABC1D23`** para a Mercosul, como aparecem no carro (`formatarPlaca`). As placas usam fonte monoespaçada (`font-mono`), em que cada caractere tem a mesma largura, o que facilita conferir letra por letra.

### Testes feitos (Playwright, no Edge, com 360 px e 1280 px)
**22 verificações, todas certas na primeira execução:**

| Caso | Resultado |
|---|---|
| Lista vazia | ✅ "Nenhum cliente cadastrado ainda." |
| Enviar vazio: só nome e WhatsApp são exigidos (veículo opcional) | ✅ |
| WhatsApp sem DDD + placa inválida + placa sem modelo | ✅ 3 erros, embaixo dos campos certos |
| Salvar abre a tela do cliente; botão aponta para `wa.me/5511999998888` | ✅ |
| Placa e WhatsApp aparecem formatados | ✅ "ABC1D23", "XYZ-1234", "(11) 99999-8888" |
| Adicionar e editar veículo | ✅ |
| Cliente sem veículo já abre com o formulário de veículo | ✅ |
| Placa de outro cliente | ✅ "A placa ABC1D23 já está cadastrada para João da Silva." |
| "Salvar dados" desabilitado sem alteração; salvar atualiza o título e mostra "Salvo" | ✅ |
| A lista mostra o nome alterado (cache invalidado) | ✅ |
| Busca "joao", "xyz-12", "3333" e sem resultado | ✅ |
| Remover veículo; excluir cliente volta para a lista | ✅ |
| Sem rolagem horizontal em 360 px (lista e detalhe) | ✅ |

### Verificação em produção, antes da migration
Uma consulta **só de leitura** no Neon (`pg_available_extensions`) confirmou que ele oferece a extensão `unaccent` (versão 1.1), ainda não instalada. A migration vai instalá-la.

### Pegadinha de Git: PRs empilhados mesclados fora de ordem
O PR das telas (#18) tinha como base a branch da API (`feat/clientes-api`), e não a `main`. A API (#17) foi mesclada primeiro e a branch dela **não foi apagada**. Assim, o GitHub não trocou a base do #18, e o merge dele levou as telas para a branch da API, não para a `main`. Foi preciso um PR extra (#19). **Lição:** a partir daqui, um PR por vez, sempre com base `main`.

---

# Semana 3

## Passo 18 — Back-end do orçamento (domínio, banco e API)

### As regras, onde ficam e por quê
| Regra | Onde |
|---|---|
| Item do catálogo entra pelo preço mínimo e **guarda uma cópia** (nome, preço, tempo) | `Orcamento.AdicionarServico` |
| Ajuste por estado: `Ajuste = ValorUnitario − PrecoCatalogo` | `ItemOrcamento` |
| Desconto em R$ ou %, nunca maior que o subtotal | `Orcamento.DefinirDesconto` e `ValorDoDesconto` |
| Validade de 7 dias; Enviado vira Expirado **no dia seguinte** ao fim | `Orcamento.ExpirarSeVencido` |
| Transições de status (o que pode virar o quê) | `MarcarComoEnviado`, `Aprovar`, `Recusar` |
| Aprovado, recusado e expirado não editam: **duplicar** | `GarantirEditavel`, `Duplicar` |
| Excluir só rascunho | `GarantirQuePodeSerExcluido` |
| O veículo é do cliente escolhido; o serviço é desta oficina | Endpoint (precisa do banco) |
| Número sequencial por oficina e ano | `ContadorDeOrcamentos` (banco) |

**33 testes novos** no domínio, **77 no total**. Os testes de dinheiro e de status são os mais importantes do projeto: um erro ali vira **prejuízo** ou um **orçamento errado na mão do cliente**.

### O domínio não lê o relógio
Nenhum método chama `DateTime.Now`. Quem chama passa `hoje` e `agora`:
```csharp
orcamento.ExpirarSeVencido(hoje);
orcamento.Aprovar(agora);
```
Por isso o teste "expira só no dia seguinte" roda em milissegundos: ele passa `Validade` e depois `Validade.AddDays(1)`. Na API, o **`Relogio`** decide o que é "hoje" **no fuso de São Paulo**. O Render roda em UTC, e às 22h de São Paulo já é o dia seguinte em UTC: sem a conversão, um orçamento expiraria 3 horas antes. O `Relogio` usa o `TimeProvider` do .NET, que pode ser trocado por um relógio "parado" em testes futuros.

### Valores calculados, não guardados
`Subtotal`, `ValorDoDesconto`, `Total` e `TempoEstimadoTotalMin` são propriedades calculadas a partir dos itens, e **não existem no banco**. Assim, é impossível um total "desencontrado" dos itens. O preço disso: a lista calcula o total no C#, carregando os itens. Com no máximo 50 orçamentos por tela, isso não pesa.

### Itens como "owned" (`OwnsMany`)
No EF, os itens são **propriedade** do orçamento: não têm `DbSet` próprio, são sempre carregados junto e herdam o filtro por oficina. É o mapeamento que combina com o agregado (passo 16). A tabela é `itens_orcamento`, e o `ValueGeneratedNever()` voltou a ser necessário, pela mesma pegadinha do `Veiculo`.

### Salvar = sincronizar
O app envia o orçamento **inteiro**, como está na tela. O endpoint compara com o que está no banco:
- item **com `id`** → `AtualizarItem` (o preço de catálogo guardado **não muda**);
- item **sem `id`** → item novo (do catálogo, se tiver `servicoId`, ou avulso);
- item que **sumiu** da lista → `RemoverItem`.

Um formato só serve para criar e para salvar, e o app faz **uma** chamada por "Salvar". Isso importa com a API acordando no Render.

### Pegadinha: a numeração falhou com pedidos simultâneos
**Primeira versão:** o próximo número era "o maior + 1". Se duas pessoas criassem um orçamento no mesmo instante, as duas calculariam o mesmo número. O índice único recusaria a segunda, que tentaria de novo, com até 3 tentativas.

**O teste com 5 criações simultâneas pegou o problema:** só **4 de 5** foram gravadas. Um pedido "azarado" perdeu a disputa 4 vezes seguidas e esgotou as tentativas. Aumentar as tentativas só diminuiria a chance do erro, sem eliminá-la.

**Versão final:** uma tabela `contadores_de_orcamentos` (uma linha por oficina e ano), incrementada pelo banco numa única instrução **atômica**:
```sql
INSERT INTO contadores_de_orcamentos (oficina_id, ano, ultimo) VALUES (@oficina, @ano, 1)
ON CONFLICT (oficina_id, ano) DO UPDATE SET ultimo = contadores_de_orcamentos.ultimo + 1
RETURNING ultimo
```
- **Dentro de uma transação**, junto com a gravação do orçamento. O PostgreSQL **trava a linha** do contador até o fim, então o segundo pedido **espera** o primeiro terminar e recebe o número seguinte. Isso se chama *bloqueio pessimista*.
- **Se algo falhar no meio**, como um item inválido, a transação é desfeita (`await using` sem `Commit`) e o contador volta. Assim, não ficam "buracos" na numeração.
- **`$"..."` no `SqlQuery`** vira parâmetros. Nunca concatene valores em SQL: isso abre espaço para *SQL injection*.

**Prova:** 10 criações simultâneas e depois **3 rodadas de 20**: 60 orçamentos, todos com números diferentes e **seguidos** (16 a 75). Um orçamento recusado por erro **não gastou número**.

### Expirar sem carregar um por um (`ExecuteUpdate`)
Antes de listar ou abrir um orçamento, um único `UPDATE` no banco expira todos os vencidos:
```csharp
db.Orcamentos.Where(o => o.Status == Enviado && o.Validade < hoje)
             .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, Expirado));
```
É a mesma regra de `Orcamento.ExpirarSeVencido`, mas sem trazer os orçamentos para o C#. O filtro por oficina vale também aqui. O trade-off: a regra existe em dois lugares (Domínio e consulta), por isso um comentário aponta de um para o outro.

### Outras mudanças
- **Enums no JSON como texto** (`"status": "Rascunho"`, e não `0`), com o `JsonStringEnumConverter`, para todos os endpoints.
- **Cliente ou veículo com orçamentos não podem mais ser excluídos** (409 com explicação), e o banco também barra (`Restrict`).
- **`Dinheiro.Arredondar`**: o arredondamento de centavos saiu do `Servico` e foi para um lugar só, porque agora itens, desconto e total também usam.
- **Busca por número só quando a busca inteira é um número de orçamento** ("3", "0003", "ORC-2026-0003"). Toda placa termina em números, e "ABC1D23" acharia o orçamento 23.

### Testes feitos (script Python, contra a API local)
**40 verificações, todas certas** (depois da correção da numeração):

| Grupo | Casos |
|---|---|
| Criar | ✅ número, status, subtotal 1.000 − 10% = 900, ajuste (catálogo 600 → 800 = +200), validade +7, prazo 660 min, ordem dos itens |
| Validações | ✅ veículo de outro cliente, serviço inexistente, quantidade zero, desconto > subtotal, validade no passado |
| Catálogo | ✅ mudar o preço do serviço **não** altera o orçamento |
| Salvar | ✅ alterar, remover e adicionar itens numa chamada; o item alterado mantém o Id; total 1.140 |
| Status | ✅ enviar → aprovar; aprovado não edita nem exclui; recusado e expirado não aprovam; enviar sem itens |
| Expiração | ✅ enviado vencido vira Expirado; rascunho vencido não |
| Duplicar | ✅ rascunho novo, próximo número, mesmo total |
| Lista | ✅ mais recente primeiro; filtro por status; busca por número, placa (sem achar o nº 23) e nome sem acento |
| Numeração | ✅ 10 simultâneos com números seguidos; erro não gasta número |
| Isolamento | ✅ a oficina B não lista, não lê, não duplica e não usa o cliente de A |
| Proteções | ✅ cliente e veículo com orçamentos dão 409; rascunho exclui |

## Passo 19 — Tela do orçamento

### O desenho, pensado para o pátio
| Parte | Decisão | Por quê |
|---|---|---|
| Tela | Uma só, rolando: cliente → serviços → desconto/validade → observações | Sem "próximo, próximo": tudo à vista |
| Total | **Barra fixa embaixo**, com o total e o botão Salvar | Sempre visível, ao alcance do polegar |
| Cliente | Busca por nome ou placa; **cliente novo** e **outro veículo** sem sair da tela | O cliente chega no pátio e ainda não está cadastrado |
| Serviços | Seletor em **tela cheia** (no computador, um painel lateral) | Lista grande espremida no meio do formulário não funciona no celular |
| Mesmo serviço 2× | **Soma na quantidade**, em vez de repetir a linha | Martelinho em 2 portas = "2×" |
| Quantidade | Botões **− / +** | Mais fácil com uma mão do que digitar |
| Ajuste | A faixa do catálogo aparece embaixo do valor, e o ajuste vira um selo (**+R$ 30,00 pelo estado**) | Mostra na hora quanto o estado do carro mudou o preço |
| Status | Botões no topo, **bloqueados enquanto houver alteração não salva** | Senão daria para aprovar um orçamento com valores diferentes dos da tela |
| Aprovado/recusado/expirado | Vira um **resumo só de leitura** + "Duplicar" | É a regra do Domínio, refletida na tela |

### `useFieldArray` e `useWatch` (React Hook Form)
- **`useFieldArray`** controla a **lista de itens** dentro do formulário: `append` (adicionar), `remove` e `update`. Cada linha recebe uma `key` estável (`campo.chave`), para o React não confundir as linhas quando uma do meio é removida.
- **`useWatch`** "assiste" aos campos e re-renderiza quando mudam. É o que faz o total mudar **a cada toque**:
  ```ts
  const itens = useWatch({ control, name: 'itens' })
  const totais = calcularTotais(paraCalculo(itens), descontoTipo, desconto)
  ```
- **`calculos.ts`** repete as fórmulas do Domínio (soma, desconto que nunca passa do subtotal, arredondamento "da escola"). É só para mostrar; **quem manda é a API**, e depois de salvar a tela mostra os valores que ela devolveu.

### Pegadinha 1: depois de salvar, os itens duplicariam
Um item novo não tem `id`, e a API cria o item e devolve o `id` dele. Se o formulário continuasse com os itens **sem `id`**, o próximo "Salvar" os enviaria como novos de novo, e a API criaria **outra cópia**.

**Correção:** depois de cada salvamento, `reset(paraValores(resposta))` recarrega o formulário com o que a API devolveu, já com os ids. O teste prova isso: ele salva, altera, salva de novo, recarrega a página, e continuam sendo **3 itens**, não 5.

### Pegadinha 2: formulário dentro de formulário
O cadastro rápido de cliente ficava num `<form>` **dentro** do `<form>` do orçamento. Ao tocar em "Cadastrar e usar", **a página inteira recarregou** e nada foi salvo.
- **Diagnóstico** (com o Playwright registrando requisições e o console): os arquivos do app foram baixados de novo depois do clique (recarga), e o React avisava no console: *"`<form>` cannot contain a nested `<form>`"*.
- **Primeira hipótese, errada:** que o botão pertencia ao formulário de fora. O teste mostrou que não, porque o React cria os elementos direto no DOM. Mesmo assim, o navegador não sabe lidar com o envio de um formulário aninhado, e a página recarrega.
- **Correção:** o seletor de cliente usa `<div>`, e os botões chamam `handleSubmit(...)` direto (`type="button"`). Agora o console fica **sem nenhum erro**.

### Pegadinha 3: Enter enviava o orçamento sem querer
Apertar Enter ("Ir", no teclado do celular) em **qualquer** campo envia o formulário, comportamento padrão do HTML. Buscar um cliente e tocar "Ir" tentaria salvar o orçamento. Agora o `onKeyDown` do formulário ignora Enter nos campos de texto, e só o botão Salvar salva.

### Outros detalhes
- **Datas sem fuso:** `new Date("2026-10-26")` é meia-noite **em UTC**, que no Brasil ainda é o dia 25. As funções `formatarData`, `hojeIso` e `somarDias` trabalham com o texto `"2026-10-26"` direto, ou com meio-dia, para nunca "voltar um dia".
- **Validade de um orçamento novo:** se você não mexer no campo, o app não manda a data, e a API usa o padrão dela (hoje + 7, no fuso de São Paulo). Assim, o relógio do celular não decide a data.
- **O valor é formatado ao sair do campo** ("150" vira "150,00"), para todos os itens ficarem no mesmo formato.
- **`SeloDeStatus`:** uma cor para cada status, sempre texto escuro sobre fundo claro (contraste acima de 4,5:1).

### Testes feitos (Playwright, no Edge, com 360 px e 1280 px)
**27 verificações, todas certas** (depois das correções acima e de duas esperas que faltavam no próprio teste):

| Etapa | O que foi conferido |
|---|---|
| Sem cliente | ✅ "Escolha o cliente e o veículo." |
| Cliente novo na hora | ✅ cadastrado e já selecionado (placa normalizada) |
| Seletor | ✅ marca "Adicionado"; tocar 2× no martelinho vira "2×" e **uma** linha |
| Total ao vivo | ✅ 360 → 390 (ajuste para 150) → 440 (avulso) → 396 (10%) → 400 (R$ 40) |
| Ajuste e faixa | ✅ "+R$ 30,00 pelo estado"; "Catálogo: R$ 120,00 a R$ 180,00" |
| Desconto inválido | ✅ "Maior que o subtotal." |
| Salvar | ✅ abre ORC-2026-0001 como rascunho; total R$ 400,00 vindo da API |
| Salvar 2× e recarregar | ✅ **3 itens** (não duplicou), total 550, observação mantida |
| Alteração pendente | ✅ status bloqueado, com aviso |
| Enviar → Aprovar | ✅ vira resumo só de leitura |
| Duplicar → Excluir | ✅ ORC-2026-0002 rascunho com o mesmo total; excluir volta para a lista |
| Computador | ✅ busca de cliente existente com 1 carro já seleciona direto |
| Layout | ✅ sem rolagem horizontal em 360 px; console sem erros |
