namespace CaixaProjeto.ApiService.Services;

/// <summary>O Program.cs traduz cada valor num código HTTP (200, 400, 404 ou 409).</summary>
public enum ResultadoDecisao
{
    Decidido,
    DadosEmFalta,         // falta o nome do analista ou a observação
    NaoEncontrado,        // não existe pedido com esse número
    NaoAguardaAnalista    // o pedido não está em ANÁLISE MANUAL (já foi decidido, ou nunca precisou)
}
