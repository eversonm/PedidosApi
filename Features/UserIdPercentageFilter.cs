using Microsoft.FeatureManagement;

namespace PedidosApi.Features;

/// <summary>
/// Filtro de feature flag baseado em porcentagem de usuários.
/// 
/// Diferença em relação ao PercentageFilter nativo:
/// - PercentageFilter nativo: avalia por REQUISIÇÃO (não determinístico)
///   → mesmo usuário pode ver/não ver a feature em chamadas diferentes
/// 
/// - UserIdPercentageFilter (este): avalia por USUÁRIO (determinístico)
///   → hash do userId define o "bucket" (0-99) do usuário
///   → se bucket < Percentage, o usuário SEMPRE vê a feature
///   → experiência consistente para o usuário final
/// 
/// Configuração no appsettings.json:
/// "FeatureManagement": {
///   "DescontoPremium": {
///     "EnabledFor": [{
///       "Name": "UserIdPercentage",
///       "Parameters": { "Percentage": 20 }
///     }]
///   }
/// }
/// </summary>
[FilterAlias("UserIdPercentage")]
public class UserIdPercentageFilter(IHttpContextAccessor http) : IFeatureFilter
{
    public Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        var settings = context.Parameters.Get<UserIdPercentageSettings>()
            ?? new UserIdPercentageSettings();

        // Lê o userId do header X-User-Id
        // Em produção: adaptar para ler do JWT claim (ex: HttpContext.User.FindFirst("sub")?.Value)
        var userId = http.HttpContext?
            .Request.Headers["X-User-Id"]
            .FirstOrDefault()
            ?? "anonymous";

        // Hash determinístico: mesmo userId sempre cai no mesmo bucket (0-99)
        var hash = Math.Abs(userId.GetHashCode());
        var bucket = hash % 100;

        // Se o bucket do usuário estiver dentro da porcentagem configurada, habilita a feature
        return Task.FromResult(bucket < settings.Percentage);
    }
}

/// <summary>
/// Parâmetros lidos do appsettings.json para o filtro UserIdPercentage.
/// </summary>
public class UserIdPercentageSettings
{
    /// <summary>
    /// Porcentagem de usuários que devem ver a feature (0 a 100).
    /// </summary>
    public int Percentage { get; set; } = 0;
}