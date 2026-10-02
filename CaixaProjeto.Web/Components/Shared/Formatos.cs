using System.Globalization;
using CaixaProjeto.Core.Dominio;

namespace CaixaProjeto.Web.Components.Shared;

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
    /// As datas estão em UTC, mas o SQLite não guarda essa informação: marca-se como UTC antes de
    /// converter para a hora local.
    /// </summary>
    public static string DataHora(DateTime dataUtc) =>
        DateTime.SpecifyKind(dataUtc, DateTimeKind.Utc).ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public static string CorDecisao(Decisao decisao) => decisao switch
    {
        Decisao.Aprovado => "success",
        Decisao.AnaliseManual => "warning",
        Decisao.Recusado => "danger",
        _ => "secondary"
    };
}
