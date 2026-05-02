using Microsoft.FeatureManagement;
using NSubstitute;
using PedidosApi.Features;

namespace PedidosApi.Tests.Features;

public class DescontoServiceTests
{
    private readonly IFeatureManager _featureManager;
    private readonly DescontoService _service;

    public DescontoServiceTests()
    {
        _featureManager = Substitute.For<IFeatureManager>();
        _service = new DescontoService(_featureManager);
    }

    [Fact]
    public async Task CalcularAsync_QuandoFlagDesligada_RetornaZero()
    {
        _featureManager.IsEnabledAsync(FeatureFlags.DescontoPremium).Returns(false);

        var resultado = await _service.CalcularAsync(1000m, clientePremium: true);

        Assert.Equal(0m, resultado);
    }

    [Fact]
    public async Task CalcularAsync_QuandoFlagDesligada_IgnoraClientePremium()
    {
        _featureManager.IsEnabledAsync(FeatureFlags.DescontoPremium).Returns(false);

        var resultadoPremium = await _service.CalcularAsync(1000m, clientePremium: true);
        var resultadoComum = await _service.CalcularAsync(1000m, clientePremium: false);

        Assert.Equal(0m, resultadoPremium);
        Assert.Equal(0m, resultadoComum);
    }

    [Fact]
    public async Task CalcularAsync_QuandoFlagLigadaEClientePremium_Retorna15Porcento()
    {
        _featureManager.IsEnabledAsync(FeatureFlags.DescontoPremium).Returns(true);

        var resultado = await _service.CalcularAsync(1000m, clientePremium: true);

        Assert.Equal(150m, resultado);
    }

    [Fact]
    public async Task CalcularAsync_QuandoFlagLigadaEClienteNaoPremium_Retorna5Porcento()
    {
        _featureManager.IsEnabledAsync(FeatureFlags.DescontoPremium).Returns(true);

        var resultado = await _service.CalcularAsync(1000m, clientePremium: false);

        Assert.Equal(50m, resultado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(999.99)]
    public async Task CalcularAsync_ClientePremium_DescontoEh15PorcentoDoValor(decimal valor)
    {
        _featureManager.IsEnabledAsync(FeatureFlags.DescontoPremium).Returns(true);

        var resultado = await _service.CalcularAsync(valor, clientePremium: true);

        Assert.Equal(valor * 0.15m, resultado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(999.99)]
    public async Task CalcularAsync_ClienteNaoPremium_DescontoEh5PorcentoDoValor(decimal valor)
    {
        _featureManager.IsEnabledAsync(FeatureFlags.DescontoPremium).Returns(true);

        var resultado = await _service.CalcularAsync(valor, clientePremium: false);

        Assert.Equal(valor * 0.05m, resultado);
    }
}