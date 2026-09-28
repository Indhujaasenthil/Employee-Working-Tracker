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

    [HttpGet]
    [Authorize(
        Roles =
            "Administrator,Manager,Employee"
    )]
    public async Task<ActionResult>
        GetProjectMembers()
    {
        var members =
            await _database.ProjectMembers
                .AsNoTracking()
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
                        member.Employee.FullName
                })
                .ToListAsync();

        return Ok(members);
    }

    [HttpGet("{id:int}")]
    [Authorize(
        Roles =
            "Administrator,Manager,Employee"
    )]
    public async Task<ActionResult>
        GetProjectMember(int id)
    {
        var member =
            await _database.ProjectMembers
                .AsNoTracking()
                .Where(member =>
                    member.Id == id
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
                        member.Employee.FullName
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

        return Ok(member);
    }

    [HttpPost]
    [Authorize(
        Roles =
            "Administrator,Manager"
    )]
    public async Task<ActionResult>
        CreateProjectMember(
            ProjectMember request
        )
    {
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

        var member = new ProjectMember
        {
            ProjectId = request.ProjectId,
            EmployeeId = request.EmployeeId,

            ProjectRole =
                request.ProjectRole.Trim(),

            AssignedDate =
                request.AssignedDate.Date,

            IsActive = request.IsActive
        };

        _database.ProjectMembers.Add(member);

        await _database.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetProjectMember),
            new { id = member.Id },
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
                    employee.FullName
            }
        );
    }

    [HttpPut("{id:int}")]
    [Authorize(
        Roles =
            "Administrator,Manager"
    )]
    public async Task<ActionResult>
        UpdateProjectMember(
            int id,
            ProjectMember request
        )
    {
        var member =
            await _database.ProjectMembers
                .FindAsync(id);

        if (member is null)
        {
            return NotFound(new
            {
                message =
                    "Project assignment was not found."
            });
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
            request.ProjectRole.Trim();

        member.AssignedDate =
            request.AssignedDate.Date;

        member.IsActive =
            request.IsActive;

        await _database.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(
        Roles =
            "Administrator,Manager"
    )]
    public async Task<ActionResult>
        DeleteProjectMember(int id)
    {
        var member =
            await _database.ProjectMembers
                .FindAsync(id);

        if (member is null)
        {
            return NotFound(new
            {
                message =
                    "Project assignment was not found."
            });
        }

        _database.ProjectMembers.Remove(member);

        await _database.SaveChangesAsync();

        return NoContent();
    }
}