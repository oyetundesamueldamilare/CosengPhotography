using CosengPhotography.Data;
using CosengPhotography.Helpers;
using CosengPhotography.Interfaces;
using CosengPhotography.Models;
using CosengPhotography.Repositories;
using CosengPhotography.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Resend;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. DATABASE CONFIGURATION
// =========================================================================
// Pulls standard connection string or Render environment equivalent
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings_DefaultConnection"];

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        // Enables resilient connections to smooth out transient cloud network interruptions
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    }));

// =========================================================================
// 2. CONTROLLERS & CORE SERVICES
// =========================================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// =========================================================================
// 3. SWAGGER GEN CONFIGURATION
// =========================================================================
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CosengPhotography API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token in the format: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// =========================================================================
// 4. IDENTITY CORE DESIGN (Must precede AddAuthentication)
// =========================================================================
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// =========================================================================
// 5. JWT AUTHENTICATION OVERRIDES
// =========================================================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // FIX: Fallback checks added to accept standard nested configurations OR single underscore environment models
    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? builder.Configuration["Jwt_Key"]
        ?? throw new InvalidOperationException("Configuration value 'Jwt:Key' or 'Jwt_Key' is required.");

    var issuer = builder.Configuration["Jwt:Issuer"] ?? builder.Configuration["Jwt_Issuer"];
    var audience = builder.Configuration["Jwt:Audience"] ?? builder.Configuration["Jwt_Audience"];

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// =========================================================================
// 6. CUSTOM DEPENDENCY INJECTION MATRIX
// =========================================================================
// 1. Register the thread-safe Queue manager as a Singleton
builder.Services.AddSingleton<IBackgroundEmailQueue, BackgroundEmailQueue>();

// 2. CORRECT RESEND REGISTRATION (Supports hierarchical section keys or flat single underscore keys)
builder.Services.Configure<ResendClientOptions>(options =>
{
    options.ApiToken = builder.Configuration["Resend:ApiKey"]
        ?? builder.Configuration["Resend_ApiKey"]
        ?? throw new InvalidOperationException("Resend API Key is missing from configuration.");
});

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddHttpClient<IResend, ResendClient>();

// 3. Register the BackgroundWorker process engine to boot up on app startup
builder.Services.AddHostedService<EmailBackgroundWorker>();
builder.Services.AddScoped<IEmailService, EmailService>();

// 4. Other App Services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IGalleryRepository, GalleryRepository>();
builder.Services.AddScoped<IGalleryService, GalleryService>();
builder.Services.AddSingleton<IGalleryTaskQueue, GalleryTaskQueue>();
builder.Services.AddHostedService<GalleryBackgroundWorker>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpClient<CloudflareBlobService>();
builder.Services.AddScoped<IBlobService, CloudflareBlobService>();

// =========================================================================
// 7. CORS POLICY (Maintained for Local Development compatibility)
// =========================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("https://localhost:7111", "http://localhost:5142")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // Same-origin deployments handle this seamlessly, but we target the live domain to be pristine
            policy.WithOrigins("https://cosengphotography.onrender.com")
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// =========================================================================
// 8. AUTOMATIC DATABASE MIGRATIONS & SEEDING ON STARTUP
// =========================================================================
// Combined table preparation and seeding routine so data structures exist on the target platform
_ = Task.Run(async () =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Ensure migrations run to create the schema tables before seeding checks occur
        Console.WriteLine("Applying pending database schema migrations on the environment target...");
        var dbContext = services.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        Console.WriteLine("Database seeding starting in the background...");
        await SeededRoleHelper.SeedRolesAndUsersAsync(services);
        Console.WriteLine("Database environment successfully prepared and seeded.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Background environment execution crashed: {ex.Message}");
    }
});

// =========================================================================
// 9. MIDDLEWARE PIPELINE ROUTING
// =========================================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CosengPhotography API v1");
    });
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serves the unified Blazor WebAssembly static binary framework files
app.UseBlazorFrameworkFiles();
// Maps delivery pipelines for traditional static content folders (images, CSS, JS scripts)
app.UseStaticFiles();

app.UseRouting();

// Pipeline execution order matrix
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Cleanly catches page refreshes and maps context back to the client router framework
app.MapFallbackToFile("index.html");

app.Run();
// =========================================================================
// 9. MIDDLEWARE PIPELINE ROUTING
// =========================================================================
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI(c =>
//    {
//        // REMOVED: c.RoutePrefix = string.Empty;
//        // This ensures Swagger stays safely out of the root URL's way.
//        // Your backend index is free, and Swagger lives explicitly at: https://localhost:7075/swagger
//        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CosengPhotography API v1");
//    });
//}
//else
//{
//    app.UseExceptionHandler("/error");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseRouting();

//// CRITICAL PIPELINE ORDER: UseCors MUST execute before authentication layers are parsed
//app.UseCors("AllowFrontend");

//app.UseAuthentication();
//app.UseAuthorization();

//app.MapControllers();

//app.Run();