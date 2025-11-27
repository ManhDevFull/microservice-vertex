using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using PaymentService.Data;
using PaymentService.Services;
using Serilog;
using PaymentService.Grpc;

var builder = WebApplication.CreateBuilder(args);

// Serilog basic
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Match ShippingService pattern: dedicate ports for REST (Http1) and gRPC (Http2)
builder.WebHost.ConfigureKestrel(options =>
{
    // REST + Swagger on HTTP/1
    options.ListenAnyIP(5307, o => o.Protocols = HttpProtocols.Http1);
    // gRPC on HTTP/2
    options.ListenAnyIP(5308, o => o.Protocols = HttpProtocols.Http2);
});

// DbContext
var conn = builder.Configuration.GetConnectionString("Default")
           ?? Environment.GetEnvironmentVariable("MICRO_PAYMENT_CONN");
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(conn);
});

// CORS
var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:3000" };
builder.Services.AddCors(o => o.AddPolicy("app", p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddGrpc();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register MoMo service
builder.Services.AddHttpClient<MoMoService>();
builder.Services.AddScoped<MoMoService>();

// Register HttpClientFactory for calling main backend
builder.Services.AddHttpClient();

var app = builder.Build();

// Ensure DB - Create table if not exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        // Try to query the table - if it doesn't exist, this will throw
        try
        {
            db.Database.ExecuteSqlRaw("SELECT 1 FROM payment_transaction LIMIT 1;");
            Log.Information("payment_transaction table already exists");
        }
        catch
        {
            // Table doesn't exist, create it
            Log.Information("Creating payment_transaction table...");
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE payment_transaction (
                    id SERIAL PRIMARY KEY,
                    order_id VARCHAR(100) NOT NULL UNIQUE,
                    account_id INTEGER NOT NULL,
                    partner_code VARCHAR(50) NOT NULL,
                    request_id VARCHAR(100) NOT NULL UNIQUE,
                    amount BIGINT NOT NULL,
                    order_info VARCHAR(500) NOT NULL,
                    payment_url TEXT,
                    qr_code TEXT,
                    status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
                    momo_transaction_id VARCHAR(100),
                    response_code VARCHAR(50),
                    message TEXT,
                    signature TEXT,
                    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    paid_at TIMESTAMP
                );
                
                CREATE INDEX idx_payment_transaction_account ON payment_transaction(account_id);
                CREATE INDEX idx_payment_transaction_status ON payment_transaction(status);
                CREATE INDEX idx_payment_transaction_order_id ON payment_transaction(order_id);
                CREATE INDEX idx_payment_transaction_request_id ON payment_transaction(request_id);
            ");
            Log.Information("payment_transaction table created successfully");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error ensuring payment_transaction table: {Message}", ex.Message);
        // Try EnsureCreated as fallback
        try
        {
            db.Database.EnsureCreated();
        }
        catch (Exception ensureEx)
        {
            Log.Error(ensureEx, "Error with EnsureCreated: {Message}", ensureEx.Message);
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("app");
app.UseHttpsRedirection();
app.MapGrpcService<PaymentService.Grpc.PaymentRpcImpl>();
app.MapControllers();
app.Run();

