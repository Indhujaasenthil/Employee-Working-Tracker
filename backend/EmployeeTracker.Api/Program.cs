using System.Text;
using EmployeeTracker.Api.Data;
using EmployeeTracker.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString =
    builder.Configuration.GetConnectionString(
        "DefaultConnection"
    ) ?? throw new InvalidOperationException(
        "The DefaultConnection connection string is missing."
    );

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<
    IPasswordHasher<AppUser>,
    PasswordHasher<AppUser>
>();

var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "The JWT key is missing."
    );

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "The JWT issuer is missing."
    );

var jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "The JWT audience is missing."
    );

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)
                    ),

                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromMinutes(1)
            };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAngularApp",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    );
});

var app = builder.Build();

// Apply migrations and create the default administrator.
using (var scope = app.Services.CreateScope())
{
    var database =
        scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

    await database.Database.MigrateAsync();

    var passwordHasher =
        scope.ServiceProvider
            .GetRequiredService<
                IPasswordHasher<AppUser>
            >();

    const string administratorUserName = "admin";
    const string administratorPassword = "admin123";

    var administratorExists =
        await database.AppUsers.AnyAsync(
            user =>
                user.UserName ==
                administratorUserName
        );

    if (!administratorExists)
    {
        var administrator = new AppUser
        {
            UserName = administratorUserName,
            Role = "Administrator"
        };

        administrator.PasswordHash =
            passwordHasher.HashPassword(
                administrator,
                administratorPassword
            );

        database.AppUsers.Add(administrator);

        await database.SaveChangesAsync();
    }
}

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();