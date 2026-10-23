using System.Security.Claims;
using EmployeeTracker.Api.Data;
using EmployeeTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeTracker.Api.Controllers;

[ApiController]
[Route("api/projectmembers")]
[Authorize]
public class ProjectMembersController : ControllerBase
{
    private readonly AppDbContext _database;

    public ProjectMembersController(
        AppDbContext database
    )
    {
        _database = database;
    }

    // =========================================================
    // GET ALL PROJECT MEMBERS
    //
    // Administrator -> all assignments
    // Manager       -> direct reports only
    // Employee      -> own assignments only
    // =========================================================

    [HttpGet]
    [Authorize(
        Roles = "Administrator,Manager,Employee"
    )]
    public async Task<ActionResult>
        GetProjectMembers(
            [FromQuery] int? projectId = null
        )
    {
        var role =
            User.FindFirstValue(
                ClaimTypes.Role
            );

        var query =
            _database.ProjectMembers
                .AsNoTracking()
                .AsQueryable();

        if (projectId.HasValue)
        {
            query =
                query.Where(member =>
                    member.ProjectId ==
                    projectId.Value
                );
        }

        if (string.Equals(
                role,
                "Manager",
                StringComparison.OrdinalIgnoreCase
            ))
        {
            var employeeId =
                GetLoggedInEmployeeId();

            if (!employeeId.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "The manager account is not linked to an employee."
                });
            }

