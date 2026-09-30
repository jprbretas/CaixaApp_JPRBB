using CaixaProjeto.ApiService.Data;
using CaixaProjeto.ApiService.Services;
using CaixaProjeto.Core;
using CaixaProjeto.Core.Dominio;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CaixaProjeto.ApiTests;

/// <summary>
/// Relógio que marca a hora que o teste quiser. O PedidoService recebe um TimeProvider,
/// por isso os testes podem "viajar no tempo" (por exemplo, criar um pedido com 60 dias).
/// </summary>
public class RelogioDeTeste(DateTimeOffset agora) : TimeProvider
{
    public DateTimeOffset Agora { get; set; } = agora;

    public override DateTimeOffset GetUtcNow() => Agora;
}

/// <summary>
/// Uma base de dados SQLite em memória, criada pelas mesmas migrações que criam a caixa.db.
/// Assim os testes confirmam também que as migrações dão uma base de dados que funciona.
/// Só existe enquanto a ligação estiver aberta. O xUnit cria uma instância da classe de testes
/// por cada teste, por isso cada teste tem a sua base de dados vazia e os testes não se misturam.
/// </summary>
public sealed class BaseDadosDeTeste : IDisposable
{
    public SqliteConnection Conexao { get; }

    // Por omissão, uma data fixa: os números dos pedidos ficam previsíveis (20260001, 20260002...)
    public RelogioDeTeste Relogio { get; } = new(new DateTimeOffset(2026, 9, 30, 10, 0, 0, TimeSpan.Zero));

    public BaseDadosDeTeste()
    {
        Conexao = new SqliteConnection("Data Source=:memory:");
        Conexao.Open();

        using var db = NovoContexto();
        db.Database.Migrate();
    }

    /// <summary>Um DbContext novo sobre a mesma base de dados.</summary>
    public CaixaDbContext NovoContexto()
    {
        var opcoes = new DbContextOptionsBuilder<CaixaDbContext>().UseSqlite(Conexao).Options;
        return new CaixaDbContext(opcoes);
    }

    /// <summary>
    /// Um PedidoService com um DbContext novo, como a API faz em cada pedido HTTP.
    /// Para confirmar o que ficou gravado, os testes leem com outro DbContext (NovoContexto),
    /// para verem o que está mesmo na base de dados e não o que o EF ainda tem em memória.
    /// </summary>
    public PedidoService Servico()
    {
        return new PedidoService(NovoContexto(), new MotorDecisao(), Relogio);
    }

    public void Dispose()
    {
        Conexao.Dispose();
    }
}

/// <summary>Pedidos de exemplo, a partir do cenário A do enunciado (aprovado).</summary>
public static class Exemplos
{
    public static readonly PedidoCredito Aprovado = new()
    {
        Nif = "123456789",
        Idade = 35,
        RendimentoMensalLiquido = 2500,
        PrestacoesAtuais = 200,
        ValorPretendido = 10000,
        PrazoMeses = 60,
        SituacaoProfissional = SituacaoProfissional.Efetivo,
        IncidentesCredito = false
    };

    /// <summary>Cenário B do enunciado: contrato a prazo e taxa de esforço de 45,83% (regras 4 e 6).</summary>
    public static readonly PedidoCredito AnaliseManual = new()
    {
        Nif = "123456789",
        Idade = 42,
        RendimentoMensalLiquido = 2000,
        PrestacoesAtuais = 500,
        ValorPretendido = 20000,
        PrazoMeses = 48,
        SituacaoProfissional = SituacaoProfissional.ContratoPrazo,
        IncidentesCredito = false
    };

    /// <summary>Recusado pela Regra 3 (incidentes de crédito).</summary>
    public static readonly PedidoCredito Recusado = Aprovado with { IncidentesCredito = true };

    /// <summary>Inválido pela Regra 1: NIF com 2 dígitos e 15 anos.</summary>
    public static readonly PedidoCredito Invalido = Aprovado with { Nif = "12", Idade = 15 };
}
