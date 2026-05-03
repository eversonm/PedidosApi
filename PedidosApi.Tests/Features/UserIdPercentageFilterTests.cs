using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using NSubstitute;
using PedidosApi.Features;

namespace PedidosApi.Tests.Features;

public class UserIdPercentageFilterTests
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserIdPercentageFilter _filter;

    public UserIdPercentageFilterTests()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _filter = new UserIdPercentageFilter(_httpContextAccessor);
    }

    private static FeatureFilterEvaluationContext CreateContext(int percentage)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Percentage"] = percentage.ToString()
            })
            .Build();

        return new FeatureFilterEvaluationContext { FeatureName = "TestFeature", Parameters = config };
    }

    private void SetupHttpContext(string? userId)
    {
        var httpContext = new DefaultHttpContext();
        if (userId is not null)
            httpContext.Request.Headers["X-User-Id"] = userId;

        _httpContextAccessor.HttpContext.Returns(httpContext);
    }

    [Fact]
    public async Task EvaluateAsync_QuandoPorcentagemZero_SempreRetornaFalso()
    {
        SetupHttpContext("qualquer-usuario");

        var resultado = await _filter.EvaluateAsync(CreateContext(0));

        Assert.False(resultado);
    }

    [Fact]
    public async Task EvaluateAsync_QuandoPorcentagem100_SempreRetornaVerdadeiro()
    {
        SetupHttpContext("qualquer-usuario");

        var resultado = await _filter.EvaluateAsync(CreateContext(100));

        Assert.True(resultado);
    }

    [Fact]
    public async Task EvaluateAsync_MesmoUserId_SempreRetornaMesmoResultado()
    {
        SetupHttpContext("usuario-fixo-xyz");
        var context = CreateContext(50);

        var r1 = await _filter.EvaluateAsync(context);
        var r2 = await _filter.EvaluateAsync(context);
        var r3 = await _filter.EvaluateAsync(context);

        Assert.Equal(r1, r2);
        Assert.Equal(r2, r3);
    }

    [Fact]
    public async Task EvaluateAsync_SemHeaderUserId_UtilizaAnonymousComoFallback()
    {
        SetupHttpContext(null);
        var bucket = Math.Abs("anonymous".GetHashCode()) % 100;

        var resultado = await _filter.EvaluateAsync(CreateContext(bucket + 1));

        Assert.True(resultado);
    }

    [Fact]
    public async Task EvaluateAsync_SemHttpContext_UtilizaAnonymousComoFallback()
    {
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var bucket = Math.Abs("anonymous".GetHashCode()) % 100;

        var resultado = await _filter.EvaluateAsync(CreateContext(bucket + 1));

        Assert.True(resultado);
    }

    [Theory]
    [InlineData("user-alpha")]
    [InlineData("user-beta")]
    [InlineData("user-gamma")]
    public async Task EvaluateAsync_UsuarioDentroDoRollout_RetornaVerdadeiro(string userId)
    {
        SetupHttpContext(userId);
        var bucket = Math.Abs(userId.GetHashCode()) % 100;

        var resultado = await _filter.EvaluateAsync(CreateContext(bucket + 1));

        Assert.True(resultado);
    }

    [Theory]
    [InlineData("user-alpha")]
    [InlineData("user-beta")]
    [InlineData("user-gamma")]
    public async Task EvaluateAsync_UsuarioForaDoRollout_RetornaFalso(string userId)
    {
        SetupHttpContext(userId);
        var bucket = Math.Abs(userId.GetHashCode()) % 100;

        var resultado = await _filter.EvaluateAsync(CreateContext(bucket));

        Assert.False(resultado);
    }
}