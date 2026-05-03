using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using PedidosApi.Features;
using PedidosApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddFeatureManagement()
    .AddFeatureFilter<PercentageFilter>()
    .AddFeatureFilter<UserIdPercentageFilter>();

builder.Services.AddScoped<DescontoService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Pedidos API",
        Version = "v1",
        Description = "API de pedidos com demonstração de Feature Flags (TBD)"
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPedidoEndpoints();

app.Run();

public partial class Program { }