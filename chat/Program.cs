using System.IO;
using System.Security.Claims;
using System.Text;
using ChatRealtime;
using ChatService.Configurations;
using ChatService.Data;
using Dotnet.Grpc;
using Grpc.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5295, o => o.Protocols = HttpProtocols.Http1); // REST + SignalR
    options.ListenAnyIP(5296, o => o.Protocols = HttpProtocols.Http2); // gRPC
});


var authGrpcAddress = builder.Configuration["Grpc:AuthUrl"];
if (string.IsNullOrWhiteSpace(authGrpcAddress))
    throw new InvalidOperationException("Grpc:AuthUrl is not configured.");

builder.Services.AddGrpc(); // Cho ChatService expose gRPC của chính nó
builder.Services.AddGrpcClient<AuthGrpc.AuthGrpcClient>(o =>
{
    o.Address = new Uri(authGrpcAddress);
}).ConfigureChannel(options =>
{
    options.Credentials = ChannelCredentials.Insecure; // không dùng TLS nội bộ
});

builder.Services.AddDbContext<ChatDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.Configure<ChatSettings>(builder.Configuration.GetSection("Chat"));

var dataProtectionPath = Path.Combine(AppContext.BaseDirectory, "..", "data-protection");
Directory.CreateDirectory(dataProtectionPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("ecommerce-platform");

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtKey))
    throw new Exception("JWT Key is missing. Please set env JWT__KEY");

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        // ✅ Hỗ trợ JWT qua query string cho SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"JWT Authentication Failed: {ctx.Exception?.Message}");
                return Task.CompletedTask;
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    success = false,
                    message = "Bạn không có quyền truy cập tài nguyên này",
                    code = 403
                });
                await context.Response.WriteAsync(result);
            }
        };
    });


builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true));
});


builder.Services.AddControllers();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors();
app.UseWebSockets();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// gRPC service (ChatService exposes for internal calls)
app.MapGrpcService<ChatService.Grpc.ChatGrpcService>();

// SignalR realtime
app.MapHub<ChatHub>("/chatHub");

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "chat" }));
app.Run();
