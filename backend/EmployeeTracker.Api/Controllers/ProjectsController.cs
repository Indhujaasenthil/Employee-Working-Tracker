using EmployeeTracker.Api.Data;
using EmployeeTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeTracker.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _database;

    public ProjectsController(
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
    public async Task<
        ActionResult<IEnumerable<Project>>
    > GetProjects()
    {
        var projects =
            await _database.Projects
                .AsNoTracking()
                .OrderBy(project =>
                    project.ProjectName
                )
                .ToListAsync();

        return Ok(projects);
    }

    [HttpGet("{id:int}")]
    [Authorize(
        Roles =
            "Administrator,Manager,Employee"
    )]
    public async Task<
        ActionResult<Project>
    > GetProject(int id)
    {
        var project =
            await _database.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    project =>
                        project.Id == id
                );

        if (project is null)
        {
            return NotFound(new
            {
                message =
                    "Project was not found."
            });
        }

        return Ok(project);
    }

    [HttpPost]
    [Authorize(
        Roles =
            "Administrator,Manager"
    )]
    public async Task<
        ActionResult<Project>
    > CreateProject(Project request)
    {
        var projectCode =
            request.ProjectCode.Trim();

        var projectName =
            request.ProjectName.Trim();

        var duplicateExists =
            await _database.Projects
                .AnyAsync(project =>
                    project.ProjectCode
                        .ToLower() ==
                    projectCode.ToLower()
                );

        if (duplicateExists)
        {
            return Conflict(new
            {
                message =
                    "Project code already exists."
            });
        }

        if (
            request.Deadline.HasValue &&
            request.Deadline.Value.Date <
            request.StartDate.Date
        )
        {
            return BadRequest(new
            {
                message =
                    "Deadline cannot be earlier than the start date."
            });
        }

        var project = new Project
        {
            ProjectCode = projectCode,
            ProjectName = projectName,

            ClientName =
                request.ClientName.Trim(),

            StartDate =
                request.StartDate.Date,

            Deadline =
                request.Deadline?.Date,

            Status = request.Status.Trim(),

            Description =
                string.IsNullOrWhiteSpace(
                    request.Description
                )
                    ? null
                    : request.Description.Trim(),

            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _database.Projects.Add(project);

        await _database.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetProject),
            new { id = project.Id },
            project
        );
    }

    [HttpPut("{id:int}")]
    [Authorize(
        Roles =
            "Administrator,Manager"
    )]
    public async Task<ActionResult>
        UpdateProject(
            int id,
            Project request
        )
    {
        var project =
            await _database.Projects
                .FindAsync(id);

        if (project is null)
        {
            return NotFound(new
            {
                message =
                    "Project was not found."
            });
        }

        var projectCode =
            request.ProjectCode.Trim();

        var duplicateExists =
            await _database.Projects
                .AnyAsync(existingProject =>
                    existingProject.Id != id &&
                    existingProject.ProjectCode
                        .ToLower() ==
                    projectCode.ToLower()
                );

        if (duplicateExists)
        {
            return Conflict(new
            {
                message =
                    "Project code already exists."
            });
        }

        if (
            request.Deadline.HasValue &&
            request.Deadline.Value.Date <
            request.StartDate.Date
        )
        {
            return BadRequest(new
            {
                message =
                    "Deadline cannot be earlier than the start date."
            });
        }

        project.ProjectCode =
            projectCode;

        project.ProjectName =
            request.ProjectName.Trim();

        project.ClientName =
            request.ClientName.Trim();

        project.StartDate =
            request.StartDate.Date;

        project.Deadline =
            request.Deadline?.Date;

        project.Status =
            request.Status.Trim();

        project.Description =
            string.IsNullOrWhiteSpace(
                request.Description
            )
                ? null
                : request.Description.Trim();

        project.IsActive =
            request.IsActive;

        await _database.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult>
        DeleteProject(int id)
    {
        var project =
            await _database.Projects
                .FindAsync(id);

        if (project is null)
        {
            return NotFound(new
            {
                message =
                    "Project was not found."
            });
        }

        var hasMembers =
            await _database.ProjectMembers
                .AnyAsync(member =>
                    member.ProjectId == id
                );

        var hasTasks =
            await _database.WorkTasks
                .AnyAsync(task =>
                    task.ProjectId == id
                );

        var hasWorkLogs =
            await _database.DailyWorkLogs
                .AnyAsync(workLog =>
                    workLog.ProjectId == id
                );

        if (
            hasMembers ||
            hasTasks ||
            hasWorkLogs
        )
        {
            return BadRequest(new
            {
                message =
                    "This project has team, task or work-log records. Set the project as inactive instead of deleting it."
            });
        }

        _database.Projects.Remove(project);

        await _database.SaveChangesAsync();

        return NoContent();
    }
}