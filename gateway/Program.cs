using Microsoft.AspNetCore.HttpOverrides;
using Yarp.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
    });
});

// Trust upstream headers (nginx, cloud load balancers, etc.)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHealthChecks();

var portFromEnv = builder.Configuration["PORT"];
var gatewayUrl = !string.IsNullOrWhiteSpace(portFromEnv)
    ? $"http://0.0.0.0:{portFromEnv}"
    : builder.Configuration["ASPNETCORE_URLS"];

gatewayUrl ??= builder.Configuration.GetValue<string>("Gateway:Url");
gatewayUrl ??= "http://0.0.0.0:5200";

builder.WebHost.UseUrls(gatewayUrl);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseRouting();
app.UseCors("AllowFrontend");

app.MapGet("/", () => Results.Ok(new
{
    service = "Gateway",
    status = "healthy",
    listeningOn = gatewayUrl
}));

app.MapHealthChecks("/health").RequireCors("AllowFrontend");
app.MapReverseProxy().RequireCors("AllowFrontend");

app.Run();
