# Decisões

Este ficheiro explica as escolhas que fiz na aplicação de pré-análise de crédito pessoal e porquê.
Começa pelas três decisões mais importantes; as secções seguintes dão os detalhes. O guia de cada
ficheiro está em [docs/Estrutura.md](docs/Estrutura.md).

**Índice:**
[As três decisões principais](#as-três-decisões-principais) ·
[Stack](#stack) ·
[Arquitetura](#arquitetura) ·
[Interpretação das regras](#interpretação-das-regras) ·
[Casos não previstos e perguntas ao negócio](#casos-não-previstos-e-perguntas-ao-negócio) ·
[Testes](#testes) ·
[Base de dados](#base-de-dados) ·
[API](#api) ·
[Interface](#interface) ·
[Simulações](#simulações) ·
[Decisão do analista](#decisão-do-analista) ·
[Consultas da Tarefa 4](#consultas-da-tarefa-4) ·
[Estilo do código](#estilo-do-código) ·
[Como foi desenvolvido](#como-foi-desenvolvido) ·
[Limitações conhecidas](#limitações-conhecidas)

---

## As três decisões principais

1. **O motor de decisão vive num projeto próprio (`CaixaProjeto.Core`), sem web nem base de
   dados.** As regras de negócio não dependem de nada, por isso são testadas em menos de um segundo
   (30 testes) e podem ser usadas por qualquer canal: as páginas, a API, ou outro sistema no futuro.
   [Detalhes](#arquitetura)
2. **Todas as regras são avaliadas e a decisão final é a mais restritiva.** Cada decisão tem um
   número de severidade (APROVADO 0, ANÁLISE MANUAL 1, RECUSADO 2, PEDIDO INVÁLIDO 3), e a Regra 8
   fica reduzida a escolher o maior. Como nenhuma regra interrompe as outras, o resultado mostra
   todos os motivos, e não só o primeiro. [Detalhes](#interpretação-das-regras)
3. **A decisão automática e o estado atual são guardados em separado, com um histórico de cada
   mudança.** A decisão do motor nunca é alterada; quando um analista aprova ou recusa um pedido em
   análise manual, muda só o estado atual e fica registado quem decidiu, quando e porquê. É isto
   que permite auditar as decisões e responder às consultas da Tarefa 4 (por exemplo, "quantos
   pedidos passaram de ANÁLISE MANUAL a APROVADO"). [Detalhes](#base-de-dados)

---

## Stack

**C# com .NET 10, Blazor, .NET Aspire, Entity Framework Core e SQLite.** O enunciado indica
preferência por .NET C#.

- **Blazor Web App** (modo Interactive Server) para as páginas: tudo em C#, sem JavaScript
  próprio. Os cliques são tratados no servidor através de uma ligação em tempo real (SignalR).
- **.NET Aspire** para arrancar a API e a Web com um só F5, ligá-las entre si e ter um dashboard
  com os logs de cada serviço.
- **SQLite** com **Entity Framework Core**: a base de dados é um ficheiro (`caixa.db`), não
  precisa de instalação e é criada sozinha no primeiro arranque.
- **xUnit** para os testes e **Bootstrap** (que vem com o modelo do Blazor) para o aspeto.

---

## Arquitetura

A solução foi criada a partir do modelo **Aspire Starter App**, que já traz a separação entre API
e Web. Acrescentei o `Core` e o `UnitTests`.

```
            AppHost  (arranca e liga tudo; não tem regras nem páginas)
           /       \
        Web  ──HTTP──>  ApiService  ──>  caixa.db (SQLite)
   (páginas Blazor)    (endpoints /api/pedidos)
                           │
                         Core  (motor de decisão, regras 1 a 8)
```

| Projeto | Responsabilidade |
|---|---|
| `Core` | Regras de negócio e os tipos partilhados. Não referencia nenhum outro projeto. |
| `ApiService` | Recebe pedidos HTTP, chama o motor e grava na base de dados. É o único que acede à `caixa.db`. |
| `Web` | As páginas. Pede tudo à API através do `CreditoApiClient`. |
| `AppHost` | Só para desenvolvimento: arranca a API e a Web e diz à Web onde está a API. |
| `ServiceDefaults` | Veio com o modelo e não foi alterado: logs, health checks, tentativas repetidas e service discovery. |
| `UnitTests` / `Tests` | Testes do motor / teste que arranca a aplicação inteira (este veio com o modelo). |

**Porquê projetos separados e não pastas.** Num projeto único (como no MVC tradicional) a
separação depende da disciplina de quem programa. Com projetos, é o compilador que garante as
fronteiras: a Web não referencia a API, por isso não consegue usar a base de dados diretamente, e
o Core não referencia nada, por isso as regras não podem depender de HTTP nem da base de dados.

**Porquê uma API à parte.** O enunciado pede "um componente que avalie os pedidos de crédito". Com
a API separada, esse componente pode servir outros canais (uma app, o balcão, outro sistema) com
as mesmas regras. A alternativa era um só projeto Blazor em que as páginas usavam a base de dados
diretamente: menos código, mas as regras ficavam presas a estas páginas.

**Porquê o endereço `https+http://apiservice`.** A Web não sabe a porta da API. O AppHost dá um
nome a cada serviço e o service discovery do Aspire traduz esse nome para o endereço real.

---

## Interpretação das regras

**Ordem de aplicação** (código em `CaixaProjeto.Core/MotorDecisao.cs`):

1. **Regra 1 primeiro.** Se o pedido for inválido, a análise pára aqui: com rendimento ou prazo a
   zero, os indicadores nem são calculáveis (haveria divisões por zero).
2. **Cálculo dos indicadores:** prestação estimada, taxa de esforço, idade no fim do contrato e
   montante máximo recomendado.
3. **Regras 2 a 7, todas**, pela ordem do enunciado. Cada uma devolve um motivo ou nada.
4. **Regra 8:** a decisão final é a mais restritiva dos motivos; sem motivos, o pedido é APROVADO.

**Como li cada regra** (os valores-limite estão todos provados nos testes):

| Regra | Interpretação |
|---|---|
| 1. Validação inicial | As cinco validações do enunciado, mais duas que também tornam o pedido impossível de analisar: prestações atuais negativas e situação profissional em falta. **Junta todos os erros de uma vez**, para o cliente os corrigir todos, em vez de os descobrir um a um. |
| 2. Idade no fim do contrato | Idade atual + prazo em anos (prazo ÷ 12). Exatamente 75 anos não dispara; 75,08 já vai a ANÁLISE MANUAL ("superior a 75"). |
| 3. Incidentes | Com incidentes registados: RECUSADO. |
| 4. Situação profissional | Efetivo prossegue, contrato a prazo vai a ANÁLISE MANUAL, desempregado é RECUSADO. |
| 5. Limite do montante | Limite = 20 × rendimento. Pedir exatamente o limite não dispara ("exceder" é estritamente maior). |
| 6. Taxa de esforço | Prestação nova = valor ÷ prazo, sem juros, como o enunciado manda simplificar. A prestação e a taxa são **arredondadas a 2 casas antes de serem comparadas com os limites**, para que o valor mostrado ao utilizador seja exatamente o que decidiu. 35,00% mantém; 35,01% a 50,00% vai a ANÁLISE MANUAL; acima de 50,00% é RECUSADO. |
| 7. Montantes elevados | Acima de 50.000 € (exatamente 50.000 não dispara). "Independentemente das restantes regras" foi lido como **um mínimo**: o pedido vai pelo menos a ANÁLISE MANUAL, mas se outra regra recusar, a recusa mantém-se, porque é mais restritiva (Regra 8). |
| 8. Prioridade | O enum `Decisao` tem os valores por ordem de severidade, e a decisão final é o máximo. |

**Os limites são configuráveis.** Os valores (18 anos, 75 anos, 20×, 35%, 50%, 50.000 €) estão na
secção `Regras` do `appsettings.json` da API, e não escritos no código. A área de risco pode
ajustá-los sem alterar o programa. Se a secção faltar, ficam os valores do enunciado.

**Uma classe por regra.** Cada regra (2 a 7) é uma classe que implementa a interface `IRegra`.
Para acrescentar uma regra nova, cria-se uma classe e junta-se à lista do motor; as outras não
mudam.

---

## Casos não previstos e perguntas ao negócio

Situações que o enunciado não resolve, com a decisão que tomei e a pergunta que faria ao analista
de negócio:

| Situação | O que decidi | Pergunta ao negócio |
|---|---|---|
| Prestações atuais negativas | Pedido inválido | Confirmam? |
| Situação profissional em falta | Pedido inválido | Há outras situações (reformado, independente...)? |
| NIF | Validar só os 9 dígitos, guardado como texto para não perder zeros à esquerda | Deve validar-se também o dígito de controlo? |
| Idade no fim do contrato | Idade + prazo ÷ 12, sem arredondar | A idade conta em anos completos ou com a fração do último ano? |
| Regra 7 contra uma recusa | A recusa prevalece | "Independentemente das restantes regras" quer dizer "no mínimo análise manual" ou "sempre análise manual, mesmo que outra regra recuse"? |
| Taxa de esforço sem juros | Como o enunciado manda | Em produção, que taxa de juro e que fórmula de prestação usar? |
| Simulações | Ficam registadas numa tabela própria, separadas dos pedidos (ver [Simulações](#simulações)) | Que dados das simulações interessa guardar, e durante quanto tempo? |

---

## Testes

**30 testes unitários do motor** (`CaixaProjeto.UnitTests/MotorDecisaoTests.cs`), sem web nem base
de dados. Cada teste parte de um pedido aprovado e muda só o campo que interessa, para ficar claro
que é essa mudança que provoca o resultado.

- **Os quatro cenários do enunciado:**

  | Cenário | Prestação | Taxa de esforço | Resultado | Motivos |
  |---|---|---|---|---|
  | A | 166,67 € | 14,67% | **APROVADO** | nenhum |
  | B | 416,67 € | 45,83% | **ANÁLISE MANUAL** | contrato a prazo (Regra 4); taxa entre 35% e 50% (Regra 6) |
  | C | 208,33 € | 16,94% | **RECUSADO** | incidentes de crédito (Regra 3) |
  | D | 694,44 € | 82,87% | **RECUSADO** | taxa acima de 50% (Regra 6); montante acima de 20× o rendimento (Regra 5) |

- **Regra 1:** NIF com 8 ou 10 dígitos, com letras, com espaços ou vazio; NIF com zero à esquerda
  é válido; 18 anos é válido e 17 não; valores a zero; os dois casos não previstos; e que os erros
  são todos juntos e as outras regras não chegam a correr.
- **Valores-limite** das regras 2, 5, 6 e 7 (exatamente no limite e logo acima).
- **Regra 8:** a Regra 7 não anula uma recusa; com várias regras a disparar, ficam todos os motivos
  e a decisão mais restritiva.
- **Parâmetros:** mudar um limite na configuração muda a decisão.

O teste de integração que veio com o modelo (`CaixaProjeto.Tests`) arranca a aplicação inteira
pelo Aspire e confirma que a página inicial responde. A lista, o detalhe e a decisão do analista
foram também testados à mão no browser, com uma base de dados de teste.

---

## Base de dados

**Modelo** (código em `CaixaProjeto.ApiService/Data/`):

```
Clientes         (Id, Nif UNIQUE, DataRegisto)
Pedidos          (Id, Numero UNIQUE, ClienteId → Clientes (NULL se o NIF for inválido),
                  Nif, Idade, RendimentoMensalLiquido, PrestacoesAtuais, ValorPretendido, PrazoMeses,
                  SituacaoProfissional, IncidentesCredito,
                  PrestacaoEstimada, TaxaEsforco, IdadeFinalContrato, LimiteMontante,
                  DecisaoAutomatica, EstadoAtual, DataSubmissao)
MotivosPedido    (Id, PedidoId → Pedidos, Regra, Descricao, Decisao)
HistoricoEstados (Id, PedidoId → Pedidos, EstadoAnterior, EstadoNovo, Data, Utilizador, Observacao)
Simulacoes       (Id, ClienteId → Clientes (NULL se o NIF for inválido),
                  Nif, Idade, RendimentoMensalLiquido, PrestacoesAtuais, ValorPretendido, PrazoMeses,
                  SituacaoProfissional, IncidentesCredito, Decisao, DataSimulacao)
```

- **`DecisaoAutomatica` nunca muda; `EstadoAtual` pode mudar.** Cada mudança de estado, incluindo
  a decisão automática, é uma linha em `HistoricoEstados`.
- **Os motivos têm tabela própria** (uma linha por regra que disparou), para se poder contar, por
  exemplo, o motivo de recusa mais frequente (Tarefa 4).
- **Os pedidos inválidos também são gravados**, para auditoria e reporting. Um NIF inválido não
  identifica ninguém, por isso esses pedidos ficam sem cliente.
- **Clientes identificados pelo NIF**, para saber quantos pedidos e simulações fez cada um
  (Tarefa 4). Um cliente pode ser criado por uma simulação e reaproveitado depois pelo pedido.
- **Os indicadores são gravados**, incluindo o montante máximo recomendado. Assim, o detalhe de um
  pedido mostra os valores que decidiram na altura, mesmo que os limites mudem depois na
  configuração. O motor não volta a correr quando se consulta um pedido.
- **Número do pedido legível:** ano + sequência de 4 dígitos (20260001, 20260002...), como no
  exemplo do enunciado.
- **Enums gravados como texto** ("AnaliseManual" em vez de 1), para as consultas SQL serem
  legíveis. **Decimais gravados como número (REAL)**, porque o SQLite não tem tipo decimal e assim
  as consultas podem somar e comparar valores diretamente.
- **Datas em UTC** na base de dados, convertidas para a hora local só no ecrã.
- **A submissão grava tudo numa só transação:** pedido, cliente, motivos e histórico, ou nada.
- **As tabelas são criadas no arranque** (`EnsureCreated`), sem migrações. É simples para esta
  fase, mas tem um custo: quando se acrescenta uma coluna, é preciso apagar a `caixa.db` para ser
  criada de novo. Aconteceu uma vez, ao acrescentar `LimiteMontante`.

---

## API

| Endpoint | O que faz |
|---|---|
| `POST /api/pedidos/preanalise` | Simula: aplica as regras, regista a simulação e devolve o resultado. Não cria pedido. |
| `POST /api/pedidos` | Analisa, grava e devolve o número atribuído e o resultado. |
| `GET /api/pedidos?estado=&pagina=&tamanho=` | Lista paginada, do mais recente para o mais antigo, com filtro opcional por estado atual. |
| `GET /api/pedidos/{numero}` | Detalhe de um pedido: dados, análise automática, estado atual e histórico. |
| `POST /api/pedidos/{numero}/decisao` | Decisão do analista (aprovar ou recusar). |

- **Enums em JSON como texto** ("AnaliseManual"), mais fáceis de ler do que números.
- **Os tipos que viajam entre a API e a Web estão no Core** (`Core/Contratos`), para os dois lados
  usarem exatamente as mesmas classes.
- **Erros no formato padrão "Problem Details"** (`{ "status": 409, "detail": "..." }`), com o
  código HTTP certo: 400 (dados em falta), 404 (o pedido não existe) e 409 (o pedido já não está em
  análise manual). A Web mostra ao utilizador a mensagem do campo `detail`.
- **O serviço não conhece códigos HTTP.** Diz só o que aconteceu (o enum `ResultadoDecisao`), e é o
  endpoint que escolhe a resposta HTTP. Assim a lógica pode ser usada fora da API.
- **Paginação com limites:** 20 por página por omissão, no máximo 100, para um pedido mal feito não
  obrigar a ler a tabela inteira.
- **Exemplos prontos a enviar** no ficheiro `CaixaProjeto.ApiService.http` (botão "Send request" do
  Visual Studio).

---

## Interface

- **Página Novo pedido** com dois botões: **Analisar** (simula; fica registada como simulação, mas
  não cria pedido) e **Submeter pedido** (cria o pedido e mostra o número, com link para o detalhe). Os quatro cenários do enunciado preenchem o
  formulário num clique.
- **O formulário não tem validações próprias.** Quem valida é o motor (Regra 1), por isso o ecrã
  mostra exatamente os mesmos motivos que a API devolveria a qualquer outro canal.
- **Página Pedidos:** tabela com 20 pedidos por página, botões Anterior/Seguinte e filtro por estado.
  O número de cada pedido abre o detalhe.
- **Página de detalhe:** dados do pedido, análise automática, estado atual e histórico. Nos pedidos
  em análise manual mostra o formulário do analista.
- **Cada decisão tem sempre a mesma cor** (verde aprovado, amarelo análise manual, vermelho recusado,
  cinzento inválido), e os valores seguem o formato português (1.234,56 €).
- **Interatividade só onde é precisa.** As páginas com botões usam `InteractiveServer`. A página de
  detalhe começou sem interatividade (só mostrava dados) e passou a tê-la quando ganhou os botões
  do analista.

---

## Simulações

A Tarefa 4 pede os "clientes que realizaram mais do que um pedido/simulação no último mês". Por
isso, desde o passo 10, cada clique em **Analisar** fica registado.

- **Numa tabela própria (`Simulacoes`), e não na tabela `Pedidos`.** Uma simulação não é um pedido:
  não tem número, não tem estado e não vai ao analista. Se ficassem juntas, com uma coluna a dizer o
  tipo, todas as listas e consultas de pedidos teriam de se lembrar de excluir as simulações, e
  bastava um esquecimento para os números saírem errados.
- **Guarda os dados de entrada e a decisão que o cliente viu**, com a data. Não guarda motivos nem
  indicadores: para estatística chega, e se for preciso, a simulação pode ser analisada de novo a
  partir dos dados.
- **As simulações inválidas também são gravadas**, como os pedidos inválidos. Com NIF inválido
  ficam sem cliente.
- **A página e o contrato da API não mudaram:** o endpoint de simulação continua a devolver o mesmo
  resultado. A única diferença para o utilizador é o texto "o pedido ainda não foi submetido".

---

## Decisão do analista

- **Só os pedidos em ANÁLISE MANUAL podem ser decididos**, e passam a APROVADO ou RECUSADO. Um
  pedido já decidido não volta a ser decidido (a API responde 409).
- **O nome do analista e a observação são obrigatórios.** São o que justifica a decisão numa
  auditoria. O ecrã valida logo, para dar uma resposta imediata, e a API volta a validar, porque é
  ela que tem a última palavra.
- **A decisão automática fica como estava.** Muda o estado atual e acrescenta-se uma linha ao
  histórico ("ANÁLISE MANUAL → APROVADO", analista, data, observação), as duas coisas na mesma
  transação.

---

## Consultas da Tarefa 4

As cinco consultas estão em [sql/Tarefa4_Consultas.sql](sql/Tarefa4_Consultas.sql), escritas para
SQLite e comentadas uma a uma.

**Porquê SQL escrito à mão, se a aplicação não tem nenhum.** A aplicação acede à base de dados só
através do Entity Framework Core, um ORM: o código é C# (LINQ), verificado pelo compilador, e é o
EF Core que gera o SQL (pode ver-se nos logs da API, no dashboard do Aspire). As consultas da
Tarefa 4 são outra coisa: o enunciado pede-as explicitamente, e simulam o trabalho de alguém de
reporting ou auditoria que abre a base de dados numa ferramenta e corre consultas, sem passar pela
aplicação. Por isso ficam num ficheiro à parte, fora do código da aplicação.

| | Código da aplicação | `sql/Tarefa4_Consultas.sql` |
|---|---|---|
| Quem usa | A própria aplicação | Uma pessoa, numa ferramenta de base de dados |
| Como acede | EF Core (LINQ), que gera o SQL | SQL escrito à mão |
| Se uma coluna mudar de nome | Deixa de compilar | Só falha quando alguém a corre |

Um exemplo, a consulta 1 nos dois estilos:

```sql
SELECT COUNT(*) FROM Pedidos WHERE EstadoAtual = 'Aprovado';
```

```csharp
var aprovados = await db.Pedidos.CountAsync(p => p.EstadoAtual == Decisao.Aprovado);
```

Decisões sobre cada consulta:

- **"Terminado com o estado X" é o estado atual do pedido** (`EstadoAtual`), e não a decisão do
  motor. Um pedido que o motor mandou para análise manual e o analista aprovou conta como aprovado.
- **Os pedidos ainda em análise manual aparecem na consulta 2** como estado próprio: são os que
  esperam pelo analista.
- **A consulta 2 mostra sempre os três estados**, mesmo os que têm 0 pedidos. Um `GROUP BY` sozinho
  esconderia os estados sem pedidos, e "0 inválidos" também é uma resposta.
- **Motivo de recusa mais frequente (consulta 3):** contam só os motivos que pediram a recusa, e
  não os outros motivos dos mesmos pedidos. **Em caso de empate, aparecem todos os motivos
  empatados**, em vez de um escolhido ao acaso. Os pedidos recusados por um analista não têm motivo
  automático de recusa; a justificação deles está na observação do histórico.
- **Pedidos e simulações contam juntos (consulta 4).** A consulta junta as duas tabelas numa só
  lista com `UNION ALL` e conta por cliente: 2 pedidos, 2 simulações, ou 1 pedido e 1 simulação
  contam todos como "mais do que um". O resultado mostra também quantos são de cada tipo.
  **"Último mês"** é desde o mesmo dia do mês anterior até agora, em UTC como as datas gravadas. Só
  entram registos com NIF válido, porque só esses estão ligados a um cliente.
- **Análise manual para aprovado (consulta 5)** lê o histórico de estados, que é o registo de
  auditoria. O ficheiro inclui também a versão que só usa a tabela `Pedidos`, que hoje dá o mesmo
  resultado porque um pedido só pode ser decidido uma vez.
- **Testadas com dados preparados para cada caso:** pedidos aprovados pelo motor e pelo analista,
  recusas por regras diferentes, um empate de motivos, simulações, e pedidos e simulações com datas
  antigas (que a consulta 4 tem de deixar de fora).

---

## Estilo do código

- **As regras estão escritas na forma longa (if / else)**, de propósito, para serem fáceis de ler
  e comparar com o enunciado, mesmo por quem não programa em C# todos os dias.
- **Nomes e comentários em português**, na linguagem do enunciado (Pedido, Regra, TaxaEsforco).
- **Uma responsabilidade por classe:** uma classe por regra, um serviço para juntar motor e base de
  dados, um cliente HTTP na Web.
- **`docs/Estrutura.md`** explica cada ficheiro e é atualizado a cada passo.

---

## Como foi desenvolvido

Um passo de cada vez, cada um testado antes de passar ao seguinte. Os passos 0 a 4 foram feitos
num repositório de trabalho e chegaram a este repositório no commit "Primeira Versão".

| Passo | O que ficou feito |
|---|---|
| 0 | Solução criada a partir do modelo Aspire, sem os exemplos do modelo. |
| 1 | Domínio no Core: pedido, decisões, resultado da análise e parâmetros das regras. |
| 2 | Motor de decisão: validação inicial, indicadores, uma classe por regra (2 a 7) e a Regra 8. |
| – | Primeira página (Novo pedido) e endpoint de simulação; link "Abrir a aplicação" no dashboard do Aspire. |
| 3 | Testes unitários do motor: cenários A a D, Regra 1, valores-limite e prioridades. |
| 4 | Base de dados SQLite: tabelas, gravação dos pedidos e botão Submeter pedido. |
| – | Regras reescritas na forma longa (if / else), sem mudar o comportamento. |
| 5 | Página com a lista de pedidos, paginada e com filtro por estado. |
| 6 | Detalhe do pedido: dados, análise e histórico. |
| 7 | Decisão do analista sobre os pedidos em análise manual. |
| 8 | Este documento (DECISIONS.md). |
| 9 | Consultas SQL da Tarefa 4. |
| 10 | Registo das simulações, para a consulta 4 contar pedidos e simulações. |

---

## Limitações conhecidas

- **Sem autenticação.** O analista escreve o seu nome à mão, e qualquer pessoa com acesso à
  aplicação pode decidir pedidos. Em produção, o nome viria do login.
- **Dois analistas ao mesmo tempo:** se ambos abrirem o mesmo pedido e decidirem quase em
  simultâneo, as duas decisões podem ficar gravadas e vale a última. Em produção, a gravação teria
  de confirmar que o estado não mudou entretanto.
- **Sem migrações de base de dados:** mudar o modelo obriga a recriar a `caixa.db`.
- **SQLite:** chega bem para uma aplicação local, mas não foi pensado para muitos utilizadores a
  gravar ao mesmo tempo.
- **Só o motor tem testes automáticos.** A API e a gravação na base de dados foram testadas à mão.
- **Por fazer:** as respostas escritas das Tarefas 5 e 6.
