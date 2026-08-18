# Korp Teste - Jonas Vicente

Sistema de emissão de Notas Fiscais desenvolvido como teste técnico para a vaga de estágio na KORP ERP (Viasoft).

## Sobre o projeto

O sistema permite o cadastro de produtos, controle de saldo em estoque, e emissão de notas fiscais com múltiplos itens. Ao "imprimir" uma nota fiscal, o sistema valida e abate automaticamente o saldo dos produtos utilizados, e trata falhas de comunicação entre os serviços de forma explícita, com recuperação automática assim que o serviço volta ao ar.

## Arquitetura

O projeto é dividido em três partes independentes:

- **EstoqueService** — microsserviço em C#/.NET, porta 5001, responsável pelo cadastro de produtos e controle de saldo.
- **FaturamentoService** — microsserviço em C#/.NET, porta 5002, responsável pela emissão de notas fiscais, que se comunica com o EstoqueService via HTTP para validar e abater saldo.
- **frontend** — aplicação Angular (porta 4200) que consome as duas APIs.

Cada microsserviço possui seu próprio banco de dados (SQLite), reforçando o isolamento entre os serviços — nenhum deles acessa o banco de dados do outro diretamente, apenas via API.
```

[Angular - frontend :4200]
|
+--- HTTP ---> [EstoqueService :5001] --- SQLite (estoque.db)
|
+--- HTTP ---> [FaturamentoService :5002] --- SQLite (faturamento.db)
|
+--- HTTP ---> [EstoqueService :5001]


## Detalhamento técnico

### Frameworks utilizados (C#)

- **ASP.NET Core Web API** — framework utilizado nos dois microsserviços para expor os endpoints REST.
- **Entity Framework Core** com **SQLite** — ORM responsável pela persistência real em banco de dados, com migrations versionadas no repositório (pastas `Migrations` em cada serviço).

### Uso de LINQ

LINQ foi utilizado extensivamente em ambos os microsserviços para consultas e regras de negócio, por exemplo:

- `_context.Produtos.ToListAsync()` — listagem de produtos.
- `_context.Produtos.FindAsync(id)` — busca por chave primária.
- `_context.Produtos.AnyAsync(p => p.Id == id)` — verificação de existência (usado no tratamento de concorrência do `PutProduto`).
- `_context.NotasFiscais.OrderByDescending(n => n.Numero).Select(n => n.Numero).FirstOrDefaultAsync()` — cálculo da numeração sequencial da próxima nota fiscal.
- `_context.NotasFiscais.Include(n => n.Itens).FirstOrDefaultAsync(n => n.Id == id)` — carregamento de uma nota fiscal junto com seus itens relacionados.

### Tratamento de erros e exceções no backend

- O endpoint de impressão de notas fiscais (`POST /api/notasfiscais/{id}/imprimir`) usa um bloco `try/catch` para capturar `HttpRequestException`, lançada automaticamente pelo `HttpClient` quando o EstoqueService está inacessível. Nesse caso, é retornado o código HTTP **503 (Service Unavailable)** com uma mensagem clara ao usuário, em vez de deixar a aplicação lançar um erro genérico.
- Validações de negócio (saldo insuficiente, produto inexistente, nota já fechada, quantidade inválida) retornam código **400 (Bad Request)** com mensagens descritivas em formato JSON (`{ "mensagem": "..." }`).
- Recursos não encontrados retornam **404 (Not Found)**.
- Esse padrão de resposta (mensagem clara em JSON) é consumido diretamente pelo frontend, que exibe a mensagem de erro real do backend ao usuário através de notificações (Angular Material `MatSnackBar`), em vez de mensagens genéricas.

### CORS

Ambos os microsserviços (EstoqueService e FaturamentoService) possuem CORS configurado (`AllowAnyOrigin`) para permitir chamadas diretas do frontend Angular, já que rodam em portas/origens diferentes.

### Ciclos de vida do Angular utilizados

- **`ngOnInit`** — utilizado em todos os componentes de listagem (`ProdutoLista`, `NotaFiscalLista`) e no formulário de nota fiscal (`NotaFiscalForm`) para disparar o carregamento inicial de dados assim que o componente é criado e está pronto para uso.

### Uso da biblioteca RxJS

RxJS é utilizado em toda a camada de comunicação HTTP do frontend:

- Todos os métodos dos serviços (`ProdutoService`, `NotaFiscalService`) retornam `Observable`, seguindo o padrão do `HttpClient` do Angular.
- O consumo é feito via `.subscribe({ next: ..., error: ... })`, tratando separadamente o caminho de sucesso e o de erro em cada chamada — por exemplo, ao criar uma nota fiscal ou ao imprimir, o callback de erro extrai a mensagem retornada pelo backend (`erro.error?.mensagem`) e a exibe ao usuário.

### Outras bibliotecas utilizadas

- **Angular Material** — biblioteca de componentes visuais (ver seção abaixo).
- **Angular Reactive Forms** — utilizado nos formulários de cadastro de Produto e de Nota Fiscal, com validações (`required`, `min`, `maxLength`) e, no caso de Nota Fiscal, uso de `FormArray` para permitir uma quantidade dinâmica de itens (produtos) por nota.
- **Angular Router** — gerencia a navegação entre as telas (`/produtos`, `/produtos/novo`, `/notas-fiscais`, `/notas-fiscais/nova`), incluindo destaque visual do link ativo via `routerLinkActive`.

### Bibliotecas de componentes visuais

- **Angular Material**: `MatTable` (listagens), `MatFormField`/`MatInput`/`MatSelect` (formulários), `MatButton`/`MatIconButton` (ações), `MatProgressSpinner` (indicadores de carregamento, incluindo o indicador de processamento durante a impressão de notas fiscais), `MatSnackBar` (notificações de sucesso/erro), `MatChip` (destaque visual do status Aberta/Fechada), `MatToolbar` (menu de navegação).

### Gerenciamento de dependências no Golang

Não aplicável — o backend foi implementado em C#/.NET, não em Golang.

## Tratamento de falhas 

Cenário implementado e testado: ao tentar imprimir uma nota fiscal com o EstoqueService fora do ar, o FaturamentoService captura a falha de comunicação e retorna erro 503 com mensagem clara, exibida ao usuário na interface via notificação. Ao religar o EstoqueService, a mesma nota pode ser impressa normalmente na tentativa seguinte, sem necessidade de qualquer ação de correção manual — demonstrando recuperação da falha.

## Requisitos opcionais implementados

### Tratamento de Concorrência

Implementado no `EstoqueService`, no endpoint de baixa de estoque (`PUT /api/produtos/{id}/baixa`), utilizando **concorrência otimista** do Entity Framework Core:

- O modelo `Produto` possui um campo `Version` marcado com `[ConcurrencyCheck]`.
- A cada atualização de saldo, essa versão é incrementada. O EF Core inclui automaticamente a versão original na cláusula `WHERE` do `UPDATE` gerado.
- Se duas requisições tentarem abater o saldo do mesmo produto simultaneamente, a segunda a chegar ao banco recebe uma `DbUpdateConcurrencyException`, pois a versão que ela tinha em memória já não corresponde mais à do banco.
- O endpoint trata essa exceção com uma **retentativa automática** (até 3 tentativas): a cada tentativa falha, o produto é lido novamente do banco (com o saldo já atualizado pela outra requisição) e a operação é reavaliada. Se todas as tentativas falharem, retorna código **409 (Conflict)**.

Isso resolve o cenário descrito no desafio: produto com saldo 1 sendo utilizado simultaneamente por duas notas fiscais — apenas uma consegue completar a baixa; a outra recebe a informação de saldo atualizado (insuficiente) na sua tentativa seguinte, evitando saldo negativo.

### Idempotência

Implementado no `FaturamentoService`, no endpoint de impressão de nota fiscal (`POST /api/notasfiscais/{id}/imprimir`):

- O endpoint aceita um cabeçalho HTTP opcional `Idempotency-Key`.
- Na primeira chamada com uma determinada chave, a operação é processada normalmente e o resultado (código de status + corpo da resposta) é armazenado em um cache em memória (`ConcurrentDictionary`), associado a essa chave.
- Em chamadas subsequentes com a **mesma** chave, o resultado armazenado é devolvido diretamente, **sem reprocessar** a operação — ou seja, sem abater o saldo do estoque novamente, mesmo que a requisição HTTP seja repetida (por exemplo, devido a uma falha de rede que faça o cliente reenviar a mesma chamada, ou um duplo clique acidental no botão de imprimir).
- Chamadas sem o cabeçalho, ou com uma chave diferente, são processadas normalmente.

**Observação**: a implementação atual utiliza um cache em memória, válido enquanto a aplicação estiver em execução. Em um cenário de produção com múltiplas instâncias do serviço, essa chave seria persistida em um armazenamento compartilhado (banco de dados ou cache distribuído como Redis), para garantir consistência entre instâncias e sobreviver a reinicializações.

## Observação técnica: ChangeDetectorRef

Durante o desenvolvimento, foi identificado que a detecção automática de mudanças do Angular não disparava de forma consistente neste projeto após respostas assíncronas (chamadas HTTP), mesmo com o Zone.js corretamente instalado. O comportamento foi contornado de forma controlada usando `ChangeDetectorRef.detectChanges()` manualmente após a atualização do estado dos componentes. Essa abordagem gera um aviso de desenvolvimento (`NG0100`) em cenários de atualização encadeada (ex: excluir um item e recarregar a lista em seguida), que não afeta o funcionamento da aplicação e não é exibido em builds de produção.

## Como rodar o projeto localmente

### Pré-requisitos
- .NET SDK 10
- Node.js e Angular CLI

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

Acesse `http://localhost:4200`. É necessário que os três (EstoqueService, FaturamentoService e o frontend) estejam rodando simultaneamente para o funcionamento completo do sistema.

## Status do desenvolvimento

- [x] Cadastro de Produtos (backend + frontend completos)
- [x] Cadastro de Notas Fiscais com numeração sequencial (backend + frontend completos)
- [x] Comunicação entre microsserviços via HTTP
- [x] Tratamento de falha e recuperação (testado via interface)
- [x] Impressão de nota fiscal com abatimento de saldo
- [x] Persistência real em banco de dados 
- [x] Tratamento de concorrência 
- [x] Idempotência 
- [ ] Vídeo de apresentação
- [ ] Uso de IA — não implementado

## Autor

Jonas Vicente