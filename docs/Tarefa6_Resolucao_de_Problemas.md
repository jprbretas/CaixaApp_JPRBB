# Tarefa 6 - Resolução de Problemas

> **Assunto:** Não consigo pedir o crédito
>
> "Boa tarde, estou a tentar fazer um pedido de crédito na aplicação e não consigo avançar. Dá erro.
> Precisava que resolvessem rapidamente. Obrigado."
>
> Descreva: que informação é conhecida e que informação está em falta; como obter a informação
> necessária; que hipóteses de erro identificaria; que equipas envolver.

As hipóteses abaixo são concretas para esta aplicação: algumas vêm de situações encontradas
durante o desenvolvimento.

---

## 1. Informação conhecida e informação em falta

**Conhecida:**

- Um cliente tentou fazer um pedido de crédito na aplicação e não conseguiu avançar.
- Viu algo que descreve como "erro".
- É urgente para ele.
- A data e a hora aproximadas, pela hora a que o e-mail chegou.

**Em falta:**

| Informação | Porque é importante |
|---|---|
| **Quem é** (nome e contacto; o NIF só por um canal seguro) | Para encontrar os pedidos e as simulações dele na base de dados. O NIF não deve circular por e-mail. |
| **Quando aconteceu**, com a hora o mais exata possível | Para procurar nos logs à volta dessa hora. |
| **Em que passo parou**: ao abrir a página, ao carregar em "Analisar" ou em "Submeter pedido" | Cada passo usa partes diferentes da aplicação. |
| **A mensagem exata ou uma captura de ecrã** | "Dá erro" pode ser uma mensagem técnica, uma página que não responde ou um resultado do tipo "PEDIDO INVÁLIDO". |
| **Por onde acedeu**: browser, telemóvel ou computador, rede de casa ou do trabalho | Alguns problemas só acontecem num browser ou numa rede. |
| **Se acontece sempre** ou só aconteceu uma vez | Separa uma falha momentânea de um problema persistente. |
| **Se tentou outra vez** e se já tinha conseguido antes | Ajuda a saber se o problema é novo (por exemplo, depois de uma atualização). |
| **Se há outros clientes com o mesmo problema** | Um cliente ou todos muda completamente a prioridade. |

---

## 2. Como obter a informação

1. **Responder ao cliente logo**, a confirmar que o pedido foi recebido e com poucas perguntas
   simples: quando, em que botão, que mensagem viu (de preferência uma captura de ecrã) e um
   contacto. O NIF é pedido por telefone ou pelo canal seguro, não por e-mail.
2. **Ver se o problema é geral**: o estado da aplicação na monitorização (os health checks, que
   a aplicação tem em desenvolvimento e que em produção seriam ligados à monitorização), a taxa de
   erros nos logs e se chegaram outros reportes parecidos ao suporte.
3. **Procurar nos logs à volta da hora indicada.** A aplicação já regista cada pedido HTTP e os
   erros (OpenTelemetry). Os erros da API trazem um `traceId` que liga a página, a API e a base de
   dados do mesmo pedido.
4. **Procurar na base de dados pelo NIF e pela hora**: se há uma simulação ou um pedido gravado,
   com que decisão e com que motivos. Isto responde logo a uma pergunta importante: o pedido chegou
   a ser analisado?
5. **Ver o que mudou**: se houve uma atualização da aplicação, da configuração ou da base de dados
   perto dessa hora.
6. **Reproduzir num ambiente de testes** com os mesmos dados que o cliente usou.

---

## 3. Hipóteses de erro

Da mais provável para a menos provável, e como confirmar cada uma:

