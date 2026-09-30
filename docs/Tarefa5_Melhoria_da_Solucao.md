# Tarefa 5 - Melhoria da Solução

> Imagine que esta aplicação vai evoluir para utilização em produção. Descreva: melhorias técnicas
> que implementaria; testes adicionais que realizaria; informações que guardaria para auditoria e
> reporting.

A aplicação atual foi feita para o exercício: corre numa máquina, com uma base de dados SQLite, sem
login. As respostas abaixo partem do que ela já tem (ver [DECISIONS.md](../DECISIONS.md)) e dizem o
que mudaria para produção, por ordem de prioridade dentro de cada secção.

---

## 1. Melhorias técnicas

### Segurança e dados pessoais

- **Autenticação e perfis.** Hoje não há login e o analista escreve o nome à mão. Em produção, o
  login seria o da organização (single sign-on) e haveria perfis: quem submete pedidos, o analista
  (que decide os pedidos em análise manual) e o auditor (que só consulta). O nome no histórico
  passaria a vir do login, e não de uma caixa de texto.
- **Segregação de funções.** Quem submete um pedido não o pode decidir como analista.
- **Proteção do NIF (RGPD).** O NIF é um dado pessoal: cifrado na base de dados, mostrado
  parcialmente nas listas (\*\*\*\*\*6789) e nunca escrito nos logs.
- **Prazos de retenção.** Definir com o jurídico durante quanto tempo se guardam pedidos e,
  sobretudo, simulações, e anonimizar ou apagar depois disso.
- **Segredos fora do código** (num cofre de segredos, como o Azure Key Vault), HTTPS obrigatório e
  limite de pedidos por minuto na API, para travar abusos.

### Base de dados

- **SQL Server (ou PostgreSQL) em vez de SQLite.** O SQLite não foi pensado para muitos utilizadores
  a gravar ao mesmo tempo, nem para backups e alta disponibilidade.
- **Migrações do Entity Framework em vez do `EnsureCreated`.** Acrescentar uma coluna obrigava a
  apagar a base de dados (durante o desenvolvimento, os passos 6 e 10 obrigaram a isso). *Já feito no
  passo 15:* cada alteração é aplicada à base de dados existente, sem perder dados, e fica versionada
  no git. Para produção faltaria aplicar as migrações num passo próprio da entrega (um script SQL
  revisto ou um "migration bundle"), e não no arranque da API, por causa de várias cópias da API a
  arrancar ao mesmo tempo e de migrações demoradas.
- **Backups automáticos** e um teste periódico de reposição.
- **Um índice em `EstadoAtual`**, porque a lista e as consultas filtram por esse campo.

### Correção em concorrência

Dois problemas que só aparecem com vários utilizadores ao mesmo tempo:

- **Número do pedido.** O `ProximoNumeroAsync` lê o último número e soma 1. Se dois pedidos forem
  submetidos no mesmo instante, os dois podem calcular o mesmo número; o índice único impede o
  duplicado, mas um dos clientes recebe um erro. Em produção, o número viria de uma sequência da
  base de dados, que nunca dá o mesmo valor duas vezes.
- **Dois analistas no mesmo pedido.** Se os dois decidirem quase ao mesmo tempo, as duas decisões
  podem ficar gravadas. A solução é a concorrência otimista: a gravação confirma que o pedido não
  mudou desde que o analista o abriu (uma coluna de versão) e, se mudou, pede-lhe para recarregar.
- **Pedidos repetidos.** O botão "Submeter pedido" fica desativado enquanto o pedido é enviado, mas
  um reenvio (a Web tenta de novo quando uma chamada à API falha, e uma falha pode acontecer depois
  de o pedido já estar gravado) pode criar dois pedidos iguais. Uma chave de idempotência enviada
  pela página faria a API reconhecer o segundo envio como repetido.

### Regras de negócio

- **Guardar a versão das regras com cada decisão.** Os limites estão no `appsettings.json` e podem
  mudar. Para uma auditoria saber com que limites um pedido foi decidido, cada pedido guardaria a
  versão dos parâmetros em vigor.
- **Parâmetros com data de início**, geridos pela área de risco numa página própria, com o registo de
  quem mudou o quê e quando, em vez de editar um ficheiro de configuração.
- **Prestação com juros.** Hoje a prestação é valor ÷ prazo, como o enunciado manda simplificar. Em
  produção usaria a fórmula de amortização com a taxa de juro do produto.
- **Validar o dígito de controlo do NIF**, e não só os 9 dígitos.

### Operação

- **Logs, métricas e alertas.** A aplicação já envia telemetria (OpenTelemetry) para o dashboard do
  Aspire. Em produção iria para uma ferramenta de monitorização (como o Application Insights), com
  alertas: API em baixo, muitos erros num curto espaço de tempo, respostas lentas.
