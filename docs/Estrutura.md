# Estrutura da solução

Guia rápido dos ficheiros que criámos, para que servem e como se ligam. Vai sendo atualizado a cada passo.

## Como os projetos se ligam

```
Browser ──► CaixaProjeto.Web (Blazor) ──HTTP──► CaixaProjeto.ApiService ──► CaixaProjeto.Core (regras)
                    ▲                                  ▲
                    └──────── CaixaProjeto.AppHost arranca os dois e liga-os ──┘
```

- O utilizador só fala com a **Web**.
- A Web pede a análise à **API** por HTTP.
- A API usa o **Core**, que é onde vivem as regras de negócio.
- O **AppHost** (Aspire) arranca tudo com um F5.

## Projetos

| Projeto | Para que serve |
|---|---|
| `CaixaProjeto.AppHost` | "Arrancador" do Aspire. Define que existem dois serviços (`apiservice` e `webfrontend`) e que a Web depende da API. Abre também o dashboard do Aspire com logs e estado. **É o projeto de arranque.** |
| `CaixaProjeto.ServiceDefaults` | Configuração comum aos serviços: logs/telemetria, health checks (`/health`), resiliência e service discovery (encontrar a API pelo nome). Não mexemos nele. |
| `CaixaProjeto.Core` | O domínio e o motor de decisão. Não depende de web nem de BD, por isso é fácil de testar. |
| `CaixaProjeto.ApiService` | API HTTP. Recebe pedidos em JSON, chama o motor, grava na BD SQLite e devolve o resultado. |
| `CaixaProjeto.Web` | A aplicação Blazor (o ecrã). |
| `CaixaProjeto.UnitTests` | Testes unitários do motor (só o Core): cenários A a D, Regra 1, valores-limite e prioridades. Correm em menos de 1 segundo. |
| `CaixaProjeto.Tests` | Teste de integração: arranca a app inteira pelo Aspire e confirma que a página inicial responde. Mais lento. |

## CaixaProjeto.Core

| Ficheiro | Para que serve |
|---|---|
| `Dominio/Enums.cs` | `SituacaoProfissional` e `Decisao`. A decisão tem um número que é a severidade: é isso que permite à Regra 8 escolher "a mais restritiva" com um simples máximo. Também tem os textos para o ecrã ("ANÁLISE MANUAL"...). |
| `Dominio/PedidoCredito.cs` | Os 8 campos de entrada do enunciado. |
| `Dominio/ResultadoAnalise.cs` | O que sai da análise: `Decisao`, lista de `Motivo` (regra + descrição + decisão pedida) e `Indicadores`. |
| `Dominio/ParametrosRegras.cs` | Os limites (18, 75, 20×, 35%, 50%, 50.000 €). Os valores vêm do `appsettings.json` da API. |
| `Regras/ValidacaoInicial.cs` | Regra 1. Junta todos os erros de uma vez. |
| `Regras/CalculoIndicadores.cs` | Prestação, taxa de esforço, idade no fim do contrato, limite de montante. |
| `Contratos/Pedidos.cs` | Os objetos que viajam em JSON entre a API e a Web: `PedidoSubmetido` (número + resultado), `PedidoResumo` e `Pagina<T>` (lista), `PedidoDetalhe` e `EstadoHistorico` (detalhe), `DecisaoAnalista`. Estão no Core porque a API e a Web partilham estes "contratos". |
| `Regras/RegrasNegocio.cs` | Interface `IRegra` e uma classe por regra, da 2 à 7. Para acrescentar uma regra nova, cria-se mais uma classe. |
| `MotorDecisao.cs` | O ponto de entrada: valida, calcula, corre as regras e aplica a Regra 8. |

## CaixaProjeto.ApiService

| Ficheiro | Para que serve |
|---|---|
| `Program.cs` | Arranque da API. Lê a secção `Regras` do appsettings, regista o `MotorDecisao`, liga a BD e expõe `POST /api/pedidos/preanalise` (simula e regista a simulação), `POST /api/pedidos` (analisa e grava) e `GET /api/pedidos` (lista paginada, com filtro opcional `?estado=`), `GET /api/pedidos/{numero}` (detalhe, ou 404 se não existir) e `POST /api/pedidos/{numero}/decisao` (decisão do analista; os erros vêm em formato Problem Details com 400, 404 ou 409). |
| `appsettings.json` | Configuração: limites das regras e a ligação à BD (`ConnectionStrings:caixa`). |
| `Data/Entidades.cs` | As tabelas: `Cliente`, `Pedido`, `MotivoPedido`, `HistoricoEstado` e `Simulacao`. |
| `Data/CaixaDbContext.cs` | A "porta" para a BD (EF Core). Define índices (NIF e número únicos), grava os enums como texto e os decimais como número. |
| `Services/PedidoService.cs` | Junta motor e BD. `SimularAsync` analisa e regista a simulação, sem criar pedido. `SubmeterAsync` analisa o pedido, atribui o número (ano + sequência, ex. 20260001), liga-o ao cliente pelo NIF e grava motivos e histórico. `ListarAsync` devolve uma página da lista, do mais recente para o mais antigo. `ObterAsync` lê um pedido com motivos e histórico, tal como foi gravado (o motor não volta a correr). `DecidirAsync` aplica a decisão do analista: só em pedidos em ANÁLISE MANUAL, com nome e observação obrigatórios; muda o `EstadoAtual` e acrescenta uma linha ao histórico. |
| `Services/ResultadoDecisao.cs` | Os resultados possíveis da decisão do analista (`Decidido`, `DadosEmFalta`, `NaoEncontrado`, `NaoAguardaAnalista`), que o `Program.cs` traduz em códigos HTTP. |
| `CaixaProjeto.ApiService.http` | Pedidos de exemplo que se podem enviar diretamente do Visual Studio (botão "Send request"). |
| `caixa.db` | O ficheiro SQLite. É criado no primeiro arranque (`EnsureCreated`) e não vai para o git. Apagar = recomeçar do zero. |

