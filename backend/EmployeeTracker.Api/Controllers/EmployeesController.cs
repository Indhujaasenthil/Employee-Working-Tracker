using System.Security.Claims;
using EmployeeTracker.Api.Data;
using EmployeeTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeTracker.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _database;

    public EmployeesController(AppDbContext database)
    {
        _database = database;
    }

    // =====================================================
    // GET EMPLOYEES
    // Administrator -> all employees
    // Manager       -> self + direct reports
    // Employee      -> self only
    // =====================================================

    [HttpGet]
    [Authorize(Roles = "Administrator,Manager,Employee")]
    public async Task<ActionResult> GetEmployees()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);

        var query = _database.Employees
            .AsNoTracking()
            .AsQueryable();

        // -------------------------------------------------
        // MANAGER
        // Only manager himself/herself + direct reports
        // -------------------------------------------------
        if (string.Equals(
                role,
                "Manager",
                StringComparison.OrdinalIgnoreCase))
        {
            var employeeId = GetLoggedInEmployeeId();

            if (!employeeId.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "Manager login is not linked to an employee."
                });
            }

            query = query.Where(employee =>
                employee.Id == employeeId.Value ||
                employee.ManagerId == employeeId.Value
            );
        }

        // -------------------------------------------------
        // EMPLOYEE
        // Only own employee record
        // -------------------------------------------------
        else if (string.Equals(
                     role,
                     "Employee",
                     StringComparison.OrdinalIgnoreCase))
        {
            var employeeId = GetLoggedInEmployeeId();

            if (!employeeId.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "Employee login is not linked to an employee."
                });
            }

            query = query.Where(employee =>
                employee.Id == employeeId.Value
            );
        }

        // Administrator reaches here without additional
        // filtering and therefore receives all employees.

        var employees = await query
            .OrderBy(employee => employee.EmployeeCode)
            .Select(employee => new
            {
                employee.Id,
                employee.EmployeeCode,
                employee.FullName,
                employee.Email,
                employee.Department,
                employee.Designation,

                employee.ManagerId,

                ManagerName =
                    employee.Manager != null
                        ? employee.Manager.FullName
                        : null,

                ManagerEmployeeCode =
                    employee.Manager != null
                        ? employee.Manager.EmployeeCode
                        : null,

                employee.IsActive,
                employee.CreatedAt
            })
            .ToListAsync();

        return Ok(employees);
    }

    // =====================================================
    // GET EMPLOYEE BY ID
    // Administrator -> any employee
    // Manager       -> self/direct report only
    // Employee      -> self only
    // =====================================================

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Administrator,Manager,Employee")]
    public async Task<ActionResult> GetEmployee(int id)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);

        var employee = await _database.Employees
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Id,
                item.EmployeeCode,
                item.FullName,
                item.Email,
                item.Department,
                item.Designation,

                item.ManagerId,

                ManagerName =
                    item.Manager != null
                        ? item.Manager.FullName
                        : null,

                ManagerEmployeeCode =
                    item.Manager != null
                        ? item.Manager.EmployeeCode
                        : null,

                item.IsActive,
                item.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (employee is null)
        {
            return NotFound(new
            {
                message = "Employee was not found."
            });
        }

        // Administrator can access any employee.
        if (string.Equals(
                role,
                "Administrator",
                StringComparison.OrdinalIgnoreCase))
        {
            return Ok(employee);
        }

        var loggedInEmployeeId =
            GetLoggedInEmployeeId();

        if (!loggedInEmployeeId.HasValue)
        {
            return Unauthorized(new
            {
                message =
                    "The login account is not linked to an employee."
            });
        }

        // Employee can only access own record.
        if (string.Equals(
                role,
                "Employee",
                StringComparison.OrdinalIgnoreCase))
        {
            if (id != loggedInEmployeeId.Value)
            {
                return Forbid();
            }

            return Ok(employee);
        }

        // Manager can access:
        // 1. own record
        // 2. direct-report employee
        if (string.Equals(
                role,
                "Manager",
                StringComparison.OrdinalIgnoreCase))
        {
            if (id == loggedInEmployeeId.Value)
            {
                return Ok(employee);
            }

            var isDirectReport =
                await _database.Employees
                    .AsNoTracking()
                    .AnyAsync(item =>
                        item.Id == id &&
                        item.ManagerId ==
                            loggedInEmployeeId.Value
                    );

            if (!isDirectReport)
            {
                return Forbid();
            }

            return Ok(employee);
        }

        return Forbid();
    }

    // =====================================================
    // CREATE EMPLOYEE
    // Administrator only
    // =====================================================

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult> CreateEmployee(
        Employee request)
    {
        var employeeCode =
            request.EmployeeCode?.Trim() ?? string.Empty;

        var fullName =
            request.FullName?.Trim() ?? string.Empty;

        var email =
            request.Email?.Trim() ?? string.Empty;

        var department =
            request.Department?.Trim() ?? string.Empty;

        var designation =
            request.Designation?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            return BadRequest(new
            {
                message = "Employee code is required."
            });
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return BadRequest(new
            {
                message = "Employee name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new
            {
                message = "Employee email is required."
            });
        }

        if (string.IsNullOrWhiteSpace(department))
        {
            return BadRequest(new
            {
                message = "Department is required."
            });
        }

        if (string.IsNullOrWhiteSpace(designation))
        {
            return BadRequest(new
            {
                message = "Designation is required."
            });
        }

        var duplicateExists =
            await _database.Employees
                .AnyAsync(employee =>
                    employee.EmployeeCode.ToLower() ==
                        employeeCode.ToLower() ||
                    employee.Email.ToLower() ==
                        email.ToLower()
                );

        if (duplicateExists)
        {
            return Conflict(new
            {
                message =
                    "Employee code or email already exists."
            });
        }

        if (request.ManagerId.HasValue)
        {
            var managerExists =
                await _database.Employees
                    .AnyAsync(employee =>
                        employee.Id ==
                            request.ManagerId.Value &&
                        employee.IsActive
                    );

            if (!managerExists)
            {
                return BadRequest(new
                {
                    message =
                        "The selected manager was not found or is inactive."
                });
            }
        }

        var employee = new Employee
        {
            EmployeeCode = employeeCode,
            FullName = fullName,
            Email = email,
            Department = department,
            Designation = designation,
            ManagerId = request.ManagerId,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _database.Employees.Add(employee);

        await _database.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetEmployee),
            new
            {
                id = employee.Id
            },
            new
            {
                employee.Id
            }
        );
    }

    // =====================================================
    // UPDATE EMPLOYEE
    // Administrator only
    // =====================================================

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult> UpdateEmployee(
        int id,
        Employee request)
    {
        var employee =
            await _database.Employees
                .FindAsync(id);

        if (employee is null)
        {
            return NotFound(new
            {
                message = "Employee was not found."
            });
        }

        var employeeCode =
            request.EmployeeCode?.Trim() ?? string.Empty;

        var fullName =
            request.FullName?.Trim() ?? string.Empty;

        var email =
            request.Email?.Trim() ?? string.Empty;

        var department =
            request.Department?.Trim() ?? string.Empty;

        var designation =
            request.Designation?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            return BadRequest(new
            {
                message = "Employee code is required."
            });
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return BadRequest(new
            {
                message = "Employee name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new
            {
                message = "Employee email is required."
            });
        }

        if (string.IsNullOrWhiteSpace(department))
        {
            return BadRequest(new
            {
                message = "Department is required."
            });
        }

        if (string.IsNullOrWhiteSpace(designation))
        {
            return BadRequest(new
            {
                message = "Designation is required."
            });
        }

        var duplicateExists =
            await _database.Employees
                .AnyAsync(existingEmployee =>
                    existingEmployee.Id != id &&
                    (
                        existingEmployee.EmployeeCode
                            .ToLower() ==
                            employeeCode.ToLower()
                        ||
                        existingEmployee.Email
                            .ToLower() ==
                            email.ToLower()
                    )
                );

        if (duplicateExists)
        {
            return Conflict(new
            {
                message =
                    "Employee code or email already exists."
            });
        }

        // Employee cannot be their own manager.
        if (request.ManagerId.HasValue &&
            request.ManagerId.Value == id)
        {
            return BadRequest(new
            {
                message =
                    "An employee cannot be their own manager."
            });
        }

        if (request.ManagerId.HasValue)
        {
            var managerExists =
                await _database.Employees
                    .AnyAsync(manager =>
                        manager.Id ==
                            request.ManagerId.Value &&
                        manager.IsActive
                    );

            if (!managerExists)
            {
                return BadRequest(new
                {
                    message =
                        "The selected manager was not found or is inactive."
                });
            }

            // Prevent a simple circular relationship:
            // A -> B and B -> A.
            var circularManager =
                await _database.Employees
                    .AnyAsync(manager =>
                        manager.Id ==
                            request.ManagerId.Value &&
                        manager.ManagerId == id
                    );

            if (circularManager)
            {
                return BadRequest(new
                {
                    message =
                        "This manager relationship would create a circular reporting relationship."
                });
            }
        }

        employee.EmployeeCode = employeeCode;
        employee.FullName = fullName;
        employee.Email = email;
        employee.Department = department;
        employee.Designation = designation;
        employee.ManagerId = request.ManagerId;
        employee.IsActive = request.IsActive;

        await _database.SaveChangesAsync();

        return NoContent();
    }

    // =====================================================
    // DELETE EMPLOYEE
    // Administrator only
    // =====================================================

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult> DeleteEmployee(int id)
    {
        var employee =
            await _database.Employees
                .FindAsync(id);

        if (employee is null)
        {
            return NotFound(new
            {
                message = "Employee was not found."
            });
        }

        var hasProjectAssignments =
            await _database.ProjectMembers
                .AnyAsync(member =>
                    member.EmployeeId == id
                );

        var hasTasks =
            await _database.WorkTasks
                .AnyAsync(task =>
                    task.AssignedEmployeeId == id
                );

        var hasWorkLogs =
            await _database.DailyWorkLogs
                .AnyAsync(workLog =>
                    workLog.EmployeeId == id
                );

        var hasTeamMembers =
            await _database.Employees
                .AnyAsync(teamMember =>
                    teamMember.ManagerId == id
                );

        if (hasProjectAssignments ||
            hasTasks ||
            hasWorkLogs ||
            hasTeamMembers)
        {
            return BadRequest(new
            {
                message =
                    "This employee cannot be deleted because related project, task, work-log or team-member records exist. Set the employee as inactive instead."
            });
        }

        _database.Employees.Remove(employee);

        await _database.SaveChangesAsync();

        return NoContent();
    }

    // =====================================================
    // JWT EMPLOYEE ID
    // =====================================================

    private int? GetLoggedInEmployeeId()
    {
        var employeeIdValue =
            User.FindFirstValue("employeeId");

        if (int.TryParse(
                employeeIdValue,
                out var employeeId))
        {
            return employeeId;
        }

        return null;
    }
}