- **Health checks em produção.** Os endereços `/health` e `/alive` só existem em desenvolvimento
  (é assim que vêm no modelo, por segurança). Em produção seriam ligados à monitorização, sem
  expor pormenores internos.
- **Mostrar ao utilizador um código de erro** e **mensagens de erro mais precisas.** Qualquer falha
  ao chamar a API mostrava "Não foi possível contactar a API", mesmo quando a API tinha respondido
  com um erro interno. *Já feito no passo 14:* as duas situações têm mensagens diferentes e, quando a
  API responde com um erro, a página mostra o código (o `traceId`) para o suporte encontrar o erro
  nos logs.
- **Pipeline de entrega contínua** (por exemplo, GitHub Actions): compilar, correr os 92 testes e
  publicar em ambientes separados (desenvolvimento, testes, produção), com aprovação antes de
  produção.
- **Versionar a API** (`/api/v1/...`) e publicar a documentação OpenAPI, que hoje só existe em
  desenvolvimento, para outros canais a poderem usar.

---

## 2. Testes adicionais

A aplicação tem 92 testes automáticos: 37 das regras, 44 do serviço, da API, das consultas SQL e das migrações, 10
do cliente HTTP da Web e 1 que arranca a aplicação inteira. Acrescentaria:

| Teste | Para quê |
|---|---|
| **Testes das páginas** (bUnit) e **no browser** (Playwright) | As páginas só foram testadas à mão. Um teste no browser faria o percurso completo: preencher, submeter, abrir o detalhe, decidir como analista. |
| **Concorrência** | Muitas submissões ao mesmo tempo (números duplicados) e dois analistas no mesmo pedido. |
| **Carga e desempenho** (por exemplo, k6) | Quantos pedidos por segundo a aplicação aguenta e a partir de quando fica lenta. |
| **Segurança** (OWASP ZAP, testes de intrusão) | Injeção, acesso sem permissão, perfis que veem o que não devem. |
| **Com a base de dados de produção** (Testcontainers com SQL Server) | Os testes atuais usam SQLite; em produção o motor de base de dados seria outro. |
| **Migrações** | Aplicar as migrações a uma cópia da base de dados de produção antes de cada entrega. |
| **Aceitação com o negócio** | Os valores-limite (exatamente 35%, exatamente 75 anos...) validados pela área de risco, e não só pela minha interpretação. |
| **Propriedades do motor** | Gerar milhares de pedidos ao acaso e confirmar regras gerais: a decisão final nunca é menos restritiva do que qualquer motivo; um pedido inválido nunca tem indicadores. |
| **Qualidade dos testes** (testes de mutação, com o Stryker) | Alterar o código de propósito (por exemplo, trocar `>` por `>=`) e confirmar que algum teste falha. |
| **Acessibilidade** | Navegação só com teclado, leitores de ecrã e contraste das cores das decisões. |

---

## 3. Informações para auditoria e reporting

### O que já é guardado

- Os **dados de entrada** de cada pedido e de cada simulação.
- Os **indicadores** calculados e **cada motivo** da decisão (regra, descrição, decisão pedida).
- A **decisão automática**, que nunca muda, e o **estado atual**.
- O **histórico de estados**: cada mudança, com o estado anterior, o novo, a data, quem decidiu e a
  observação.
- Os **pedidos inválidos** e as **simulações**, e não só os pedidos que avançaram.

### O que acrescentaria

| Informação | Porquê |
|---|---|
| **Versão das regras e da aplicação** em cada decisão | Saber com que limites e com que versão do código cada pedido foi decidido. |
| **Utilizador autenticado e canal** (web, balcão, app) | Hoje o nome do analista é escrito à mão e não se sabe por onde entrou o pedido. |
| **Quem consultou cada pedido** | Com dados pessoais, o RGPD exige saber quem acedeu a quê. |
| **Alterações aos parâmetros das regras** | Quem mudou um limite, de que valor para que valor, e quando. |
| **Ligação entre a simulação e o pedido** | Saber quantas simulações se tornam pedidos (taxa de conversão). |
| **Erros e tentativas falhadas**, com o `traceId` | Encontrar e explicar falhas, como a da Tarefa 6. |
| **Tempo de resposta** das análises | Acompanhar o desempenho. |

Tudo isto em registos **só de acrescentar**: o histórico não se altera nem se apaga, só se
acrescentam linhas.

### Relatórios que estes dados permitem

- Taxa de aprovação, recusa e análise manual por mês e por canal.
- As regras que mais recusam e as que mais enviam para análise manual (a consulta 3 da Tarefa 4 é o
  começo).
- Tempo médio entre a submissão e a decisão do analista, que sai das datas do histórico.
- Percentagem de pedidos em análise manual que acabam aprovados (a consulta 5 da Tarefa 4).
- Distribuição das taxas de esforço e dos montantes pedidos, para a área de risco afinar os limites.
- Conversão de simulações em pedidos.
