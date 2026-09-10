using DocumentService.Data;
using DocumentService.Repositories;
using DocumentService.Services.Ai;
using DocumentService.Services.Documents;
using DocumentService.Services.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Minio;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// DATABASE
// ============================================================

builder.Services.AddDbContext<DocumentDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ============================================================
// DEPENDENCY INJECTION
// ============================================================

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();

builder.Services.AddScoped<
    IDocumentService,
    DocumentServiceImpl>();

// ============================================================
// MINIO (stockage des fichiers)
// ============================================================

builder.Services.Configure<MinioOptions>(
    builder.Configuration.GetSection("Minio"));

var minioOptions = builder.Configuration
    .GetSection("Minio")
    .Get<MinioOptions>()
    ?? throw new InvalidOperationException("Configuration Minio manquante.");

builder.Services.AddSingleton<IMinioClient>(_ =>
    new MinioClient()
        .WithEndpoint(minioOptions.Endpoint)
        .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
        .WithSSL(minioOptions.UseSSL)
        // Timeout explicite : une opération réseau MinIO ne doit pas
        // attendre indéfiniment. La valeur est configurable dans appsettings.
        .WithTimeout((int)TimeSpan.FromSeconds(minioOptions.TimeoutSeconds).TotalMilliseconds)
        .Build());

builder.Services.AddScoped<IFileStorageService, MinioStorageService>();

// ============================================================
// SERVICE IA (notification d'indexation)
// ============================================================

var aiServiceBaseUrl = builder.Configuration["AiService:BaseUrl"]
    ?? throw new InvalidOperationException("AiService:BaseUrl est manquant.");

builder.Services.AddHttpClient<IAiIngestionClient, AiIngestionClient>(client =>
{
    client.BaseAddress = new Uri(aiServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
});

// ============================================================
// JWT
// ============================================================

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key est manquant.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer est manquant.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience est manquant.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key doit contenir au moins 32 octets pour HS256.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ClockSkew = TimeSpan.Zero,

            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };
    });

builder.Services.AddAuthorization();

// ============================================================
// CONTROLLERS + SWAGGER
// ============================================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ============================================================
// DATABASE MIGRATIONS
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<DocumentDbContext>();

    await context.Database.MigrateAsync();

    var storage = (MinioStorageService)scope.ServiceProvider
        .GetRequiredService<IFileStorageService>();

    await storage.EnsureBucketExistsAsync();
}

// ============================================================
// HTTP PIPELINE
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// IMPORTANT : pas de HTTPS dans Docker
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();