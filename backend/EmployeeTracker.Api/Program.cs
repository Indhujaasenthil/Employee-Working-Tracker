
using System.Text;

using EmployeeTracker.Api.Data;
using EmployeeTracker.Api.Models;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// CONTROLLERS
// ==========================================

builder.Services.AddControllers();

// ==========================================
// GLOBAL ERROR RESPONSE CONFIGURATION
// ==========================================

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler(options =>
{
    options.ExceptionHandler = async context =>
    {
        var logger = context.RequestServices
            .GetRequiredService<
                ILoggerFactory>()
            .CreateLogger("GlobalExceptionHandler");

        var exception = context.Features
            .Get<IExceptionHandlerFeature>()?
            .Error;

        if (exception != null)
        {
            logger.LogError(
                exception,
                "Unhandled exception occurred for {Path}",
                context.Request.Path);
        }

        context.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        context.Response.ContentType =
            "application/problem+json";

        var problem = new ProblemDetails
        {
            Status =
                StatusCodes.Status500InternalServerError,

            Title = "Internal Server Error",

            Detail =
                "An unexpected error occurred. " +
                "Please try again."
        };

        await context.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json");
    };
});

// ==========================================
// DATABASE CONNECTION
// ==========================================

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection")
    ?? throw new InvalidOperationException(
        "The DefaultConnection connection string is missing.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// ==========================================
// PASSWORD HASHER
// ==========================================

builder.Services.AddScoped<
    IPasswordHasher<AppUser>,
    PasswordHasher<AppUser>>();

// ==========================================
// JWT CONFIGURATION
// ==========================================

var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "The JWT key is missing.");

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "The JWT issuer is missing.");

var jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "The JWT audience is missing.");

if (string.IsNullOrWhiteSpace(jwtKey) ||
    Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must be at least 32 bytes.");
}

if (string.IsNullOrWhiteSpace(jwtIssuer) ||
    string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException(
        "JWT issuer and audience must be configured.");
}

// ==========================================
// JWT AUTHENTICATION
// ==========================================

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = true;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,

                RequireExpirationTime = true,
                RequireSignedTokens = true,

                ClockSkew = TimeSpan.FromMinutes(1),

                NameClaimType =
                    System.Security.Claims.ClaimTypes.Name,

                RoleClaimType =
                    System.Security.Claims.ClaimTypes.Role
            };
    });

// ==========================================
// AUTHORIZATION
// ==========================================

builder.Services.AddAuthorization();

// ==========================================
// CORS FOR ANGULAR
// ==========================================

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAngularApp",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:4200",
                    "http://localhost:4300",
                    "http://localhost:50614")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

// ==========================================
// BUILD APPLICATION
// ==========================================

var app = builder.Build();

// ==========================================
// DATABASE MIGRATION
// ==========================================

using (var scope = app.Services.CreateScope())
{
    var database =
        scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

    await database.Database.MigrateAsync();

    var passwordHasher =
        scope.ServiceProvider
            .GetRequiredService<
                IPasswordHasher<AppUser>>();

    const string administratorUserName = "admin";

    var administratorExists =
        await database.AppUsers.AnyAsync(user =>
            user.UserName == administratorUserName);

    if (!administratorExists)
    {
        var administratorPassword =
            builder.Configuration[
                "BootstrapAdmin:Password"];

        if (string.IsNullOrWhiteSpace(
            administratorPassword))
        {
            app.Logger.LogWarning(
                "Default administrator does not exist. " +
                "Configure BootstrapAdmin:Password " +
                "to create the initial administrator.");
        }
        else if (administratorPassword.Length < 8)
        {
            throw new InvalidOperationException(
                "Bootstrap administrator password " +
                "must contain at least 8 characters.");
        }
        else
        {
            var administrator = new AppUser
            {
                UserName = administratorUserName,
                Role = "Administrator"
            };

            administrator.PasswordHash =
                passwordHasher.HashPassword(
                    administrator,
                    administratorPassword);

            database.AppUsers.Add(administrator);

            await database.SaveChangesAsync();

            app.Logger.LogInformation(
                "Initial administrator account created.");
        }
    }
}

// ==========================================
// HTTP PIPELINE
// ==========================================

// Handle unexpected API exceptions before
// they reach the client.
app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseCors("AllowAngularApp");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
