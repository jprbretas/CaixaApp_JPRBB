-- =====================================================================================
-- Tarefa 4 - Extrações / queries à base de dados
--
-- Base de dados: CaixaProjeto.ApiService/caixa.db (SQLite).
-- Os estados estão gravados como texto: 'Aprovado', 'AnaliseManual', 'Recusado', 'PedidoInvalido'.
-- As datas estão gravadas em UTC, no formato 'AAAA-MM-DD HH:MM:SS.fffffff'.
--
-- Pedidos.DecisaoAutomatica = a decisão do motor, nunca muda.
-- Pedidos.EstadoAtual       = o estado em que o pedido está agora (muda se um analista decidir).
-- HistoricoEstados          = uma linha por cada mudança de estado.
-- Simulacoes                = uma linha por cada simulação (botão "Analisar"); não são pedidos.
--
-- "Terminado com o estado X" é lido como o estado atual do pedido (Pedidos.EstadoAtual).
-- =====================================================================================


-- -------------------------------------------------------------------------------------
-- 1. Número de pedidos terminados com o estado APROVADO
--    Conta os aprovados pelo motor e os aprovados por um analista.
-- -------------------------------------------------------------------------------------
SELECT COUNT(*) AS PedidosAprovados
FROM Pedidos
WHERE EstadoAtual = 'Aprovado';


-- -------------------------------------------------------------------------------------
-- 2. Número de pedidos terminados com cada um dos restantes estados
--    A lista "Estados" garante que os três estados aparecem sempre, mesmo com 0 pedidos
--    (um GROUP BY sozinho esconderia os estados sem nenhum pedido).
--    Os pedidos em 'AnaliseManual' são os que ainda esperam pela decisão de um analista.
-- -------------------------------------------------------------------------------------
WITH Estados (Estado) AS (
    VALUES ('Recusado'), ('AnaliseManual'), ('PedidoInvalido')
)
SELECT e.Estado,
       COUNT(p.Id) AS Pedidos
FROM Estados e
LEFT JOIN Pedidos p ON p.EstadoAtual = e.Estado
GROUP BY e.Estado
ORDER BY Pedidos DESC;


-- -------------------------------------------------------------------------------------
-- 3. No caso de RECUSA, o motivo mais frequente
--    Conta os motivos que pediram a recusa (Decisao = 'Recusado') nos pedidos recusados.
--    Se houver empate, mostra todos os motivos empatados (em vez de escolher um ao acaso).
--    Nota: um pedido recusado por um analista não tem motivo automático de recusa;
--    a justificação dele está em HistoricoEstados.Observacao.
-- -------------------------------------------------------------------------------------
WITH Contagem AS (
    SELECT m.Regra,
           m.Descricao,
           COUNT(*) AS Ocorrencias
    FROM MotivosPedido m
    JOIN Pedidos p ON p.Id = m.PedidoId
    WHERE p.EstadoAtual = 'Recusado'
      AND m.Decisao = 'Recusado'
    GROUP BY m.Regra, m.Descricao
)
SELECT Regra, Descricao, Ocorrencias
FROM Contagem
WHERE Ocorrencias = (SELECT MAX(Ocorrencias) FROM Contagem);


-- -------------------------------------------------------------------------------------
-- 4. Clientes que realizaram mais do que um pedido/simulação no último mês
--    Junta pedidos e simulações numa só lista ("Atividade") com UNION ALL e conta por cliente:
--    2 pedidos, 2 simulações, ou 1 pedido + 1 simulação, todos contam como "mais do que um".
--    "Último mês" = desde o mesmo dia do mês anterior até agora (em UTC, como as datas).
--    Só entram registos com NIF válido, porque só esses estão ligados a um cliente.
-- -------------------------------------------------------------------------------------
WITH Atividade AS (
    SELECT ClienteId, DataSubmissao AS Data, 'Pedido' AS Tipo
    FROM Pedidos
    UNION ALL
    SELECT ClienteId, DataSimulacao AS Data, 'Simulacao' AS Tipo
    FROM Simulacoes
)
SELECT c.Nif,
       COUNT(*)                                              AS Total,
       SUM(CASE WHEN a.Tipo = 'Pedido' THEN 1 ELSE 0 END)    AS Pedidos,
       SUM(CASE WHEN a.Tipo = 'Simulacao' THEN 1 ELSE 0 END) AS Simulacoes,
       MIN(a.Data)                                           AS Primeiro,
       MAX(a.Data)                                           AS Ultimo
FROM Atividade a
JOIN Clientes c ON c.Id = a.ClienteId
WHERE a.Data >= datetime('now', '-1 month')
GROUP BY c.Id, c.Nif
HAVING COUNT(*) > 1
ORDER BY Total DESC, c.Nif;


-- -------------------------------------------------------------------------------------
-- 5. Número de pedidos que terminaram com ANÁLISE MANUAL e evoluíram para APROVADO
--    Lê o histórico: conta os pedidos com uma mudança de 'AnaliseManual' para 'Aprovado'.
--    DISTINCT para contar cada pedido uma só vez.
-- -------------------------------------------------------------------------------------
SELECT COUNT(DISTINCT h.PedidoId) AS ManualParaAprovado
FROM HistoricoEstados h
WHERE h.EstadoAnterior = 'AnaliseManual'
  AND h.EstadoNovo = 'Aprovado';

-- Alternativa sem o histórico (dá o mesmo resultado, porque hoje um pedido só pode ser
-- decidido uma vez): decisão do motor = análise manual e estado atual = aprovado.
-- SELECT COUNT(*) FROM Pedidos
-- WHERE DecisaoAutomatica = 'AnaliseManual' AND EstadoAtual = 'Aprovado';