| # | Hipótese | Como confirmar |
|---|---|---|
| 1 | **Não é um erro técnico: o pedido deu PEDIDO INVÁLIDO ou RECUSADO**, e o cliente leu o resultado como "erro". Um caso concreto desta aplicação: um NIF escrito com espaços ("123 456 789") ou com traços dá PEDIDO INVÁLIDO, porque a validação exige exatamente 9 dígitos. O mesmo com a situação profissional por escolher. | Na base de dados: uma simulação ou um pedido com o NIF e a hora do cliente, com decisão `PedidoInvalido` ou `Recusado`, e os motivos. |
| 2 | **A API está em baixo ou inacessível.** A página mostra "Não foi possível contactar a API". | Health checks e logs; outros clientes afetados à mesma hora. |
| 3 | **A base de dados não corresponde ao código**, depois de uma atualização. Esteve para acontecer durante o desenvolvimento: depois de se acrescentar uma coluna, a base de dados antiga teria dado "no such column" ao submeter. | Logs da API com o erro da base de dados; comparar a versão entregue com a estrutura da base de dados. |
| 4 | **A API respondeu com um erro interno** (por exemplo, a base de dados ocupada ou sem espaço). A página mostra a mesma mensagem da hipótese 2, "Não foi possível contactar a API", o que confunde o diagnóstico. | Logs da API com respostas 500 à hora indicada. |
| 5 | **Dois pedidos submetidos no mesmo instante** calcularam o mesmo número de pedido, e o índice único rejeitou um deles. | Logs com violação do índice único em `Numero`; outro pedido gravado no mesmo segundo. |
| 6 | **A ligação em tempo real da página perdeu-se.** As páginas usam Blazor Server, que mantém uma ligação aberta com o servidor. Depois de muito tempo parado, de uma troca de rede ou num telemóvel, a página deixa de responder aos botões ("não consigo avançar"). | Logs de ligações SignalR perdidas; pedir ao cliente para recarregar a página. |
| 7 | **A rede do cliente bloqueia a ligação em tempo real** (proxies de empresas bloqueiam WebSockets) ou o browser é antigo ou tem o JavaScript bloqueado. | Perguntar o browser e a rede; tentar com outro browser ou rede. |
| 8 | **Um erro introduzido numa atualização recente.** | Datas das entregas; reproduzir com a versão anterior. |
| 9 | **Um problema de infraestrutura**: certificado HTTPS expirado, DNS, servidor sem memória ou sem disco. | Monitorização da infraestrutura; o browser avisa se o certificado expirou. |

A hipótese 1 é a primeira a confirmar porque é a mais rápida (uma consulta à base de dados) e,
nesta aplicação, bastante provável. Se se confirmar, não há nada a corrigir no código, mas há uma
melhoria a fazer: a página devia aceitar o NIF com espaços, ou explicar melhor o problema. As
hipóteses 2, 4 e 6 mostram também que a mensagem "Não foi possível contactar a API" é demasiado
genérica (ver as melhorias na [Tarefa 5](Tarefa5_Melhoria_da_Solucao.md)).

---

## 4. Equipas a envolver

| Equipa | Papel |
|---|---|
| **Suporte / service desk** (1.ª linha) | Responde ao cliente, recolhe a informação em falta, regista o incidente e mantém o cliente informado. |
| **Desenvolvimento** | Analisa os logs e a base de dados, reproduz o erro, corrige e acrescenta um teste para ele não voltar. |
| **Operações / infraestrutura** | Servidores, rede, certificados, base de dados, entregas recentes. |
| **Área de negócio (crédito / risco)** | Se o "erro" for uma recusa ou um pedido inválido: explicar ao cliente o motivo e decidir se a regra ou a mensagem devem mudar. |
| **Segurança / proteção de dados** | Se houver dados pessoais envolvidos (por exemplo, o cliente enviou o NIF por e-mail) ou suspeita de abuso. |
| **Testes / QA** | Confirma a correção antes de chegar a produção. |

**Depois de resolvido:** responder ao cliente com a explicação e o que ele deve fazer; se foi um
erro da aplicação, uma análise curta do que aconteceu, porque não foi apanhado antes e o que muda
para não se repetir.
