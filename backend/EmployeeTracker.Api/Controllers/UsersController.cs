using EmployeeTracker.Api.Data;
using EmployeeTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeTracker.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Administrator")]
public class UsersController : ControllerBase
{
    private static readonly string[] AllowedRoles =
    {
        "Administrator",
        "Manager",
        "Employee"
    };

    private readonly AppDbContext _context;
    private readonly PasswordHasher<AppUser> _passwordHasher;

    public UsersController(
        AppDbContext context
    )
    {
        _context = context;

        _passwordHasher =
            new PasswordHasher<AppUser>();
    }

    [HttpGet]
    public async Task<ActionResult> GetUsers()
    {
        var users =
            await _context.AppUsers
                .AsNoTracking()
                .Include(user => user.Employee)
                .OrderBy(user => user.UserName)
                .Select(user => new
                {
                    user.Id,
                    user.UserName,
                    user.Role,
                    user.EmployeeId,

                    EmployeeCode =
                        user.Employee == null
                            ? null
                            : user.Employee.EmployeeCode,

                    EmployeeName =
                        user.Employee == null
                            ? null
                            : user.Employee.FullName
                })
                .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetUser(
        int id
    )
    {
        var user =
            await _context.AppUsers
                .AsNoTracking()
                .Include(item => item.Employee)
                .Where(item => item.Id == id)
                .Select(item => new
                {
                    item.Id,
                    item.UserName,
                    item.Role,
                    item.EmployeeId,

                    EmployeeCode =
                        item.Employee == null
                            ? null
                            : item.Employee.EmployeeCode,

                    EmployeeName =
                        item.Employee == null
                            ? null
                            : item.Employee.FullName
                })
                .FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound(new
            {
                message =
                    "User was not found."
            });
        }

        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult> CreateUser(
        UserRequest request
    )
    {
        var validationResult =
            await ValidateRequest(
                request,
                null,
                passwordRequired: true
            );

        if (validationResult is not null)
        {
            return validationResult;
        }

        var normalizedRole =
            NormalizeRole(request.Role);

        var requiresEmployeeLink =
            normalizedRole == "Manager" ||
            normalizedRole == "Employee";

        var user = new AppUser
        {
            UserName =
                request.UserName.Trim(),

            Role =
                normalizedRole,

            EmployeeId =
                requiresEmployeeLink
                    ? request.EmployeeId
                    : null
        };

        user.PasswordHash =
            _passwordHasher.HashPassword(
                user,
                request.Password!
            );

        _context.AppUsers.Add(user);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetUser),
            new { id = user.Id },
            new
            {
                user.Id,
                user.UserName,
                user.Role,
                user.EmployeeId
            }
        );
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateUser(
        int id,
        UserRequest request
    )
    {
        var user =
            await _context.AppUsers
                .FirstOrDefaultAsync(item =>
                    item.Id == id
                );

        if (user is null)
        {
            return NotFound(new
            {
                message =
                    "User was not found."
            });
        }

        var validationResult =
            await ValidateRequest(
                request,
                id,
                passwordRequired: false
            );

        if (validationResult is not null)
        {
            return validationResult;
        }

        var normalizedRole =
            NormalizeRole(request.Role);

        var requiresEmployeeLink =
            normalizedRole == "Manager" ||
            normalizedRole == "Employee";

        user.UserName =
            request.UserName.Trim();

        user.Role =
            normalizedRole;

        user.EmployeeId =
            requiresEmployeeLink
                ? request.EmployeeId
                : null;

        /*
         * When a new password is supplied,
         * generate and save a new password hash.
         *
         * When password is blank, keep the
         * existing password.
         */
        if (
            !string.IsNullOrWhiteSpace(
                request.Password
            )
        )
        {
            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    request.Password
                );
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteUser(
        int id
    )
    {
        var user =
            await _context.AppUsers
                .FirstOrDefaultAsync(item =>
                    item.Id == id
                );

        if (user is null)
        {
            return NotFound(new
            {
                message =
                    "User was not found."
            });
        }

        var currentUserName =
            User.Identity?.Name;

        if (
            string.Equals(
                currentUserName,
                user.UserName,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return BadRequest(new
            {
                message =
                    "You cannot delete your own login account."
            });
        }

        _context.AppUsers.Remove(user);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<ActionResult?>
        ValidateRequest(
            UserRequest request,
            int? existingUserId,
            bool passwordRequired
        )
    {
        request.UserName =
            request.UserName?.Trim() ??
            string.Empty;

        request.Role =
            request.Role?.Trim() ??
            string.Empty;

        if (
            string.IsNullOrWhiteSpace(
                request.UserName
            )
        )
        {
            return BadRequest(new
            {
                message =
                    "Username is required."
            });
        }

        if (request.UserName.Length < 3)
        {
            return BadRequest(new
            {
                message =
                    "Username must contain at least 3 characters."
            });
        }

        if (
            passwordRequired &&
            string.IsNullOrWhiteSpace(
                request.Password
            )
        )
        {
            return BadRequest(new
            {
                message =
                    "Password is required."
            });
        }

        if (
            !string.IsNullOrWhiteSpace(
                request.Password
            ) &&
            request.Password.Length < 6
        )
        {
            return BadRequest(new
            {
                message =
                    "Password must contain at least 6 characters."
            });
        }

        var normalizedRole =
            NormalizeRole(request.Role);

        if (
            !AllowedRoles.Contains(
                normalizedRole,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            return BadRequest(new
            {
                message =
                    "Role must be Administrator, Manager or Employee."
            });
        }

        var duplicateUserName =
            await _context.AppUsers
                .AnyAsync(user =>
                    user.UserName.ToLower() ==
                        request.UserName.ToLower() &&
                    user.Id != existingUserId
                );

        if (duplicateUserName)
        {
            return Conflict(new
            {
                message =
                    "This username already exists."
            });
        }

        /*
         * Administrator does not require
         * an Employee record.
         *
         * Manager and Employee do.
         */
        var requiresEmployeeLink =
            normalizedRole == "Manager" ||
            normalizedRole == "Employee";

        if (!requiresEmployeeLink)
        {
            return null;
        }

        if (!request.EmployeeId.HasValue)
        {
            return BadRequest(new
            {
                message =
                    "Please select an employee for the Manager or Employee role."
            });
        }

        var employeeExists =
            await _context.Employees
                .AnyAsync(employee =>
                    employee.Id ==
                        request.EmployeeId.Value &&
                    employee.IsActive
                );

        if (!employeeExists)
        {
            return BadRequest(new
            {
                message =
                    "The selected employee does not exist or is inactive."
            });
        }

        /*
         * One Employee record can only have
         * one login account.
         */
        var employeeAlreadyLinked =
            await _context.AppUsers
                .AnyAsync(user =>
                    user.EmployeeId ==
                        request.EmployeeId.Value &&
                    user.Id != existingUserId
                );

        if (employeeAlreadyLinked)
        {
            return Conflict(new
            {
                message =
                    "The selected employee already has a login account."
            });
        }

        return null;
    }

    private static string NormalizeRole(
        string role
    )
    {
        if (
            role.Equals(
                "Administrator",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return "Administrator";
        }

        if (
            role.Equals(
                "Manager",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return "Manager";
        }

        if (
            role.Equals(
                "Employee",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return "Employee";
        }

        return role;
    }
}