using Microsoft.FeatureManagement;

namespace PedidosApi.Features;

/// <summary>
/// Serviço de cálculo de desconto.
/// 
/// Padrão TBD + Feature Flag:
/// - O código da feature nova já está na main desde o primeiro commit 
/// - Flag desligada → comportamento antigo (zero desconto), sem risco para produção
/// - Flag ligada → nova lógica de negócio ativa
/// - O serviço não sabe nada sobre porcentagem de rollout — isso é responsabilidade do filtro 
/// </summary>
public class DescontoService(IFeatureManager featureManager)
{
    public async Task<decimal> CalcularAsync(
        decimal valorPedido,
        bool clientePremium)
    {
        // Se a flag estiver desligada (ou o usuário não estiver no grupo de rollout),
        // retorna zero — mantém o comportamento atual sem quebrar nada
        if (!await featureManager.IsEnabledAsync(FeatureFlags.DescontoPremium))
        {
            return 0;
        }

        if (await featureManager.IsEnabledAsync(FeatureFlags.DescontoProgressivo))
        {
            return valorPedido switch
            {
                >= 500 => valorPedido * 0.20m, // 20% para pedidos grandes
                >= 200 => valorPedido * 0.12m, // 12% para pedidos médios
                _      => valorPedido * 0.05m  // 5% para pedidos pequenos
            };
        }

        // Flag ativa → aplica a nova lógica de desconto
        return clientePremium
            ? valorPedido * 0.15m   // 15% para clientes premium
            : valorPedido * 0.05m;  // 5% para demais clientes
    }
}