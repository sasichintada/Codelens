using CodeLens.Core.Models;
using CodeLens.Core.Interfaces;  // ADD THIS for IAppDbContext
using CodeLens.Services.AI;
using CodeLens.API.Data;
using CodeLens.Services.Services;
using CodeLens.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System;
using System.Diagnostics;
using System.Text;
using Microsoft.OpenApi.Models;
using CodeLens.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CodeLens API", Version = "v1" });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below. Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Load AI configuration with error checking
var aiConfig = builder.Configuration.GetSection("AI").Get<AIConfiguration>();
if (aiConfig == null)
{
    Console.WriteLine("WARNING: AI section not found in appsettings.json, using defaults");
    aiConfig = new AIConfiguration();
}

if (string.IsNullOrEmpty(aiConfig.ApiKey))
{
    Console.WriteLine("ERROR: API Key is missing in appsettings.json!");
    Console.WriteLine("Please add your OpenRouter API key to appsettings.json");
}
else
{
    Console.WriteLine($"API Key loaded successfully (length: {aiConfig.ApiKey.Length})");
    Console.WriteLine($"API Key (first 5 chars): {aiConfig.ApiKey.Substring(0, Math.Min(5, aiConfig.ApiKey.Length))}...");
    Console.WriteLine($"Model: {aiConfig.Model}");
    Console.WriteLine($"BaseUrl: {aiConfig.BaseUrl}");
}

// IMPORTANT: Register as singleton so the same instance is used everywhere
builder.Services.AddSingleton(aiConfig);

// Register OpenRouter Service with the configuration
builder.Services.AddScoped<IAIAnalysisService, OpenRouterService>();

// Add database context for SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=codelens.db"));

// REGISTER IAppDbContext TO USE AppDbContext - THIS FIXES THE ERROR
builder.Services.AddScoped<IAppDbContext>(provider => 
    provider.GetRequiredService<AppDbContext>());

// Add Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? 
            "your-super-secret-key-with-at-least-32-characters!"))
    };
    
    // For SignalR later
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/analysisHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Register JwtService
builder.Services.AddScoped<IJwtService, JwtService>();

// Register SessionService
builder.Services.AddScoped<ISessionService, SessionService>();

// Add HttpContextAccessor for SessionService
builder.Services.AddHttpContextAccessor();
// Register IAppDbContext to use AppDbContext
builder.Services.AddScoped<IAppDbContext>(provider => 
    provider.GetRequiredService<AppDbContext>());

// Add Rate Limiting - SIMPLIFIED VERSION
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
    
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again later.", 
            cancellationToken);
    };
});

// Add caching
builder.Services.AddMemoryCache();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "http://localhost:5173",
            "http://localhost:5500",
            "http://127.0.0.1:5500"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CodeLens API v1");
        c.DisplayRequestDuration();
        c.EnableTryItOutByDefault();
    });
    app.UseDeveloperExceptionPage();
    
    // AUTO-OPEN BROWSER TO SWAGGER
    try
    {
        Console.WriteLine("Attempting to open Swagger UI in your browser...");
        var url = "http://localhost:5120/swagger";
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
        Console.WriteLine($"Browser opened to: {url}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not automatically open browser. Please manually navigate to:");
        Console.WriteLine("http://localhost:5120/swagger");
        Console.WriteLine($"Error: {ex.Message}");
    }
}

app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SessionTrackingMiddleware>();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    message = "CodeLens AI API is running with Authentication",
    version = "2.0",
    ai_model = aiConfig.Model,
    api_key_configured = !string.IsNullOrEmpty(aiConfig.ApiKey),
    api_key_length = aiConfig.ApiKey?.Length ?? 0,
    auth_enabled = true,
    rate_limiting_enabled = true,
    docs = "/swagger"
}));

// Ensure database is created and migrations are applied on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
    
    // Create default roles if they don't exist
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "User", "Admin" };
    
    foreach (var role in roles)
    {
        if (!roleManager.RoleExistsAsync(role).Result)
        {
            roleManager.CreateAsync(new IdentityRole(role)).Wait();
            Console.WriteLine($"✅ Role created: {role}");
        }
    }
    
    Console.WriteLine("✅ Database ensured at: codelens.db");
}

Console.WriteLine("✅ CodeLens Backend is ready!");
Console.WriteLine("🌐 Swagger UI: http://localhost:5120/swagger");
Console.WriteLine("🚀 Frontend should connect to: http://localhost:5120/api");
Console.WriteLine("🗄️  Database: codelens.db (SQLite)");
Console.WriteLine("🔐 Authentication: JWT Bearer Token");
Console.WriteLine("⏱️  Rate Limiting: 100 requests per minute");

app.Run();