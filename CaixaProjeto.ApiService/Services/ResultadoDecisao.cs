namespace CaixaProjeto.ApiService.Services;

/// <summary>
/// Como correu a decisão de um analista. O Program.cs transforma cada valor
/// na resposta HTTP certa (200, 400, 404 ou 409).
/// </summary>
public enum ResultadoDecisao
{
    Decidido,
    DadosEmFalta,         // falta o nome do analista ou a observação
    NaoEncontrado,        // não existe pedido com esse número
    NaoAguardaAnalista    // o pedido não está em ANÁLISE MANUAL (já foi decidido, ou nunca precisou)
}
