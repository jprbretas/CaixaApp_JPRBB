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

    /// <summary>Classe Bootstrap da cor de cada decisão.</summary>
    public static string CorDecisao(Decisao decisao) => decisao switch
    {
        Decisao.Aprovado => "success",
        Decisao.AnaliseManual => "warning",
        Decisao.Recusado => "danger",
        _ => "secondary"
    };
}
