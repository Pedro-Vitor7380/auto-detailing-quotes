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
