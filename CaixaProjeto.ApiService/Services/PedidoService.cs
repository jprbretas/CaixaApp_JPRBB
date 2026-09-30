using CaixaProjeto.ApiService.Data;
using CaixaProjeto.Core;
using CaixaProjeto.Core.Contratos;
using CaixaProjeto.Core.Dominio;
using CaixaProjeto.Core.Regras;
using Microsoft.EntityFrameworkCore;

namespace CaixaProjeto.ApiService.Services;

/// <summary>
/// Junta o motor (Core) e a base de dados: analisa um pedido e grava tudo numa só transação.
/// </summary>
public class PedidoService(CaixaDbContext db, MotorDecisao motor, TimeProvider relogio)
{
    public const string UtilizadorSistema = "sistema";

    /// <summary>
    /// Simulação (botão "Analisar"): analisa o pedido, regista a simulação e devolve o resultado.
    /// Não cria pedido: não há número, estado nem histórico.
    /// </summary>
    public async Task<ResultadoAnalise> SimularAsync(PedidoCredito dados, CancellationToken ct = default)
    {
        var resultado = motor.Analisar(dados);
        var agora = relogio.GetUtcNow().UtcDateTime;

        db.Simulacoes.Add(new Simulacao
        {
            Cliente = await ObterOuCriarClienteAsync(dados.Nif, agora, ct),
            Nif = dados.Nif,
            Idade = dados.Idade,
            RendimentoMensalLiquido = dados.RendimentoMensalLiquido,
            PrestacoesAtuais = dados.PrestacoesAtuais,
            ValorPretendido = dados.ValorPretendido,
            PrazoMeses = dados.PrazoMeses,
            SituacaoProfissional = dados.SituacaoProfissional,
            IncidentesCredito = dados.IncidentesCredito,
            Decisao = resultado.Decisao,
            DataSimulacao = agora
        });
        await db.SaveChangesAsync(ct);

        return resultado;
    }

    public async Task<PedidoSubmetido> SubmeterAsync(PedidoCredito dados, CancellationToken ct = default)
    {
        var resultado = motor.Analisar(dados);
        var agora = relogio.GetUtcNow().UtcDateTime;

        var pedido = new Pedido
        {
            Numero = await ProximoNumeroAsync(agora.Year, ct),
            Cliente = await ObterOuCriarClienteAsync(dados.Nif, agora, ct),
            Nif = dados.Nif,
            Idade = dados.Idade,
            RendimentoMensalLiquido = dados.RendimentoMensalLiquido,
            PrestacoesAtuais = dados.PrestacoesAtuais,
            ValorPretendido = dados.ValorPretendido,
            PrazoMeses = dados.PrazoMeses,
            SituacaoProfissional = dados.SituacaoProfissional,
            IncidentesCredito = dados.IncidentesCredito,
            PrestacaoEstimada = resultado.Indicadores?.PrestacaoEstimada,
            TaxaEsforco = resultado.Indicadores?.TaxaEsforco,
            IdadeFinalContrato = resultado.Indicadores?.IdadeFinalContrato,
            LimiteMontante = resultado.Indicadores?.LimiteMontante,
            DecisaoAutomatica = resultado.Decisao,
            EstadoAtual = resultado.Decisao,
            DataSubmissao = agora,
            Motivos = resultado.Motivos
                .Select(m => new MotivoPedido { Regra = m.Regra, Descricao = m.Descricao, Decisao = m.Decisao })
                .ToList(),
            Historico =
            [
                new HistoricoEstado
                {
                    EstadoAnterior = null,
                    EstadoNovo = resultado.Decisao,
                    Data = agora,
                    Utilizador = UtilizadorSistema,
                    Observacao = "Decisão automática"
                }
            ]
        };

        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(ct);   // o SaveChanges grava pedido, cliente, motivos e histórico numa transação

        return new PedidoSubmetido(pedido.Numero, resultado);
    }

