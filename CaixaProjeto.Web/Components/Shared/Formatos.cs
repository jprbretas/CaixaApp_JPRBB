using System.Globalization;
using CaixaProjeto.Core.Dominio;

namespace CaixaProjeto.Web.Components.Shared;

/// <summary>Formatação de valores para o ecrã, sempre no estilo português ("1.234,56 €").</summary>
public static class Formatos
{
    private static readonly NumberFormatInfo Pt = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ","
    };

    public static string Euros(decimal valor) => valor.ToString("#,##0.00", Pt) + " €";

    public static string Percentagem(decimal valor) => valor.ToString("0.00", Pt) + "%";

    public static string Anos(decimal valor) => valor.ToString("0.#", Pt) + " anos";

    /// <summary>
    /// Data e hora à portuguesa, na hora local. A BD guarda as datas em UTC, mas o SQLite
    /// não guarda essa informação, por isso dizemos explicitamente que a data é UTC antes de converter.
    /// </summary>
    public static string DataHora(DateTime dataUtc) =>
        DateTime.SpecifyKind(dataUtc, DateTimeKind.Utc).ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    /// <summary>Classe Bootstrap da cor de cada decisão.</summary>
    public static string CorDecisao(Decisao decisao) => decisao switch
    {
        Decisao.Aprovado => "success",
        Decisao.AnaliseManual => "warning",
        Decisao.Recusado => "danger",
        _ => "secondary"
    };
}