### Modelo de dados

```
Clientes         (Id, Nif UNIQUE, DataRegisto)
Pedidos          (Id, Numero UNIQUE, ClienteId → Clientes (pode ser NULL se o NIF for inválido),
                  Nif, Idade, RendimentoMensalLiquido, PrestacoesAtuais, ValorPretendido, PrazoMeses,
                  SituacaoProfissional, IncidentesCredito,
                  PrestacaoEstimada, TaxaEsforco, IdadeFinalContrato, LimiteMontante,
                  DecisaoAutomatica, EstadoAtual, DataSubmissao)
MotivosPedido    (Id, PedidoId → Pedidos, Regra, Descricao, Decisao)
HistoricoEstados (Id, PedidoId → Pedidos, EstadoAnterior, EstadoNovo, Data, Utilizador, Observacao)
Simulacoes       (Id, ClienteId → Clientes (pode ser NULL), Nif, Idade, RendimentoMensalLiquido,
                  PrestacoesAtuais, ValorPretendido, PrazoMeses, SituacaoProfissional,
                  IncidentesCredito, Decisao, DataSimulacao)
```

- `DecisaoAutomatica` é a do motor e nunca muda. `EstadoAtual` pode mudar quando um analista decide.
- Cada mudança de estado fica em `HistoricoEstados`. É daí que sai "quantos pedidos passaram de ANÁLISE MANUAL a APROVADO".
- Os pedidos inválidos também são gravados, para auditoria e reporting.
- Cada simulação (botão "Analisar") fica em `Simulacoes`, separada dos pedidos: não tem número, estado nem histórico.

## sql

| Ficheiro | Para que serve |
|---|---|
| `Tarefa4_Consultas.sql` | As cinco consultas da Tarefa 4 do enunciado, em SQL para SQLite, comentadas. Correm sobre a `caixa.db` num programa como o DB Browser for SQLite (abrir a base de dados, separador "Execute SQL"). |

## CaixaProjeto.Web

| Ficheiro | Para que serve |
|---|---|
| `Program.cs` | Arranque da Web. Regista o `CreditoApiClient` com o endereço `https+http://apiservice`, que o Aspire traduz para o endereço real da API. |
| `Services/CreditoApiClient.cs` | Faz os pedidos HTTP à API: simular (`PreAnalisarAsync`), gravar (`SubmeterAsync`), listar (`ListarAsync`), ler um pedido (`ObterAsync`, devolve null se a API responder 404) e enviar a decisão do analista (`DecidirAsync`, devolve null ou a mensagem de erro da API). |
| `Models/PedidoForm.cs` | Modelo do formulário (com `set`, porque o Blazor precisa) e os cenários A a D para preencher num clique. |
| `Components/App.razor` | A página HTML "mãe": carrega o Bootstrap, o CSS e o script do Blazor. |
| `Components/Routes.razor` | Diz ao Blazor para encontrar as páginas pelo `@page` e usar o `MainLayout`. |
| `Components/_Imports.razor` | `@using` partilhados por todos os componentes. |
| `Components/Layout/MainLayout.razor` | Moldura de todas as páginas: menu à esquerda, barra em cima, conteúdo no meio. |
| `Components/Layout/NavMenu.razor` | O menu lateral. |
| `Components/Pages/Home.razor` | Página inicial (`/`). |
| `Components/Pages/NovoPedido.razor` | Formulário do pedido e resultado (`/pedidos/novo`). "Analisar" simula (fica registada em `Simulacoes`, mas não cria pedido); "Submeter pedido" cria o pedido e mostra o número. É `InteractiveServer`: os cliques são tratados no servidor através de uma ligação em tempo real (SignalR). |
| `Components/Pages/Pedidos.razor` | Lista dos pedidos gravados (`/pedidos`): tabela com 20 por página, botões Anterior/Seguinte e filtro por estado atual. O número de cada pedido abre o detalhe. |
| `Components/Pages/DetalhePedido.razor` | Detalhe de um pedido (`/pedidos/{numero}`): dados, análise automática (com o `ResultadoView`), estado atual e histórico de estados. Nos pedidos em ANÁLISE MANUAL mostra o formulário do analista (nome, observação, Aprovar/Recusar), por isso usa `InteractiveServer`. |
| `Components/Shared/ResultadoView.razor` | Cartão reutilizável com decisão, motivos e indicadores. |
| `Components/Shared/Formatos.cs` | Formatação "1.234,56 €", percentagens, datas (de UTC para a hora local) e a cor de cada decisão. |
| `wwwroot/` | Ficheiros estáticos: `app.css`, Bootstrap e favicon. |
