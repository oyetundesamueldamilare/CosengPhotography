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
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. DATABASE CONFIGURATION
// =========================================================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Configuration value 'Jwt:Key' is required.");

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// =========================================================================
// 6. CUSTOM DEPENDENCY INJECTION MATRIX
// =========================================================================
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IBlobService, InMemoryBlobService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IGalleryRepository, GalleryRepository>();

// =========================================================================
// 7. CORS POLICY (Corrected to target Frontend App Ports instead of itself)
// =========================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Allowed entries encompass standard Blazor dev server ports
            policy.WithOrigins("https://localhost:7111", "http://localhost:5142")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            policy.WithOrigins("https://yourfrontend.com")
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// =========================================================================
// 8. DATA SEEDING IMPLEMENTATION
// =========================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeededRoleHelper.SeedRolesAndUsersAsync(services);
}

// =========================================================================
// 9. MIDDLEWARE PIPELINE ROUTING
// =========================================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // REMOVED: c.RoutePrefix = string.Empty;
        // This ensures Swagger stays safely out of the root URL's way.
        // Your backend index is free, and Swagger lives explicitly at: https://localhost:7075/swagger
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CosengPhotography API v1");
    });
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// CRITICAL PIPELINE ORDER: UseCors MUST execute before authentication layers are parsed
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();