    /// <summary>
    /// Uma página da lista de pedidos, do mais recente para o mais antigo.
    /// Se vier um estado, mostra só os pedidos que estão nesse estado.
    /// </summary>
    public async Task<Pagina<PedidoResumo>> ListarAsync(Decisao? estado, int numeroPagina, int tamanhoPagina, CancellationToken ct = default)
    {
        // Proteção contra valores sem sentido vindos do URL (?pagina=0, ?tamanho=5000)
        if (numeroPagina < 1)
        {
            numeroPagina = 1;
        }
        if (tamanhoPagina < 1 || tamanhoPagina > 100)
        {
            tamanhoPagina = 20;
        }

        // A query só é enviada à BD no CountAsync / ToListAsync; até lá vamos só construindo-a
        IQueryable<Pedido> query = db.Pedidos;
        if (estado is not null)
        {
            query = query.Where(p => p.EstadoAtual == estado);
        }

        var total = await query.CountAsync(ct);

        var itens = await query
            .OrderByDescending(p => p.DataSubmissao)
            .ThenByDescending(p => p.Numero)
            .Skip((numeroPagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(p => new PedidoResumo(
                p.Numero,
                p.Nif,
                p.ValorPretendido,
                p.PrazoMeses,
                p.TaxaEsforco,
                p.DecisaoAutomatica,
                p.EstadoAtual,
                p.DataSubmissao))
            .ToListAsync(ct);

        return new Pagina<PedidoResumo>(itens, total, numeroPagina, tamanhoPagina);
    }

    /// <summary>
    /// Tudo o que se sabe de um pedido, pelo número. Devolve null se o pedido não existir.
    /// Os dados vêm da BD tal como foram gravados: o motor não volta a correr.
    /// </summary>
    public async Task<PedidoDetalhe?> ObterAsync(string numero, CancellationToken ct = default)
    {
        // Include: traz também as linhas das tabelas MotivosPedido e HistoricoEstados deste pedido.
        // AsNoTracking: só vamos ler, por isso o EF não precisa de vigiar alterações.
        var pedido = await db.Pedidos
            .Include(p => p.Motivos)
            .Include(p => p.Historico)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Numero == numero, ct);

        if (pedido is null)
        {
            return null;
        }

        var dados = new PedidoCredito
        {
            Nif = pedido.Nif,
            Idade = pedido.Idade,
            RendimentoMensalLiquido = pedido.RendimentoMensalLiquido,
            PrestacoesAtuais = pedido.PrestacoesAtuais,
            ValorPretendido = pedido.ValorPretendido,
            PrazoMeses = pedido.PrazoMeses,
            SituacaoProfissional = pedido.SituacaoProfissional,
            IncidentesCredito = pedido.IncidentesCredito
        };

        // Os motivos pela ordem em que foram gravados (a ordem das regras)
        var motivos = pedido.Motivos
            .OrderBy(m => m.Id)
            .Select(m => new Motivo(m.Regra, m.Descricao, m.Decisao))
            .ToList();

        // Um pedido inválido não tem indicadores (ficaram a null na BD)
        Indicadores? indicadores = null;
        if (pedido.PrestacaoEstimada is not null
            && pedido.TaxaEsforco is not null
            && pedido.IdadeFinalContrato is not null
            && pedido.LimiteMontante is not null)
        {
            indicadores = new Indicadores(
                pedido.PrestacaoEstimada.Value,
                pedido.TaxaEsforco.Value,
                pedido.IdadeFinalContrato.Value,
                pedido.LimiteMontante.Value);
        }

        var resultado = new ResultadoAnalise(pedido.DecisaoAutomatica, motivos, indicadores);

        // O histórico do mais antigo para o mais recente
        var historico = pedido.Historico
            .OrderBy(h => h.Data)
            .ThenBy(h => h.Id)
            .Select(h => new EstadoHistorico(h.EstadoAnterior, h.EstadoNovo, h.Data, h.Utilizador, h.Observacao))
            .ToList();

        return new PedidoDetalhe(pedido.Numero, pedido.DataSubmissao, dados, resultado, pedido.EstadoAtual, historico);
    }

    /// <summary>
    /// Decisão de um analista sobre um pedido em ANÁLISE MANUAL: passa a APROVADO ou RECUSADO.
    /// A DecisaoAutomatica fica como estava (é a do motor); muda só o EstadoAtual,
    /// e a mudança fica registada no histórico com o nome do analista e a observação.
    /// </summary>
    public async Task<ResultadoDecisao> DecidirAsync(string numero, DecisaoAnalista decisao, CancellationToken ct = default)
    {
        // O nome e a observação são obrigatórios: é o que justifica a decisão numa auditoria
        if (string.IsNullOrWhiteSpace(decisao.Utilizador) || string.IsNullOrWhiteSpace(decisao.Observacao))
        {
            return ResultadoDecisao.DadosEmFalta;
        }

        // Sem AsNoTracking: desta vez vamos alterar o pedido, e o EF tem de dar pela alteração
        var pedido = await db.Pedidos.FirstOrDefaultAsync(p => p.Numero == numero, ct);
        if (pedido is null)
        {
            return ResultadoDecisao.NaoEncontrado;
        }

        // Só os pedidos em análise manual esperam por um analista
        if (pedido.EstadoAtual != Decisao.AnaliseManual)
        {
            return ResultadoDecisao.NaoAguardaAnalista;
        }

        Decisao novoEstado;
        if (decisao.Aprovar)
        {
            novoEstado = Decisao.Aprovado;
        }
        else
        {
            novoEstado = Decisao.Recusado;
        }

        db.HistoricoEstados.Add(new HistoricoEstado
        {
            PedidoId = pedido.Id,
            EstadoAnterior = pedido.EstadoAtual,
            EstadoNovo = novoEstado,
            Data = relogio.GetUtcNow().UtcDateTime,
            Utilizador = decisao.Utilizador.Trim(),
            Observacao = decisao.Observacao.Trim()
        });
        pedido.EstadoAtual = novoEstado;

        // Grava as duas coisas (novo estado e linha do histórico) na mesma transação
        await db.SaveChangesAsync(ct);
        return ResultadoDecisao.Decidido;
    }

    /// <summary>Número legível: ano + sequência de 4 dígitos (20260001, 20260002, ...).</summary>
    private async Task<string> ProximoNumeroAsync(int ano, CancellationToken ct)
    {
        var prefixo = ano.ToString();
        var ultimo = await db.Pedidos
            .Where(p => p.Numero.StartsWith(prefixo))
            .OrderByDescending(p => p.Numero)
            .Select(p => p.Numero)
            .FirstOrDefaultAsync(ct);

        var sequencia = ultimo is null ? 1 : int.Parse(ultimo[prefixo.Length..]) + 1;
        return $"{prefixo}{sequencia:0000}";
    }

    /// <summary>Só cria cliente quando o NIF é válido; um NIF inválido não identifica ninguém.</summary>
    private async Task<Cliente?> ObterOuCriarClienteAsync(string? nif, DateTime agora, CancellationToken ct)
    {
        if (!ValidacaoInicial.TemNoveDigitos(nif))
            return null;

        return await db.Clientes.FirstOrDefaultAsync(c => c.Nif == nif, ct)
            ?? new Cliente { Nif = nif!, DataRegisto = agora };
    }
}
