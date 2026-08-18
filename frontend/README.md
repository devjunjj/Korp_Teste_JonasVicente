## Tecnologias utilizadas

### Backend (C# / .NET)

- **ASP.NET Core Web API** — framework para os endpoints REST.
- **Entity Framework Core** com **SQLite** — persistência real em banco de dados, com migrations versionadas.
- **HttpClient** (`AddHttpClient`) — comunicação entre os microsserviços.
- **LINQ** — utilizado em consultas ao banco (ex: `Produtos.ToListAsync()`, `Produtos.FindAsync()`, `NotasFiscais.OrderByDescending(...)` para numeração sequencial, `Produtos.AnyAsync(...)` para verificação de concorrência) e no cálculo de regras de negócio.
- **Swagger / Swashbuckle** — documentação e testes interativos da API.
- **CORS** — configurado no EstoqueService para permitir chamadas do frontend Angular.

### Frontend (Angular)

- **Angular** (standalone components) com **Angular Material** para os componentes visuais (tabelas, formulários, spinners, botões, notificações).
- **RxJS** — todas as chamadas HTTP retornam `Observable`, tratadas via `.subscribe()`.
- **Reactive Forms** — utilizado no cadastro de produtos, com validações (`required`, `min`, `maxLength`).
- **HttpClient** do Angular para consumo das APIs.
- **Angular Router** — navegação entre as telas (`/produtos`, `/produtos/novo`).

## Tratamento de erros e exceções

- No backend, o endpoint de impressão de notas fiscais usa blocos `try/catch` para capturar falhas de comunicação com o EstoqueService (`HttpRequestException`), retornando código HTTP 503 com mensagem clara ao usuário, em vez de deixar a aplicação quebrar.
- Validações de negócio (saldo insuficiente, produto inexistente, nota já fechada) retornam código 400 com mensagens descritivas.
- No frontend, chamadas HTTP tratam o callback de erro do `Observable` separadamente do de sucesso, exibindo feedback ao usuário (via `MatSnackBar`) em vez de falhar silenciosamente.

## Observação técnica: ChangeDetectorRef

Durante o desenvolvimento, foi identificado que a detecção automática de mudanças do Angular (mecanismo que atualiza a tela sozinho após uma resposta assíncrona) não disparava de forma consistente neste projeto após chamadas HTTP, mesmo com o Zone.js corretamente instalado. A causa exata não foi totalmente isolada, mas o comportamento foi contornado de forma controlada usando `ChangeDetectorRef.detectChanges()` manualmente após a atualização do estado do componente, garantindo que a tela reflita os dados corretamente. Essa abordagem gera um aviso de desenvolvimento (`NG0100`) em alguns cenários de atualização encadeada, que não afeta o funcionamento da aplicação e não aparece em builds de produção.

## Como rodar o projeto localmente

### Pré-requisitos
- .NET SDK 10
- Node.js e Angular CLI
- SQLite (geralmente já vem com o EF Core, não precisa instalar separado)

### Backend
```bash
cd EstoqueService
dotnet restore
dotnet run
```
```bash
cd FaturamentoService
dotnet restore
dotnet run
```

### Frontend
```bash
cd frontend
npm install
ng serve
```

Acesse `http://localhost:4200`.

## Status do desenvolvimento

- [x] Cadastro de Produtos (CRUD completo no backend)
- [x] Cadastro de Notas Fiscais com numeração sequencial (backend)
- [x] Comunicação entre microsserviços via HTTP
- [x] Tratamento de falha e recuperação (EstoqueService indisponível)
- [x] Tela de listagem de Produtos (Angular)
- [x] Tela de cadastro de novo Produto (Angular)
- [x] Exclusão de Produtos (Angular)
- [ ] Tela de listagem de Notas Fiscais (Angular)
- [ ] Tela de criação de Nota Fiscal (Angular)
- [ ] Botão de impressão com indicador de carregamento
- [ ] Documentação técnica completa
- [ ] Vídeo de apresentação

## Autor

Jonas Vicente