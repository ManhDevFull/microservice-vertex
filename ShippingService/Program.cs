using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using ShippingService.Data;
using Serilog;
using ShippingService.GrpcImpl;

var builder = WebApplication.CreateBuilder(args);

// Serilog basic
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Match chat service pattern: dedicate ports for REST (Http1) and gRPC (Http2)
builder.WebHost.ConfigureKestrel(options =>
{
    // REST + Swagger on HTTP/1
    options.ListenAnyIP(5305, o => o.Protocols = HttpProtocols.Http1);
    // gRPC on HTTP/2
    options.ListenAnyIP(5306, o => o.Protocols = HttpProtocols.Http2);
});

// DbContext
var conn = builder.Configuration.GetConnectionString("Default")
           ?? Environment.GetEnvironmentVariable("MICRO_SHIPPING_CONN");
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(conn);
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("app", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddGrpc();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure DB + seed initial data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await Seed.EnsureSeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("app");
app.UseHttpsRedirection();
app.MapGrpcService<PaymentRpcImpl>();
app.MapGrpcService<ShippingRpcImpl>();
app.MapGrpcService<CheckoutRpcImpl>();
app.MapControllers().RequireCors("app");
app.Run();


