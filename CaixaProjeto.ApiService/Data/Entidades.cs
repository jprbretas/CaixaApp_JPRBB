using CaixaProjeto.Core.Dominio;

namespace CaixaProjeto.ApiService.Data;

// As tabelas da base de dados. Ficam na API, e não no Core, porque são um detalhe de gravação.

public class Cliente
{
    public int Id { get; set; }
    public string Nif { get; set; } = "";
    public DateTime DataRegisto { get; set; }

    public List<Pedido> Pedidos { get; set; } = [];
    public List<Simulacao> Simulacoes { get; set; } = [];
}

/// <summary>A DecisaoAutomatica (do motor) nunca muda; o EstadoAtual muda quando um analista decide.</summary>
public class Pedido
{
    public int Id { get; set; }
    public string Numero { get; set; } = "";          // ex.: 20260001 (ano + sequência)

    // Os pedidos inválidos também são gravados (auditoria); sem NIF válido, não há cliente
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public string? Nif { get; set; }
    public int Idade { get; set; }
    public decimal RendimentoMensalLiquido { get; set; }
    public decimal PrestacoesAtuais { get; set; }
    public decimal ValorPretendido { get; set; }
    public int PrazoMeses { get; set; }
    public SituacaoProfissional? SituacaoProfissional { get; set; }
    public bool IncidentesCredito { get; set; }

    // Indicadores: null quando o pedido é inválido
    public decimal? PrestacaoEstimada { get; set; }
    public decimal? TaxaEsforco { get; set; }
    public decimal? IdadeFinalContrato { get; set; }
    public decimal? LimiteMontante { get; set; }        // gravado para o detalhe mostrar o limite que decidiu

    public Decisao DecisaoAutomatica { get; set; }
    public Decisao EstadoAtual { get; set; }
    public DateTime DataSubmissao { get; set; }

    public List<MotivoPedido> Motivos { get; set; } = [];
    public List<HistoricoEstado> Historico { get; set; } = [];
}

/// <summary>
/// Um clique em "Analisar". Tabela própria porque não é um pedido: não tem número, estado nem
/// histórico. Serve para a Tarefa 4 ("pedido/simulação").
/// </summary>
public class Simulacao
{
    public int Id { get; set; }

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public string? Nif { get; set; }
    public int Idade { get; set; }
    public decimal RendimentoMensalLiquido { get; set; }
    public decimal PrestacoesAtuais { get; set; }
    public decimal ValorPretendido { get; set; }
    public int PrazoMeses { get; set; }
    public SituacaoProfissional? SituacaoProfissional { get; set; }
    public bool IncidentesCredito { get; set; }

    public Decisao Decisao { get; set; }
    public DateTime DataSimulacao { get; set; }
}

public class MotivoPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int Regra { get; set; }
    public string Descricao { get; set; } = "";
    public Decisao Decisao { get; set; }
}

/// <summary>
/// Cada mudança de estado. É daqui que sai "quantos pedidos passaram de ANÁLISE MANUAL a
/// APROVADO" (Tarefa 4).
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
