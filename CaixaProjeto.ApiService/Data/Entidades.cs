using CaixaProjeto.Core.Dominio;

namespace CaixaProjeto.ApiService.Data;

// Tabelas da base de dados. São classes simples com "set", porque o EF Core precisa de as preencher.
// Ficam na API (e não no Core) porque são um detalhe de gravação, não regras de negócio.

/// <summary>Um cliente, identificado pelo NIF. Permite saber quantos pedidos fez cada cliente (Tarefa 4).</summary>
public class Cliente
{
    public int Id { get; set; }
    public string Nif { get; set; } = "";
    public DateTime DataRegisto { get; set; }

    public List<Pedido> Pedidos { get; set; } = [];
}

/// <summary>
/// Um pedido submetido: os dados de entrada, os indicadores e a decisão.
/// DecisaoAutomatica é a do motor e nunca muda; EstadoAtual pode evoluir
/// (por exemplo, um analista aprova um pedido que estava em análise manual).
/// </summary>
public class Pedido
{
    public int Id { get; set; }
    public string Numero { get; set; } = "";          // ex.: 20260001 (ano + sequência)

    // Pedidos inválidos também são gravados (para auditoria); podem não ter cliente se o NIF for inválido
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    // Dados de entrada
    public string? Nif { get; set; }
    public int Idade { get; set; }
    public decimal RendimentoMensalLiquido { get; set; }
    public decimal PrestacoesAtuais { get; set; }
    public decimal ValorPretendido { get; set; }
    public int PrazoMeses { get; set; }
    public SituacaoProfissional? SituacaoProfissional { get; set; }
    public bool IncidentesCredito { get; set; }

    // Indicadores (null quando o pedido é inválido)
    public decimal? PrestacaoEstimada { get; set; }
    public decimal? TaxaEsforco { get; set; }
    public decimal? IdadeFinalContrato { get; set; }
    public decimal? LimiteMontante { get; set; }        // Regra 5: gravado para o detalhe mostrar o limite que decidiu

    // Decisão
    public Decisao DecisaoAutomatica { get; set; }
    public Decisao EstadoAtual { get; set; }
    public DateTime DataSubmissao { get; set; }

    public List<MotivoPedido> Motivos { get; set; } = [];
    public List<HistoricoEstado> Historico { get; set; } = [];
}

/// <summary>Um motivo da decisão automática (uma linha por regra que disparou).</summary>
public class MotivoPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int Regra { get; set; }
    public string Descricao { get; set; } = "";
    public Decisao Decisao { get; set; }
}

/// <summary>
/// Cada mudança de estado de um pedido: a decisão automática e, mais tarde, a do analista.
/// É o que permite responder "quantos pedidos passaram de ANÁLISE MANUAL a APROVADO" (Tarefa 4).
/// </summary>
public class HistoricoEstado
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public Decisao? EstadoAnterior { get; set; }
    public Decisao EstadoNovo { get; set; }
    public DateTime Data { get; set; }
    public string Utilizador { get; set; } = "";
    public string? Observacao { get; set; }
}