            query =
                query.Where(member =>
                    member.Employee.ManagerId ==
                    employeeId.Value
                );
        }
        else if (string.Equals(
                     role,
                     "Employee",
                     StringComparison.OrdinalIgnoreCase
                 ))
        {
            var employeeId =
                GetLoggedInEmployeeId();

            if (!employeeId.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "The employee account is not linked to an employee."
                });
            }

            query =
                query.Where(member =>
                    member.EmployeeId ==
                    employeeId.Value
                );
        }

        var members =
            await query
                .OrderBy(member =>
                    member.Project.ProjectName
                )
                .ThenBy(member =>
                    member.Employee.FullName
                )
                .Select(member => new
                {
                    member.Id,
                    member.ProjectId,
                    member.EmployeeId,
                    member.ProjectRole,
                    member.AssignedDate,
                    member.IsActive,

                    ProjectCode =
                        member.Project.ProjectCode,

                    ProjectName =
                        member.Project.ProjectName,

                    EmployeeCode =
                        member.Employee.EmployeeCode,

                    EmployeeName =
                        member.Employee.FullName,

                    Department =
                        member.Employee.Department,

                    Designation =
                        member.Employee.Designation
                })
                .ToListAsync();

        return Ok(members);
    }

    // =========================================================
    // GET ONE PROJECT MEMBER
    // =========================================================

    [HttpGet("{id:int}")]
    [Authorize(
        Roles = "Administrator,Manager,Employee"
    )]
    public async Task<ActionResult>
        GetProjectMember(int id)
    {
        var member =
            await _database.ProjectMembers
                .AsNoTracking()
                .Where(item =>
                    item.Id == id
                )
                .Select(item => new
                {
                    item.Id,
                    item.ProjectId,
                    item.EmployeeId,
                    item.ProjectRole,
                    item.AssignedDate,
                    item.IsActive,

                    ProjectCode =
                        item.Project.ProjectCode,

                    ProjectName =
                        item.Project.ProjectName,

                    EmployeeCode =
                        item.Employee.EmployeeCode,

                    EmployeeName =
                        item.Employee.FullName,

                    Department =
                        item.Employee.Department,

                    Designation =
                        item.Employee.Designation,

                    ManagerId =
                        item.Employee.ManagerId
                })
                .FirstOrDefaultAsync();

        if (member is null)
        {
            return NotFound(new
            {
                message =
                    "Project assignment was not found."
            });
        }

        var role =
            User.FindFirstValue(
                ClaimTypes.Role
            );

        if (string.Equals(
                role,
                "Manager",
                StringComparison.OrdinalIgnoreCase
            ))
        {
            var managerEmployeeId =
                GetLoggedInEmployeeId();

            if (!managerEmployeeId.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "The manager account is not linked to an employee."
                });
            }

            if (member.ManagerId !=
                managerEmployeeId.Value)
            {
                return Forbid();
            }
        }
        else if (string.Equals(
                     role,
                     "Employee",
                     StringComparison.OrdinalIgnoreCase
                 ))
        {
            var employeeId =
                GetLoggedInEmployeeId();

            if (!employeeId.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "The employee account is not linked to an employee."
                });
            }

            if (member.EmployeeId !=
                employeeId.Value)
            {
                return Forbid();
            }
        }

        return Ok(member);
    }

    // =========================================================
    // GET EMPLOYEES AVAILABLE FOR PROJECT ASSIGNMENT
    //
    // Administrator -> all active employees
    // Manager       -> active direct reports only
    // Employee      -> not allowed
    // =========================================================

    [HttpGet("available-employees")]
    [Authorize(
        Roles = "Administrator,Manager"
    )]
    public async Task<ActionResult>
        GetAvailableEmployees()
    {
        var role =
            User.FindFirstValue(
                ClaimTypes.Role
            );

        var query =
            _database.Employees
                .AsNoTracking()
                .Where(employee =>
                    employee.IsActive
                )
                .AsQueryable();

        if (string.Equals(
                role,
                "Manager",
                StringComparison.OrdinalIgnoreCase
            ))
        {
            var managerEmployeeId =
                GetLoggedInEmployeeId();

            if (!managerEmployeeId.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "The manager account is not linked to an employee."
                });
            }

            query =
                query.Where(employee =>
                    employee.ManagerId ==
                    managerEmployeeId.Value
                );
        }

        var employees =
            await query
                .OrderBy(employee =>
                    employee.FullName
                )
                .Select(employee => new
                {
                    employee.Id,
                    employee.EmployeeCode,
                    employee.FullName,
                    employee.Email,
                    employee.Department,
                    employee.Designation,
                    employee.ManagerId,
                    employee.IsActive
                })
                .ToListAsync();

        return Ok(employees);
    }

    // =========================================================
    // CREATE
    // =========================================================

    [HttpPost]
    [Authorize(
        Roles = "Administrator,Manager"
    )]
    public async Task<ActionResult>
        CreateProjectMember(
            ProjectMemberRequest request
        )
    {
        var authorizationResult =
            await CanManageEmployee(
                request.EmployeeId
            );

        if (!authorizationResult.Allowed)
        {
            return StatusCode(
                authorizationResult.StatusCode,
                new
                {
                    message =
                        authorizationResult.Message
                }
            );
        }

        var project =
            await _database.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(project =>
                    project.Id ==
                    request.ProjectId
                );

        if (project is null)
        {
            return BadRequest(new
            {
                message =
                    "The selected project does not exist."
            });
        }

        if (!project.IsActive)
        {
            return BadRequest(new
            {
                message =
                    "The selected project is inactive."
            });
        }

        var employee =
            await _database.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(employee =>
                    employee.Id ==
                    request.EmployeeId
                );

        if (employee is null)
        {
            return BadRequest(new
            {
                message =
                    "The selected employee does not exist."
            });
        }

        if (!employee.IsActive)
        {
            return BadRequest(new
            {
                message =
                    "The selected employee is inactive."
            });
        }

        var projectRole =
            request.ProjectRole?.Trim() ??
            string.Empty;

        if (string.IsNullOrWhiteSpace(
                projectRole
            ))
        {
            return BadRequest(new
            {
                message =
                    "Project role is required."
            });
        }

        var duplicateExists =
            await _database.ProjectMembers
                .AnyAsync(member =>
                    member.ProjectId ==
                        request.ProjectId &&
                    member.EmployeeId ==
                        request.EmployeeId
                );

        if (duplicateExists)
        {
            return Conflict(new
            {
                message =
                    "This employee is already assigned to the selected project."
            });
        }

        var member =
            new ProjectMember
            {
                ProjectId =
                    request.ProjectId,

                EmployeeId =
                    request.EmployeeId,

                ProjectRole =
                    projectRole,

                AssignedDate =
                    request.AssignedDate.Date,

                IsActive =
                    request.IsActive
            };

        _database.ProjectMembers.Add(
            member
        );

        await _database.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetProjectMember),
            new
            {
                id = member.Id
            },
            new
            {
                member.Id,
                member.ProjectId,
                member.EmployeeId,
                member.ProjectRole,
                member.AssignedDate,
                member.IsActive,

                project.ProjectCode,
                project.ProjectName,

                employee.EmployeeCode,

                EmployeeName =
                    employee.FullName,

                employee.Department,
                employee.Designation
            }
        );
    }

    // =========================================================
    // UPDATE
    // =========================================================

    [HttpPut("{id:int}")]
    [Authorize(
        Roles = "Administrator,Manager"
    )]
    public async Task<ActionResult>
        UpdateProjectMember(
            int id,
            ProjectMemberRequest request
        )
    {
        var member =
            await _database.ProjectMembers
                .FirstOrDefaultAsync(item =>
                    item.Id == id
                );

        if (member is null)
        {
            return NotFound(new
            {
                message =
                    "Project assignment was not found."
            });
        }

        // Manager must be allowed to manage
        // the existing assignment.
        var existingAuthorization =
            await CanManageEmployee(
                member.EmployeeId
            );

        if (!existingAuthorization.Allowed)
        {
            return StatusCode(
                existingAuthorization.StatusCode,
                new
                {
                    message =
                        existingAuthorization.Message
                }
            );
        }

        // Manager must also be allowed to
        // select the new employee.
        var requestedAuthorization =
            await CanManageEmployee(
                request.EmployeeId
            );

        if (!requestedAuthorization.Allowed)
        {
            return StatusCode(
                requestedAuthorization.StatusCode,
                new
                {
                    message =
                        requestedAuthorization.Message
                }
            );
        }

        var projectExists =
            await _database.Projects
                .AnyAsync(project =>
                    project.Id ==
                        request.ProjectId &&
                    project.IsActive
                );

        if (!projectExists)
        {
            return BadRequest(new
            {
                message =
                    "Select a valid active project."
            });
        }

        var employeeExists =
            await _database.Employees
                .AnyAsync(employee =>
                    employee.Id ==
                        request.EmployeeId &&
                    employee.IsActive
                );

        if (!employeeExists)
        {
            return BadRequest(new
            {
                message =
                    "Select a valid active employee."
            });
        }

        var projectRole =
            request.ProjectRole?.Trim() ??
            string.Empty;

        if (string.IsNullOrWhiteSpace(
                projectRole
            ))
        {
            return BadRequest(new
            {
                message =
                    "Project role is required."
            });
        }

        var duplicateExists =
            await _database.ProjectMembers
                .AnyAsync(existingMember =>
                    existingMember.Id != id &&
                    existingMember.ProjectId ==
                        request.ProjectId &&
                    existingMember.EmployeeId ==
                        request.EmployeeId
                );

        if (duplicateExists)
        {
            return Conflict(new
            {
                message =
                    "This employee is already assigned to the selected project."
            });
        }

        member.ProjectId =
            request.ProjectId;

        member.EmployeeId =
            request.EmployeeId;

        member.ProjectRole =
            projectRole;

        member.AssignedDate =
            request.AssignedDate.Date;

        member.IsActive =
            request.IsActive;

        await _database.SaveChangesAsync();

        return NoContent();
    }

    // =========================================================
    // DELETE
    // =========================================================

    [HttpDelete("{id:int}")]
    [Authorize(
        Roles = "Administrator,Manager"
    )]
    public async Task<ActionResult>
        DeleteProjectMember(int id)
    {
        var member =
            await _database.ProjectMembers
                .FirstOrDefaultAsync(item =>
                    item.Id == id
                );

        if (member is null)
        {
            return NotFound(new
            {
                message =
                    "Project assignment was not found."
            });
        }

        var authorizationResult =
            await CanManageEmployee(
                member.EmployeeId
            );

        if (!authorizationResult.Allowed)
        {
            return StatusCode(
                authorizationResult.StatusCode,
                new
                {
                    message =
                        authorizationResult.Message
                }
            );
        }

        _database.ProjectMembers.Remove(
            member
        );

        await _database.SaveChangesAsync();

        return NoContent();
    }

    // =========================================================
    // AUTHORIZATION HELPERS
    // =========================================================

    private int? GetLoggedInEmployeeId()
    {
        var employeeIdValue =
            User.FindFirstValue(
                "employeeId"
            );

        if (int.TryParse(
                employeeIdValue,
                out var employeeId
            ))
        {
            return employeeId;
        }

        return null;
    }

    private async Task<(
        bool Allowed,
        int StatusCode,
        string Message
    )> CanManageEmployee(
        int employeeId
    )
    {
        if (User.IsInRole(
                "Administrator"
            ))
        {
            return (
                true,
                StatusCodes.Status200OK,
                string.Empty
            );
        }

        if (!User.IsInRole(
                "Manager"
            ))
        {
            return (
                false,
                StatusCodes.Status403Forbidden,
                "You are not allowed to manage project assignments."
            );
        }

        var managerEmployeeId =
            GetLoggedInEmployeeId();

        if (!managerEmployeeId.HasValue)
        {
            return (
                false,
                StatusCodes.Status401Unauthorized,
                "The manager account is not linked to an employee."
            );
        }

        var isDirectReport =
            await _database.Employees
                .AsNoTracking()
                .AnyAsync(employee =>
                    employee.Id ==
                        employeeId &&
                    employee.ManagerId ==
                        managerEmployeeId.Value
                );

        if (!isDirectReport)
        {
            return (
                false,
                StatusCodes.Status403Forbidden,
                "Managers can manage project assignments only for their direct-report employees."
            );
        }

        return (
            true,
            StatusCodes.Status200OK,
            string.Empty
        );
    }
}