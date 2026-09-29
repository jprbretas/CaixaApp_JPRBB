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
