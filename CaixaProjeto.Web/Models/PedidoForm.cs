using CaixaProjeto.Core.Dominio;

namespace CaixaProjeto.Web.Models;

/// <summary>
/// Modelo do formulário. Existe à parte do PedidoCredito porque o Blazor precisa de
/// propriedades com "set" para fazer binding; o PedidoCredito é imutável (só "init").
/// A validação fica toda no motor (Regra 1), para o ecrã mostrar os mesmos motivos que a API.
/// </summary>
public class PedidoForm
{
    public string? Nif { get; set; }
    public int Idade { get; set; }
    public decimal RendimentoMensalLiquido { get; set; }
    public decimal PrestacoesAtuais { get; set; }
    public decimal ValorPretendido { get; set; }
    public int PrazoMeses { get; set; }
    public SituacaoProfissional? SituacaoProfissional { get; set; }
    public bool IncidentesCredito { get; set; }

    public PedidoCredito ParaPedido() => new()
    {
        Nif = Nif?.Trim(),
        Idade = Idade,
        RendimentoMensalLiquido = RendimentoMensalLiquido,
        PrestacoesAtuais = PrestacoesAtuais,
        ValorPretendido = ValorPretendido,
        PrazoMeses = PrazoMeses,
        SituacaoProfissional = SituacaoProfissional,
        IncidentesCredito = IncidentesCredito
    };

    /// <summary>Os quatro cenários da Tarefa 2 do enunciado, para preencher o formulário num clique.</summary>
    public static IReadOnlyDictionary<string, PedidoForm> Cenarios { get; } = new Dictionary<string, PedidoForm>
    {
        ["A"] = new() { Nif = "123456789", Idade = 35, RendimentoMensalLiquido = 2500, PrestacoesAtuais = 200, ValorPretendido = 10000, PrazoMeses = 60, SituacaoProfissional = Core.Dominio.SituacaoProfissional.Efetivo },
        ["B"] = new() { Nif = "123456789", Idade = 42, RendimentoMensalLiquido = 2000, PrestacoesAtuais = 500, ValorPretendido = 20000, PrazoMeses = 48, SituacaoProfissional = Core.Dominio.SituacaoProfissional.ContratoPrazo },
        ["C"] = new() { Nif = "123456789", Idade = 40, RendimentoMensalLiquido = 3000, PrestacoesAtuais = 300, ValorPretendido = 15000, PrazoMeses = 72, SituacaoProfissional = Core.Dominio.SituacaoProfissional.Efetivo, IncidentesCredito = true },
        ["D"] = new() { Nif = "123456789", Idade = 30, RendimentoMensalLiquido = 1200, PrestacoesAtuais = 300, ValorPretendido = 25000, PrazoMeses = 36, SituacaoProfissional = Core.Dominio.SituacaoProfissional.Efetivo }
    };

    public PedidoForm Copia() => (PedidoForm)MemberwiseClone();
}
