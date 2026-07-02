# 📚 EstudosDash

Dashboard pessoal de **estudos e matérias** em **C# / .NET**, que se conecta ao **Notion** para
mostrar suas matérias, status e progresso num painel limpo. Projeto **open source**, feito como
projeto pessoal — código aberto, dados privados (o token do Notion fica só na sua máquina).

## O que faz
- Consome a **API do Notion** (uma database de estudos sua) via `HttpClient`.
- Expõe uma **API REST** própria (`/api/subjects`, `/api/health`).
- Serve um **dashboard em React** que agrupa as matérias por categoria e mostra status + barra de progresso,
  com uma identidade visual de "caderno de estudos" (marca-texto, fundo pontilhado).

## Stack
- **Back-end:** .NET 10 / ASP.NET Core (Minimal API), `HttpClient` tipado + injeção de dependência,
  `System.Text.Json`. Arquitetura com interface (`INotionClient`) + implementação — fácil de testar e trocar.
- **Front-end:** React + Vite (em `client/`). O build sai para `wwwroot/` e é servido pela própria API.

## Front-end (React)
```bash
cd client
npm install
npm run dev      # dev server com proxy de /api -> :5099
npm run build    # build de produção -> ../wwwroot (servido pelo .NET)
```
Em produção, basta o `dotnet run` (o front já buildado está em `wwwroot/`).

## Como rodar

### 1. Crie uma integração no Notion
- Acesse **notion.so/my-integrations** → *New integration* → copie o **Internal Integration Token**.
- Abra a sua **database de estudos** no Notion → menu `•••` → *Connections* → conecte a integração.
- Pegue o **Database ID** (está na URL da database: `notion.so/<workspace>/<DATABASE_ID>?v=...`).

### 2. Configure (sem colocar segredo no git)
Opção A — arquivo local (gitignored):
```bash
cp appsettings.Local.json.example appsettings.Local.json
# edite appsettings.Local.json com o seu Token e DatabaseId
```
Opção B — variáveis de ambiente:
```bash
export Notion__Token="secret_xxx"
export Notion__DatabaseId="xxxxxxxx"
```

### 3. Rode
```bash
dotnet run
```
Abra o endereço que aparecer no console (ex.: `http://localhost:5000`).

## Mapeamento das propriedades
O parser acha o **título** automaticamente (pela propriedade do tipo *title*). Os demais campos
são lidos por **nome** — ajuste em `appsettings.json` conforme a sua database:
- `StatusProperty` (padrão `Status`) — propriedade *status* ou *select*.
- `CategoryProperty` (padrão `Matéria`) — propriedade *select* usada para agrupar.
- `ProgressProperty` (padrão `Progresso`) — propriedade *number* (0–100).

## Segurança
- O token do Notion **nunca** é versionado (`appsettings.Local.json` e `.env` estão no `.gitignore`).
- Em produção, prefira variáveis de ambiente ou um cofre de segredos.

---
Feito por Davi Evangelista · [github.com/davievan12](https://github.com/davievan12)
