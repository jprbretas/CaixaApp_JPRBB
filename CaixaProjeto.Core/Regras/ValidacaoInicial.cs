using CaixaProjeto.Core.Dominio;

namespace CaixaProjeto.Core.Regras;

/// <summary>Regra 1. Junta todos os erros, para o cliente os corrigir de uma vez.</summary>
public static class ValidacaoInicial
{
    public const int Regra = 1;

    public static IReadOnlyList<Motivo> Validar(PedidoCredito pedido, ParametrosRegras parametros)
    {
        var erros = new List<Motivo>();

        void Erro(string descricao) => erros.Add(new Motivo(Regra, descricao, Decisao.PedidoInvalido));

        if (!TemNoveDigitos(pedido.Nif))
            Erro("O NIF tem de ter exatamente 9 dígitos");
        if (pedido.Idade < parametros.IdadeMinima)
            Erro($"O cliente tem de ter pelo menos {parametros.IdadeMinima} anos");
        if (pedido.RendimentoMensalLiquido <= 0)
            Erro("O rendimento mensal tem de ser superior a 0");
        if (pedido.ValorPretendido <= 0)
            Erro("O valor pretendido tem de ser superior a 0");
        if (pedido.PrazoMeses <= 0)
            Erro("O prazo tem de ser superior a 0 meses");

        // Não previstos no enunciado, mas também tornam o pedido impossível de analisar
        if (pedido.PrestacoesAtuais < 0)
            Erro("As prestações atuais não podem ser negativas");
        if (pedido.SituacaoProfissional is null)
            Erro("A situação profissional é obrigatória");

        return erros;
    }

    public static bool TemNoveDigitos(string? nif) =>
        nif is { Length: 9 } && nif.All(char.IsAsciiDigit);
}
