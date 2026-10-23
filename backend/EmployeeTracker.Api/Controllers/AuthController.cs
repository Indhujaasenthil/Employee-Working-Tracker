
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using EmployeeTracker.Api.Data;
using EmployeeTracker.Api.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeTracker.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public AuthController(
        AppDbContext context,
        IConfiguration configuration,
        IPasswordHasher<AppUser> passwordHasher)
    {
        _context = context;
        _configuration = configuration;
        _passwordHasher = passwordHasher;
    }

    // ==========================================
    // LOGIN
    // POST: api/auth/login
    // ==========================================

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request)
    {
        var userName =
            request.UserName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message =
                    "Username and password are required."
            });
        }

        var user = await _context.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.UserName.ToLower() ==
                userName.ToLower());

        if (user is null)
        {
            return InvalidCredentials();
        }

        var verificationResult =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

        if (verificationResult ==
            PasswordVerificationResult.Failed)
        {
            return InvalidCredentials();
        }

        // Validate the application role.
        var isAdministrator = string.Equals(
            user.Role,
            "Administrator",
            StringComparison.OrdinalIgnoreCase);

        var isManager = string.Equals(
            user.Role,
            "Manager",
            StringComparison.OrdinalIgnoreCase);

        var isEmployee = string.Equals(
            user.Role,
            "Employee",
            StringComparison.OrdinalIgnoreCase);

        if (!isAdministrator &&
            !isManager &&
            !isEmployee)
        {
            return Unauthorized(new
            {
                message = "Invalid user role."
            });
        }

        // Manager and Employee accounts must
        // be linked to an Employee record.
        if ((isManager || isEmployee) &&
            !user.EmployeeId.HasValue)
        {
            return Unauthorized(new
            {
                message =
                    "This login account is not linked to an employee."
            });
        }

        // Check linked Employee active status.
        if (user.EmployeeId.HasValue)
        {
            var employeeIsActive =
                await _context.Employees
                    .AsNoTracking()
                    .AnyAsync(employee =>
                        employee.Id == user.EmployeeId.Value &&
                        employee.IsActive);

            if (!employeeIsActive)
            {
                return Unauthorized(new
                {
                    message =
                        "The linked employee account is inactive or missing."
                });
            }
        }

        // Normalize role name for JWT claims.
        var normalizedRole =
            isAdministrator
                ? "Administrator"
                : isManager
                    ? "Manager"
                    : "Employee";

        var expiresAt =
            DateTime.UtcNow.AddHours(8);

        var token = CreateToken(
            user,
            normalizedRole,
            expiresAt);

        return Ok(new LoginResponse
        {
            Token = token,
            UserName = user.UserName,
            Role = normalizedRole,
            ExpiresAt = expiresAt
        });
    }

    // ==========================================
    // CHANGE PASSWORD
    // POST: api/auth/change-password
    // ==========================================

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(
            request.CurrentPassword))
        {
            return BadRequest(new
            {
                message = "Current password is required."
            });
        }

        if (string.IsNullOrWhiteSpace(
            request.NewPassword) ||
            request.NewPassword.Length < 8)
        {
            return BadRequest(new
            {
                message =
                    "New password must contain at least 8 characters."
            });
        }

        if (request.NewPassword.Length > 128)
        {
            return BadRequest(new
            {
                message =
                    "New password cannot exceed 128 characters."
            });
        }

        if (request.CurrentPassword ==
            request.NewPassword)
        {
            return BadRequest(new
            {
                message =
                    "New password must be different from the current password."
            });
        }

        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(
            userIdValue,
            out var userId))
        {
            return Unauthorized(new
            {
                message = "The login token is invalid."
            });
        }

        var user = await _context.AppUsers
            .FirstOrDefaultAsync(item =>
                item.Id == userId);

        if (user is null)
        {
            return Unauthorized(new
            {
                message = "User account was not found."
            });
        }

        var verificationResult =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.CurrentPassword);

        if (verificationResult ==
            PasswordVerificationResult.Failed)
        {
            return BadRequest(new
            {
                message =
                    "The current password is incorrect."
            });
        }

        user.PasswordHash =
            _passwordHasher.HashPassword(
                user,
                request.NewPassword);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Password changed successfully."
        });
    }

    // ==========================================
    // GENERATE JWT TOKEN
    // ==========================================

    private string CreateToken(
        AppUser user,
        string role,
        DateTime expiresAt)
    {
        var jwtKey =
            _configuration["Jwt:Key"];

        var jwtIssuer =
            _configuration["Jwt:Issuer"];

        var jwtAudience =
            _configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException(
                "JWT key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(jwtIssuer))
        {
            throw new InvalidOperationException(
                "JWT issuer is not configured.");
        }

        if (string.IsNullOrWhiteSpace(jwtAudience))
        {
            throw new InvalidOperationException(
                "JWT audience is not configured.");
        }

        if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must be at least 32 bytes.");
        }

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                user.UserName),

            new Claim(
                ClaimTypes.Role,
                role),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        if (user.EmployeeId.HasValue)
        {
            claims.Add(new Claim(
                "employeeId",
                user.EmployeeId.Value.ToString()));
        }

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    // ==========================================
    // INVALID CREDENTIALS RESPONSE
    // ==========================================

    private UnauthorizedObjectResult InvalidCredentials()
    {
        return Unauthorized(new
        {
            message =
                "Invalid username or password."
        });
    }
}
