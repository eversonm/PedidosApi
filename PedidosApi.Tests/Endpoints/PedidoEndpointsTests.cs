using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PedidosApi.Tests.Endpoints;

public class PedidoEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PedidoEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithFlags(Dictionary<string, string?> flags)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(flags));
        }).CreateClient();
    }

    [Fact]
    public async Task GET_Pedidos_RetornaTresPedidos()
    {
        var client = _factory.CreateClient();

        var resposta = await client.GetAsync("/pedidos");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var pedidos = await resposta.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(pedidos);
        Assert.Equal(3, pedidos.Length);
    }

    [Fact]
    public async Task GET_Pedidos_RetornaPedidosComCamposEsperados()
    {
        var client = _factory.CreateClient();

        var resposta = await client.GetAsync("/pedidos");
        var pedidos = (await resposta.Content.ReadFromJsonAsync<JsonElement[]>())!;

        var primeiro = pedidos[0];
        Assert.True(primeiro.TryGetProperty("id", out _));
        Assert.True(primeiro.TryGetProperty("valor", out _));
        Assert.True(primeiro.TryGetProperty("clientePremium", out _));
    }

    [Fact]
    public async Task POST_CalcularDesconto_FlagDesligada_RetornaDescontoZeroESemFeatureAtiva()
    {
        var client = CreateClientWithFlags(new() { ["FeatureManagement:DescontoPremium"] = "false" });
        var payload = new { Valor = 1000m, ClientePremium = true };

        var resposta = await client.PostAsJsonAsync("/pedidos/calcular-desconto", payload);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var resultado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0m, resultado.GetProperty("desconto").GetDecimal());
        Assert.Equal(1000m, resultado.GetProperty("total").GetDecimal());
        Assert.False(resultado.GetProperty("featureAtiva").GetBoolean());
    }

    [Fact]
    public async Task POST_CalcularDesconto_FlagLigadaClientePremium_Retorna15PorcentoDesconto()
    {
        var client = CreateClientWithFlags(new() { ["FeatureManagement:DescontoPremium"] = "true", ["FeatureManagement:DescontoProgressivo"] = "false" });
        var payload = new { Valor = 1000m, ClientePremium = true };

        var resposta = await client.PostAsJsonAsync("/pedidos/calcular-desconto", payload);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var resultado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(150m, resultado.GetProperty("desconto").GetDecimal());
        Assert.Equal(850m, resultado.GetProperty("total").GetDecimal());
        Assert.True(resultado.GetProperty("featureAtiva").GetBoolean());
    }

    [Fact]
    public async Task POST_CalcularDesconto_FlagLigadaClienteNaoPremium_Retorna5PorcentoDesconto()
    {
        var client = CreateClientWithFlags(new() { ["FeatureManagement:DescontoPremium"] = "true", ["FeatureManagement:DescontoProgressivo"] = "false" });
        var payload = new { Valor = 1000m, ClientePremium = false };

        var resposta = await client.PostAsJsonAsync("/pedidos/calcular-desconto", payload);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var resultado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(50m, resultado.GetProperty("desconto").GetDecimal());
        Assert.Equal(950m, resultado.GetProperty("total").GetDecimal());
        Assert.True(resultado.GetProperty("featureAtiva").GetBoolean());
    }

    [Fact]
    public async Task GET_FeatureFlags_QuandoDescontoPremiumLigado_RetornaVerdadeiro()
    {
        var client = CreateClientWithFlags(new()
        {
            ["FeatureManagement:DescontoPremium"] = "true",
            ["FeatureManagement:NovoCalculoFrete"] = "false"
        });

        var resposta = await client.GetAsync("/feature-flags");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var resultado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(resultado.GetProperty("descontoPremium").GetBoolean());
        Assert.False(resultado.GetProperty("novoCalculoFrete").GetBoolean());
    }

    [Fact]
    public async Task GET_FeatureFlags_QuandoAmbasDesligadas_RetornaFalsoParaAmbas()
    {
        var client = CreateClientWithFlags(new()
        {
            ["FeatureManagement:DescontoPremium"] = "false",
            ["FeatureManagement:NovoCalculoFrete"] = "false"
        });

        var resposta = await client.GetAsync("/feature-flags");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var resultado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(resultado.GetProperty("descontoPremium").GetBoolean());
        Assert.False(resultado.GetProperty("novoCalculoFrete").GetBoolean());
    }
}