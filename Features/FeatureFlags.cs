namespace PedidosApi.Features;

/// <summary>
/// Centraliza os nomes das feature flags como constantes.
/// Evita typos e facilita refatoração — se mudar o nome aqui,
/// o compilador aponta todos os lugares que precisam ser atualizados.
/// </summary>
public static class FeatureFlags
{
    public const string DescontoPremium = "DescontoPremium";
    public const string NovoCalculoFrete = "NovoCalculoFrete";
}