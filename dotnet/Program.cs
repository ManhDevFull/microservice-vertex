using System.IO;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Google.Apis.Auth.OAuth2;
using FirebaseAdmin;
using Grpc.Core;
using be_dotnet_ecommerce1.Data;
using be_dotnet_ecommerce1.Dtos;
using be_dotnet_ecommerce1.Repository;
using be_dotnet_ecommerce1.Repository.IRepository;
using be_dotnet_ecommerce1.Service;
using be_dotnet_ecommerce1.Service.IService;
using dotnet.Repository;
using dotnet.Repository.IRepository;
using dotnet.Service;
using dotnet.Service.IService;
using Chat.Grpc;
using be_dotnet_ecommerce1.Repository.IReopsitory;
using be.Service.IService;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5100, o => o.Protocols = HttpProtocols.Http2); // gRPC
    options.ListenAnyIP(5000, o => o.Protocols = HttpProtocols.Http1); // REST API
});

builder.Services.AddDbContext<ConnectData>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});


// Repositories
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IUserReponsitory, UserReponsitory>();
builder.Services.AddScoped<IAddressReponsitory, AddressReponsitory>();
builder.Services.AddScoped<IProductReponsitory, ProductReponsitory>();
builder.Services.AddScoped<IVariantRepository, VariantRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();

// Services
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IVariantService, VariantService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

// Cloudinary
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));

var dataProtectionPath = Path.Combine(AppContext.BaseDirectory, "..", "data-protection");
Directory.CreateDirectory(dataProtectionPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("ecommerce-platform");

//JWT
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

        options.Events = new JwtBearerEvents
        {
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
            },
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"JWT Authentication Failed: {ctx.Exception?.Message}");
                return Task.CompletedTask;
            }
        };
    });
if (FirebaseApp.DefaultInstance == null)
{
    // 🔹 Local
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile("vertex.json")
    });
    // 🔹 Production (read from environment)
    // var json = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS_JSON");
    // FirebaseApp.Create(new AppOptions
    // {
    //     Credential = GoogleCredential.FromJson(json)
    // });
}


var chatGrpcAddress = builder.Configuration["Grpc:ChatUrl"];
if (string.IsNullOrWhiteSpace(chatGrpcAddress))
    throw new Exception("Grpc:ChatUrl is missing. Please configure chat service address.");
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
});
builder.Services.AddGrpcClient<ChatGrpc.ChatGrpcClient>(o =>
{
    o.Address = new Uri(chatGrpcAddress);
}).ConfigureChannel(options =>
{
    options.Credentials = ChannelCredentials.Insecure;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNext", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "http://localhost:5296",
            "https://vertex-ecom.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowNext");
app.UseAuthentication();
app.UseAuthorization();

// Handle preflight
app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Options)
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }
    await next();
});

app.MapControllers();
app.MapGrpcService<AuthGrpcService>();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "dotnet" }));
app.Run();
