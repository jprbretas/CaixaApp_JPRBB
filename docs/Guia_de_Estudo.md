# Guia de estudo

Para estudar o projeto ficheiro a ficheiro: o que cada ficheiro faz e, numa linha, o que faz cada
método. Começa pelo caminho que um pedido percorre, que liga quase tudo; depois vem cada projeto,
pela ordem em que os dados passam (Core → ApiService → Web), os testes e, no fim, os conceitos de
C# e .NET que vale a pena rever.

Para os porquês das decisões, ver o [DECISIONS.md](../DECISIONS.md).

**Índice:**
[O caminho de um pedido](#o-caminho-de-um-pedido) ·
[Core](#caixaprojetocore-as-regras) ·
[ApiService](#caixaprojetoapiservice-a-api-e-a-base-de-dados) ·
[Web](#caixaprojetoweb-as-páginas) ·
[AppHost e ServiceDefaults](#caixaprojetoapphost-e-caixaprojetoservicedefaults-o-aspire) ·
[Testes](#os-testes) ·
[Outros ficheiros](#outros-ficheiros) ·
[Conceitos para rever](#conceitos-para-rever)

---

## O caminho de um pedido

O que acontece quando alguém carrega em **Submeter pedido**, passo a passo:

| # | Onde | O que acontece |
|---|---|---|
| 1 | `Web/Components/Pages/NovoPedido.razor` → `SubmeterAsync` | O clique chega ao servidor (Blazor Server). O formulário é convertido em `PedidoCredito` com `PedidoForm.ParaPedido()`. |
| 2 | `Web/Services/CreditoApiClient.cs` → `SubmeterAsync` | Envia o pedido em JSON para a API: `POST /api/pedidos`. |
| 3 | `ApiService/Program.cs` → `MapPost("/api/pedidos")` | O endpoint recebe o JSON, converte-o em `PedidoCredito` e chama o serviço. |
| 4 | `ApiService/Services/PedidoService.cs` → `SubmeterAsync` | Limpa o NIF (`Normalizado`), pede a análise ao motor, atribui o número e prepara o que vai ser gravado. |
| 5 | `Core/MotorDecisao.cs` → `Analisar` | Regra 1 (`ValidacaoInicial`), indicadores (`CalculoIndicadores`), regras 2 a 7 (`RegrasNegocio`) e Regra 8 (fica a mais restritiva). |
| 6 | `ApiService/Data/CaixaDbContext.cs` | O `SaveChangesAsync` grava pedido, cliente, motivos e histórico na `caixa.db`, numa só transação. |
| 7 | De volta pelo mesmo caminho | A API devolve `PedidoSubmetido` (número + resultado), e a página mostra-o com o `ResultadoView`. |

Os outros botões seguem o mesmo caminho com outros métodos: **Analisar** → `PreAnalisarAsync` →
`POST /api/pedidos/preanalise` → `SimularAsync`; **Aprovar/Recusar** → `DecidirAsync` →
`POST /api/pedidos/{numero}/decisao` → `DecidirAsync`.

---

## CaixaProjeto.Core (as regras)

Não depende de web nem de base de dados. É o "cérebro" da aplicação.

| Ficheiro | O que faz |
|---|---|
| `Dominio/Enums.cs` | As listas fixas de valores: situações profissionais e decisões, e os textos para o ecrã. |
| `Dominio/ParametrosRegras.cs` | Os limites das regras (18, 75, 20×, 35%, 50%, 50.000 €). |
| `Dominio/PedidoCredito.cs` | Os 8 dados de entrada de um pedido. |
| `Dominio/ResultadoAnalise.cs` | O que sai da análise: decisão, motivos e indicadores. |
| `Contratos/Pedidos.cs` | Os objetos que viajam em JSON entre a API e a Web. |
| `Regras/ValidacaoInicial.cs` | A Regra 1. |
| `Regras/CalculoIndicadores.cs` | O cálculo dos indicadores. |
| `Regras/RegrasNegocio.cs` | As regras 2 a 7, uma classe por regra. |
| `MotorDecisao.cs` | O ponto de entrada: junta tudo e aplica a Regra 8. |

### Dominio/Enums.cs

| Tipo / método | O que faz |
|---|---|
| `enum SituacaoProfissional` | `Efetivo`, `ContratoPrazo`, `Desempregado`. |
| `enum Decisao` | `Aprovado` (0), `AnaliseManual` (1), `Recusado` (2), `PedidoInvalido` (3): o número é a severidade, por isso a Regra 8 é só "o maior". |
| `Texto(this Decisao)` | Devolve o texto para o ecrã ("ANÁLISE MANUAL"). É um método de extensão: escreve-se `decisao.Texto()`. |
| `Texto(this SituacaoProfissional)` | Devolve o texto para o ecrã ("Contrato a prazo"). |

### Dominio/ParametrosRegras.cs

| Propriedade | O que guarda |
|---|---|
| `IdadeMinima` | Idade mínima do cliente (Regra 1, 18). |
| `IdadeMaximaFinalContrato` | Idade máxima no fim do contrato (Regra 2, 75). |
| `MultiploRendimentoMaximo` | Quantas vezes o rendimento se pode pedir (Regra 5, 20). |
| `TaxaEsforcoAnaliseManual` / `TaxaEsforcoRecusa` | Limites da taxa de esforço (Regra 6, 35% e 50%). |
| `MontanteElevado` | A partir de que valor vai a análise manual (Regra 7, 50.000 €). |

### Dominio/PedidoCredito.cs

| Membro | O que faz |
|---|---|
| `Nif`, `Idade`, `RendimentoMensalLiquido`, `PrestacoesAtuais`, `ValorPretendido`, `PrazoMeses`, `SituacaoProfissional`, `IncidentesCredito` | Os 8 campos do enunciado (com `init`: só se dão valores ao criar). |
| `Normalizado()` | Devolve uma cópia com o NIF sem espaços, pontos nem hífenes. |

### Dominio/ResultadoAnalise.cs

| Tipo | O que guarda |
|---|---|
| `record Indicadores` | Prestação estimada, taxa de esforço, idade no fim do contrato e montante máximo. |
| `record Motivo` | Uma razão da decisão: a regra, a descrição e a decisão que essa regra pede. |
| `record ResultadoAnalise` | A decisão final, a lista de motivos e os indicadores (`null` se o pedido for inválido). |

### Contratos/Pedidos.cs

| Tipo / membro | O que guarda ou faz |
|---|---|
| `PedidoSubmetido` | Resposta ao submeter: o número atribuído e o resultado. |
| `PedidoResumo` | Uma linha da lista de pedidos. |
| `Pagina<T>` | Uma página de resultados: os itens, o total, o número e o tamanho da página. |
| `Pagina<T>.TotalPaginas` | Calcula quantas páginas há (arredonda para cima; com 0 itens, 1). |
| `EstadoHistorico` | Uma mudança de estado: de, para, data, quem e observação. |
| `PedidoDetalhe` | Tudo sobre um pedido: dados, resultado, estado atual e histórico. |
| `PedidoDetalhe.AguardaAnalista` | `true` se o pedido estiver em análise manual. |
| `DecisaoAnalista` | O que o analista envia: aprovar ou não, o nome e a observação. |

### Regras/ValidacaoInicial.cs

| Método | O que faz |
|---|---|
| `Validar(pedido, parametros)` | Confere os dados (NIF, idade, rendimento, valor, prazo, prestações, situação) e devolve todos os erros da Regra 1 de uma vez. |
| `TemNoveDigitos(nif)` | Diz se o NIF tem exatamente 9 algarismos. |

### Regras/CalculoIndicadores.cs

| Método | O que faz |
|---|---|
| `Calcular(pedido, parametros)` | Calcula a prestação (valor ÷ prazo), a taxa de esforço, a idade no fim do contrato e o limite do montante, arredondados a 2 casas. |

### Regras/RegrasNegocio.cs

Cada classe tem um só método, `Avaliar`, que devolve um `Motivo` se a regra disparar, ou `null`.

| Classe | O que o `Avaliar` faz |
|---|---|
| `interface IRegra` | Define o método `Avaliar` que todas as regras têm. |
| `RegraIdadeFinal` | Regra 2: idade no fim do contrato acima de 75 → ANÁLISE MANUAL. |
| `RegraIncidentes` | Regra 3: incidentes de crédito → RECUSADO. |
| `RegraSituacaoProfissional` | Regra 4: contrato a prazo → ANÁLISE MANUAL; desempregado → RECUSADO. |
| `RegraLimiteMontante` | Regra 5: valor acima de 20 × o rendimento → ANÁLISE MANUAL. |
| `RegraTaxaEsforco` | Regra 6: acima de 35% → ANÁLISE MANUAL; acima de 50% → RECUSADO. |
| `RegraMontanteElevado` | Regra 7: valor acima de 50.000 € → ANÁLISE MANUAL. |

### MotorDecisao.cs

| Membro | O que faz |
|---|---|
| `MotorDecisao(parametros)` | Cria o motor com os limites indicados (vêm do `appsettings.json`). |
| `MotorDecisao()` | Cria o motor com os limites do enunciado (usado nos testes). |
| `Regras` | A lista das regras 2 a 7, pela ordem do enunciado. |
| `Analisar(pedido)` | Normaliza o NIF, valida (Regra 1), calcula os indicadores, corre as regras 2 a 7 e escolhe a decisão mais restritiva (Regra 8). |

---

## CaixaProjeto.ApiService (a API e a base de dados)

| Ficheiro | O que faz |
|---|---|
| `Program.cs` | Arranque da API: configuração, base de dados e os endpoints. |
| `appsettings.json` | Os limites das regras (secção `Regras`) e a ligação à base de dados (`ConnectionStrings:caixa`). |
| `Data/Entidades.cs` | As tabelas da base de dados, como classes. |
| `Data/CaixaDbContext.cs` | A "porta" para a base de dados (Entity Framework Core). |
| `Data/Migrations/` | As migrações: como criar e alterar as tabelas. |
| `Services/PedidoService.cs` | Junta o motor e a base de dados. |
| `Services/ResultadoDecisao.cs` | Os resultados possíveis da decisão do analista. |
| `CaixaProjeto.ApiService.http` | Pedidos de exemplo para enviar à API a partir do Visual Studio. |

### Program.cs

| Parte | O que faz |
|---|---|
| `AddServiceDefaults()` | Liga a configuração comum do Aspire (logs, health checks, service discovery). |
| `ConfigureHttpJsonOptions` | Os enums passam a ir em JSON como texto ("AnaliseManual"). |
| `GetSection("Regras").Get<ParametrosRegras>()` | Lê os limites das regras do `appsettings.json`. |
| `AddDbContext<CaixaDbContext>` + `UseSqlite` | Liga a base de dados SQLite. |
| `AddSingleton` / `AddScoped` | Regista o motor, o relógio e o `PedidoService` (injeção de dependências). |
| `Database.Migrate()` | Ao arrancar, aplica as migrações que faltam. |
| `MapPost("/api/pedidos/preanalise")` | Simula: chama `SimularAsync`. |
| `MapPost("/api/pedidos")` | Submete: chama `SubmeterAsync`. |
| `MapGet("/api/pedidos")` | Lista paginada, com filtro `?estado=`: chama `ListarAsync`. |
| `MapGet("/api/pedidos/{numero}")` | Detalhe: chama `ObterAsync`; responde 404 se não existir. |
| `MapPost("/api/pedidos/{numero}/decisao")` | Decisão do analista: chama `DecidirAsync` e traduz o resultado em 200, 400, 404 ou 409. |

### Data/Entidades.cs

| Classe | Tabela |
|---|---|
| `Cliente` | `Clientes`: um cliente por NIF, com os seus pedidos e simulações. |
| `Pedido` | `Pedidos`: dados de entrada, indicadores, decisão automática, estado atual e data. |
| `Simulacao` | `Simulacoes`: cada clique em "Analisar", com os dados e a decisão. |
| `MotivoPedido` | `MotivosPedido`: um motivo da decisão automática por linha. |
| `HistoricoEstado` | `HistoricoEstados`: cada mudança de estado, com quem e porquê. |

### Data/CaixaDbContext.cs

| Membro | O que faz |
|---|---|
| `Clientes`, `Pedidos`, `MotivosPedido`, `HistoricoEstados`, `Simulacoes` | Uma propriedade `DbSet` por tabela: é por elas que se consulta e grava. |
| `OnModelCreating` | Afina as tabelas: índices (NIF e número únicos, datas), enums gravados como texto e as ligações entre tabelas. |
| `ConfigureConventions` | Grava todos os `decimal` como número (REAL), porque o SQLite não tem tipo decimal. |

### Data/Migrations/

| Ficheiro | O que faz |
|---|---|
| `..._Inicial.cs` → `Up` | Cria as 5 tabelas e os índices. |
| `..._Inicial.cs` → `Down` | Apaga-as (desfaz a migração). |
| `..._Inicial.Designer.cs` | Gerado pelo `dotnet ef`; não se mexe. |
| `CaixaDbContextModelSnapshot.cs` | A "fotografia" do modelo atual, para a próxima migração saber o que mudou. |

### Services/PedidoService.cs

| Método | O que faz |
|---|---|
| `UtilizadorSistema` | O nome gravado no histórico para a decisão automática ("sistema"). |
| `SimularAsync(dados)` | Analisa, grava a simulação e devolve o resultado, sem criar pedido. |
| `SubmeterAsync(dados)` | Analisa, atribui o número, liga ao cliente e grava pedido, motivos e histórico numa só transação. |
| `ListarAsync(estado, pagina, tamanho)` | Devolve uma página de pedidos, do mais recente para o mais antigo, filtrada por estado se pedido. |
| `ObterAsync(numero)` | Lê um pedido com os motivos e o histórico; devolve `null` se não existir. |
| `DecidirAsync(numero, decisao)` | Aplica a decisão do analista a um pedido em análise manual: muda o estado e acrescenta uma linha ao histórico. |
| `ProximoNumeroAsync(ano)` (privado) | Calcula o próximo número do ano (20260001, 20260002...). |
| `ObterOuCriarClienteAsync(nif)` (privado) | Encontra o cliente pelo NIF, ou cria-o; com NIF inválido, não há cliente. |

### Services/ResultadoDecisao.cs

| Valor | Quando |
|---|---|
| `Decidido` | A decisão foi gravada. |
| `DadosEmFalta` | Falta o nome do analista ou a observação. |
| `NaoEncontrado` | Não existe pedido com esse número. |
| `NaoAguardaAnalista` | O pedido não está em análise manual. |

---

## CaixaProjeto.Web (as páginas)

| Ficheiro | O que faz |
|---|---|
| `Program.cs` | Arranque da Web: liga o Blazor e regista o `CreditoApiClient` com o endereço `https+http://apiservice`. |
| `Components/App.razor` | A página HTML "mãe": letra, Bootstrap, CSS, ícone e o script do Blazor. |
| `Components/Routes.razor` | Encontra a página certa pelo `@page` e usa o `MainLayout`. |
| `Components/_Imports.razor` | Os `@using` partilhados por todas as páginas. |
| `Components/Layout/MainLayout.razor` | A moldura: aviso de demonstração, cabeçalho, conteúdo e rodapé. |
| `Components/Layout/NavMenu.razor` | O cabeçalho com a marca e o menu. |
| `Components/Layout/NavMenu.razor.js` | No telemóvel, fecha o menu depois de escolher uma página. |
| `Components/Pages/Home.razor` | Página inicial (`/`), só com conteúdo (sem métodos). |
| `Components/Pages/NovoPedido.razor` | Formulário e resultado (`/pedidos/novo`). |
| `Components/Pages/Pedidos.razor` | Lista de pedidos (`/pedidos`). |
| `Components/Pages/DetalhePedido.razor` | Detalhe e decisão do analista (`/pedidos/{numero}`). |
| `Components/Pages/Error.razor` | Página de erro do modelo do Blazor. |
| `Components/Shared/ResultadoView.razor` | Cartão com a decisão, os motivos e os indicadores. |
| `Components/Shared/Marca.razor` | O símbolo e o nome da aplicação. |
| `Components/Shared/CabecalhoPagina.razor` | O título de cada página. |
| `Components/Shared/Formatos.cs` | Formata valores para o ecrã. |
| `Models/PedidoForm.cs` | O modelo do formulário. |
| `Services/CreditoApiClient.cs` | Faz os pedidos HTTP à API. |
| `Services/ErroDaApiException.cs` | A exceção "a API respondeu com um erro" e as mensagens de erro. |
| `wwwroot/app.css` | A identidade visual (cores em variáveis no início). |
| `wwwroot/favicon.svg` | O ícone do separador do browser. |

### Components/Pages/NovoPedido.razor

| Método | O que faz |
|---|---|
| `Preencher(cenario)` | Preenche o formulário com um dos cenários A a D. |
| `Limpar()` | Esvazia o formulário e o resultado. |
| `LimparResultado()` | Apaga o resultado, o número gravado e o erro. |
| `AnalisarAsync()` | Botão "Analisar": pede a simulação à API. |
| `SubmeterAsync()` | Botão "Submeter pedido": submete e guarda o número atribuído. |
| `ChamarApiAsync(chamada)` | Trata o que os dois botões têm em comum: o "a trabalhar" e os erros de ligação. |

### Components/Pages/Pedidos.razor

| Método | O que faz |
|---|---|
| `OnInitializedAsync()` | Quando a página abre, carrega a primeira página sem filtro. |
| `MudarEstadoAsync(e)` | Quando o filtro muda, guarda o estado escolhido e volta à página 1. |
| `IrParaPaginaAsync(numero)` | Botões Anterior / Seguinte. |
| `CarregarAsync(numero)` | Pede a página à API e trata os erros. |

### Components/Pages/DetalhePedido.razor

| Membro | O que faz |
|---|---|
| `Numero` (`[Parameter]`) | O número do pedido, que vem do endereço (`/pedidos/20260001`). |
| `OnParametersSetAsync()` | Carrega o pedido (e volta a carregar se o número no endereço mudar). |
| `DecidirAsync(aprovar)` | Botões Aprovar / Recusar: valida, envia a decisão e volta a ler o pedido. |
| `TextoSituacao(situacao)` | Texto da situação profissional, ou "-" se estiver em falta. |

### Components/Pages/Error.razor

| Método | O que faz |
|---|---|
| `OnInitialized()` | Guarda o identificador do pedido que falhou, para o mostrar. |

### Components/Shared

| Componente / método | O que faz |
|---|---|
| `ResultadoView` → `Resultado` (`[Parameter]`) | O resultado a mostrar no cartão. |
| `CabecalhoPagina` → `Eyebrow`, `Titulo`, `Descricao` | A linha pequena em azul, o título e a descrição. |
| `CabecalhoPagina` → `ChildContent` | O que se escreve entre as etiquetas aparece à direita do título. |
| `Marca` | Sem parâmetros: só o símbolo (SVG) e o nome. |
| `Formatos.Euros(valor)` | "1.234,56 €". |
| `Formatos.Percentagem(valor)` | "45,83%". |
| `Formatos.Anos(valor)` | "75,1 anos". |
| `Formatos.DataHora(dataUtc)` | Converte de UTC para a hora local e formata "30/09/2026 14:24". |
| `Formatos.CorDecisao(decisao)` | A cor do Bootstrap de cada decisão (success, warning, danger, secondary). |

### Models/PedidoForm.cs

| Membro | O que faz |
|---|---|
| Os 8 campos (com `set`) | O que o formulário preenche; o Blazor precisa de `set` para o `@bind`. |
| `ParaPedido()` | Converte o formulário num `PedidoCredito` para enviar à API. |
| `Cenarios` | Os quatro cenários do enunciado (A a D). |
| `Copia()` | Uma cópia do formulário, para mexer num cenário sem estragar o original. |

### Services/CreditoApiClient.cs

| Método | O que faz |
|---|---|
| `PreAnalisarAsync(pedido)` | `POST /api/pedidos/preanalise`: devolve o resultado da simulação. |
| `SubmeterAsync(pedido)` | `POST /api/pedidos`: devolve o número e o resultado. |
| `ListarAsync(estado, pagina, tamanho)` | `GET /api/pedidos`: devolve uma página da lista. |
| `ObterAsync(numero)` | `GET /api/pedidos/{numero}`: devolve o detalhe, ou `null` se a API responder 404. |
| `DecidirAsync(numero, decisao)` | `POST .../decisao`: devolve `null` se correu bem, ou a explicação da API se a decisão não foi aceite. |
| `CriarErroAsync(resposta)` (privado) | Transforma uma resposta de erro numa `ErroDaApiException`, com a explicação e o código. |
| `LerProblemaAsync(resposta)` (privado) | Lê o corpo de um erro no formato Problem Details; `null` se não for JSON. |

### Services/ErroDaApiException.cs

| Membro | O que faz |
|---|---|
| `ErroDaApiException` | A exceção para "a API respondeu com um erro" (deriva de `HttpRequestException`). |
| `Detalhe` | A explicação que a API deu (campo `detail`). |
| `Codigo` | O `traceId`: o código que o suporte procura nos logs. |
| `MensagensErro.SemLigacao` | A frase para quando a API não responde. |
| `MensagensErro.Para(erro)` | Escolhe a frase a mostrar: "não foi possível contactar" ou "respondeu com um erro (500)... código". |

---

## CaixaProjeto.AppHost e CaixaProjeto.ServiceDefaults (o Aspire)

### AppHost/AppHost.cs

| Parte | O que faz |
|---|---|
| `AddProject<...ApiService>("apiservice")` | Regista a API com o nome `apiservice`. |
| `.WithHttpHealthCheck("/health")` | O Aspire pergunta à API se está pronta. |
| `AddProject<...Web>("webfrontend")` | Regista a Web. |
| `.WithReference(apiService)` | Diz à Web onde está a API (é daqui que vem o `https+http://apiservice`). |
| `.WaitFor(apiService)` | A Web só arranca depois de a API estar pronta. |
| `.WithUrlForEndpoint(...)` | Põe o link "Abrir a aplicação" no dashboard. |

### ServiceDefaults/Extensions.cs (veio com o modelo)

| Método | O que faz |
|---|---|
| `AddServiceDefaults()` | Liga tudo o que está abaixo, mais o service discovery e as tentativas repetidas nas chamadas HTTP. |
| `ConfigureOpenTelemetry()` | Recolhe logs, métricas e "traces" para o dashboard do Aspire. |
| `AddOpenTelemetryExporters()` (privado) | Envia a telemetria para o dashboard, se estiver configurado. |
| `AddDefaultHealthChecks()` | Cria a verificação "estou vivo". |
| `MapDefaultEndpoints()` | Expõe `/health` e `/alive` (só em desenvolvimento). |

---

## Os testes

### CaixaProjeto.UnitTests / MotorDecisaoTests.cs (37 testes contando os `[Theory]`, só o Core)

| Teste | O que confirma |
|---|---|
| `CenarioA_Aprovado` | Cenário A: APROVADO, sem motivos. |
| `CenarioB_AnaliseManual_PorContratoPrazoETaxaEsforco` | Cenário B: ANÁLISE MANUAL pelas regras 4 e 6. |
| `CenarioC_Recusado_PorIncidentes` | Cenário C: RECUSADO pela Regra 3. |
| `CenarioD_Recusado_PorTaxaEsforco_ComMontanteAcimaDoLimite` | Cenário D: RECUSADO pela Regra 6, com o motivo da Regra 5 também. |
| `Regra1_NifSemNoveDigitos_Invalido` | NIFs errados (8 ou 10 dígitos, letras, vazio, só espaços) dão inválido. |
| `Regra1_NifComEspacosPontosOuHifenes_Valido` | "123 456 789", "123-456-789" e "123.456.789" são válidos. |
| `Normalizado_TiraSoOsSeparadores_ENaoMudaOResto` | O `Normalizado()` só mexe no NIF. |
| `Regra1_NifComZeroAEsquerda_Valido` | "012345678" é válido. |
| `Regra1_Idade18_Valido_Idade17_Invalido` | O limite de idade. |
| `Regra1_ValoresAZeroOuNegativos_Invalido` | Rendimento, valor e prazo a zero dão inválido. |
| `Regra1_CasosNaoPrevistos_PrestacoesNegativasESemSituacao_Invalido` | Os dois casos que acrescentei à Regra 1. |
| `Regra1_JuntaTodosOsErros_ENaoAvaliaAsOutrasRegras` | Os erros vêm todos juntos e as outras regras não correm. |
| `Regra2_IdadeFinalExatamente75_NaoDispara` / `..._AcimaDe75_AnaliseManual` | O limite dos 75 anos. |
| `Regra3_Incidentes_Recusado` | Incidentes dão RECUSADO. |
| `Regra4_SituacaoProfissional` | As três situações profissionais. |
| `Regra5_MontanteIgualA20xRendimento_NaoDispara` | O limite dos 20×. |
| `Regra6_LimitesDaTaxaDeEsforco` | 35,00% / 35,01% / 50,00% / 50,01%. |
| `Regra7_Exatamente50000_NaoDispara_Acima_AnaliseManual` | O limite dos 50.000 €. |
| `Regra8_MontanteElevadoNaoAnulaRecusa` | A Regra 7 não anula uma recusa. |
| `Regra8_AcumulaTodosOsMotivos_EFicaOMaisRestritivo` | Com várias regras, ficam todos os motivos e a decisão mais restritiva. |
| `Parametros_LimitesConfiguraveis` | Mudar um limite muda a decisão. |

### CaixaProjeto.ApiTests (44 testes, com base de dados em memória)

**BaseDadosDeTeste.cs** (as ferramentas dos testes):

| Membro | O que faz |
|---|---|
| `RelogioDeTeste.GetUtcNow()` | Devolve a hora que o teste escolher, em vez da hora real. |
| `BaseDadosDeTeste()` | Abre uma base de dados SQLite em memória e aplica as migrações. |
| `NovoContexto()` | Um `DbContext` novo sobre essa base de dados. |
| `Servico()` | Um `PedidoService` pronto a usar, com o relógio de teste. |
| `Dispose()` | Fecha a ligação (e a base de dados em memória desaparece). |
| `Exemplos.Aprovado` / `AnaliseManual` / `Recusado` / `Invalido` | Pedidos de exemplo. |

**PedidoServiceTests.cs** (18 métodos, 26 testes contando os `[Theory]`):

| Teste | O que confirma |
|---|---|
| `Submeter_GravaPedidoMotivosHistoricoECliente` | O que fica gravado ao submeter. |
| `Submeter_NumerosSeguidos_ERecomecamNoAnoSeguinte` | 20260001, 20260002 e depois 20270001. |
| `Submeter_MesmoNif_ReaproveitaOCliente` | Dois pedidos do mesmo NIF dão um só cliente. |
| `Submeter_NifComEspacos_GravaONifLimpo_EOMesmoCliente` | As três formas de escrever o NIF dão o mesmo cliente. |
| `Submeter_PedidoInvalido_GravaSemClienteESemIndicadores` | Um inválido fica gravado, sem cliente nem indicadores. |
| `Simular_GravaSimulacao_ENaoCriaPedido` | Simular não cria pedido. |
| `Simular_EDepoisSubmeter_UsamOMesmoCliente` | A simulação e o pedido ficam no mesmo cliente. |
| `Simular_NifInvalido_GravaSemCliente` | Simulação com NIF inválido fica sem cliente. |
| `Listar_DoMaisRecenteParaOMaisAntigo_ComFiltroEPaginas` | Ordem, filtro e páginas. |
| `Listar_ValoresSemSentido_UsamOsValoresPorOmissao` | Página 0 ou tamanho 500 são corrigidos. |
| `Obter_PedidoQueNaoExiste_DevolveNull` | Número inexistente dá `null`. |
| `Obter_DevolveDadosAnaliseEHistorico_TalComoForamGravados` | O detalhe é o que foi gravado. |
| `Obter_PedidoInvalido_NaoTemIndicadores` | Um inválido não tem indicadores. |
| `Decidir_MudaOEstado_AcrescentaHistorico_ENaoMudaADecisaoAutomatica` | Aprovar e recusar. |
| `Decidir_SemNomeOuSemObservacao_DadosEmFalta_ENaoMudaNada` | Os campos obrigatórios. |
| `Decidir_PedidoQueNaoExiste_NaoEncontrado` | Número inexistente. |
| `Decidir_PedidoQueNaoEstaEmAnaliseManual_NaoAguardaAnalista` | Só se decidem pedidos em análise manual. |
| `Decidir_DuasVezes_ASegundaJaNaoEAceite` | Não se decide duas vezes. |

**EndpointsTests.cs** (8 testes da API por HTTP):

| Membro | O que faz ou confirma |
|---|---|
| `ApiDeTeste` | Arranca a API verdadeira dentro do teste (`WebApplicationFactory`). |
| `ApiDeTeste.ConfigureWebHost` | Troca a `caixa.db` por uma base de dados em memória. |
| `SubmeterAsync` / `DecidirAsync` (ajudas) | Enviam um pedido ou uma decisão à API. |
| `Preanalise_DevolveOResultado_ComAsDecisoesEmTexto` | As decisões vão em texto no JSON. |
| `Submeter_EDepoisObter_DevolveODetalhe` | Submeter e ler o detalhe. |
| `Obter_PedidoQueNaoExiste_Responde404` | 404. |
| `Listar_ComFiltro_SoDevolvePedidosNesseEstado` | O filtro por estado. |
| `Decidir_PedidoEmAnaliseManual_Responde200_ComODetalheAtualizado` | 200, com os acentos intactos. |
| `Decidir_SemObservacao_Responde400_ComAMensagem` | 400 com a mensagem. |
| `Decidir_PedidoQueNaoExiste_Responde404` | 404. |
| `Decidir_PedidoJaDecidido_Responde409` | 409. |

**ConsultasTarefa4Tests.cs** (7 testes das consultas SQL):

| Membro | O que faz ou confirma |
|---|---|
| `ConsultasDoFicheiro()` | Lê o `sql/Tarefa4_Consultas.sql` e separa as 5 consultas. |
| `Correr(numero)` | Corre uma consulta e devolve as linhas. |
| `PrepararDados()` | Cria os pedidos e simulações de teste (alguns com 60 dias). |
| `Consulta1_...` a `Consulta5_...` | Cada consulta dá o resultado esperado, incluindo o empate na 3 e a base de dados vazia na 2. |

**MigracoesTests.cs** (3 testes):

| Teste | O que confirma |
|---|---|
| `NaoHaMudancasNoModeloSemMigracao` | Ninguém mudou uma entidade sem criar a migração. |
| `TodasAsMigracoesForamAplicadas_ENaoFaltaNenhuma` | A `Inicial` foi aplicada e não falta nenhuma. |
| `AsMigracoesCriamTodasAsTabelas` | As 5 tabelas e a `__EFMigrationsHistory` existem. |

### CaixaProjeto.WebTests / CreditoApiClientTests.cs (10 testes)

| Membro | O que faz ou confirma |
|---|---|
| `ApiFalsa.SendAsync` | Faz de API: devolve a resposta que o teste quiser, sem rede. |
| `Cliente(...)` / `Problema(...)` (ajudas) | Criam o cliente com a API falsa e uma resposta de erro no formato da API. |
| `Erro500_LancaErroDaApi_ComOCodigoParaOSuporte` | Um 500 dá `ErroDaApiException` com o código. |
| `Erro500_ComCorpoQueNaoEJson_LancaErroDaApi_SemCodigo` | Uma página HTML de um proxy também é tratada. |
| `TodosOsMetodos_LancamErroDaApi_QuandoAApiRespondeComErro` | Os 5 métodos tratam os erros da mesma forma. |
| `Obter_404_DevolveNull` | 404 dá `null`. |
| `Decidir_DecisaoNaoAceite_DevolveAExplicacaoDaApi` | 400, 404 e 409 devolvem a explicação. |
| `Mensagem_SemLigacao_...` / `Mensagem_ErroDaApi_...` / `Mensagem_ErroDaApiSemCodigo_...` | As frases que as páginas mostram em cada caso. |

### CaixaProjeto.Tests / WebTests.cs (1 teste, veio com o modelo)

| Teste | O que confirma |
|---|---|
| `GetWebResourceRootReturnsOkStatusCode` | Arranca a aplicação inteira pelo Aspire e a página inicial responde. |

---

## Outros ficheiros

| Ficheiro | O que faz |
|---|---|
| `sql/Tarefa4_Consultas.sql` | As 5 consultas da Tarefa 4, comentadas. |
| `sql/correr_consultas.py` | Corre as consultas da Tarefa 4 (ou uma consulta qualquer) e mostra os resultados em tabelas, só para leitura. |
| `CaixaProjeto.slnx` | A solução: a lista dos projetos. |
| `*.csproj` | Cada projeto: a versão do .NET, os pacotes NuGet e as referências a outros projetos. |
| `Properties/launchSettings.json` | Como cada projeto arranca no Visual Studio (portas, ambiente). |
| `appsettings.Development.json` | Configuração só para desenvolvimento (níveis de log). |
| `aspire.config.json` | Diz à ferramenta do Aspire qual é o projeto AppHost. |
| `dotnet-tools.json` | As ferramentas do projeto (`dotnet-ef`), na versão certa. |
| `.gitignore` | O que o git ignora (`bin`, `obj`, `*.db`...). |
| `README.md` / `DECISIONS.md` / `docs/` | A documentação. |

---

## Conceitos para rever

Cada conceito com um exemplo deste projeto.

| Conceito | Em poucas palavras | Onde ver |
|---|---|---|
| `record` | Classe para guardar dados, que compara pelos valores e é imutável. | `PedidoCredito`, `ResultadoAnalise` |
| `with` | Cria uma cópia de um `record` com alguns campos mudados. | `pedido with { Nif = ... }` nos testes e no `Normalizado()` |
| `init` vs `set` | `init` só deixa dar valor ao criar; `set` deixa mudar sempre. | `PedidoCredito` (init) vs `PedidoForm` (set) |
| `decimal?` e `?.` | Um valor que pode ser `null`; e "só lê se não for `null`". | `Pedido.TaxaEsforco`, `resultado.Indicadores?.LimiteMontante` |
| `enum` | Lista fixa de valores com nome. | `Decisao`, `ResultadoDecisao` |
| Método de extensão (`this`) | Um método que parece pertencer a outro tipo. | `decisao.Texto()` em `Enums.cs` |
| Interface | Um contrato: as classes que a implementam têm os mesmos métodos. | `IRegra` |
| Construtor primário | Os parâmetros do construtor escritos logo a seguir ao nome da classe. | `PedidoService(CaixaDbContext db, ...)` |
| Injeção de dependências | O .NET cria os objetos e entrega-os a quem precisa. | `AddScoped<PedidoService>()`, `@inject CreditoApiClient Api` |
| `async` / `await` | Esperar pela base de dados ou pela rede sem bloquear. | Todos os métodos `...Async` |
| LINQ | Consultas escritas em C# (`Where`, `OrderBy`, `Select`). | `ListarAsync` |
| `Include` / `AsNoTracking` | Trazer tabelas ligadas; ler sem o EF vigiar alterações. | `ObterAsync` |
| Migrações | Ficheiros com cada mudança à estrutura da base de dados. | `Data/Migrations/` |
| Minimal APIs | Endpoints definidos com `MapGet` / `MapPost`. | `ApiService/Program.cs` |
| Problem Details | O formato padrão dos erros de uma API (`status`, `detail`, `traceId`). | `Results.Problem(...)` e `LerProblemaAsync` |
| `@page`, `@bind`, `@onclick` | Endereço de uma página; ligar um campo a uma variável; reagir a um clique. | `NovoPedido.razor` |
| `[Parameter]` e `RenderFragment` | Dados que um componente recebe; conteúdo que se passa a um componente. | `ResultadoView`, `CabecalhoPagina` |
| `@rendermode InteractiveServer` | A página reage a cliques através de uma ligação em tempo real. | Páginas com botões |
| `[Fact]` / `[Theory]` + `[InlineData]` | Um teste; um teste que corre várias vezes com dados diferentes. | `MotorDecisaoTests.cs` |
| `TimeProvider` | Dar a hora através de um objeto, para os testes a poderem controlar. | `PedidoService` e `RelogioDeTeste` |
