using System.Security.Claims;
using EmployeeTracker.Api.Data;
using EmployeeTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeTracker.Api.Controllers;

[ApiController]
[Route("api/worktasks")]
[Route("api/work-tasks")]
[Route("api/tasks")]
[Authorize]
public class WorkTasksController : ControllerBase
{
    private readonly AppDbContext _database;

    public WorkTasksController(
        AppDbContext database
    )
    {
        _database = database;
    }

    [HttpGet]
    [Authorize(
        Roles = "Administrator,Manager,Employee"
    )]
    public async Task<ActionResult> GetWorkTasks()
    {
        IQueryable<WorkTask> query =
            _database.WorkTasks
                .AsNoTracking();

        if (User.IsInRole("Employee"))
        {
            var employeeId = GetEmployeeId();

            if (!employeeId.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "Your login is not linked to an employee."
                });
            }

            query = query.Where(workTask =>
                workTask.AssignedEmployeeId ==
                employeeId.Value
            );
        }

        var workTasks = await query
            .OrderByDescending(workTask =>
                workTask.CreatedAt
            )
            .Select(workTask => new
            {
                workTask.Id,
                workTask.TaskCode,
                workTask.TaskTitle,
                workTask.Description,
                workTask.ProjectId,
                workTask.AssignedEmployeeId,
                workTask.StartDate,
                workTask.DueDate,
                workTask.Priority,
                workTask.Status,
                workTask.EstimatedHours,
                workTask.IsActive,
                workTask.CreatedAt,

                ProjectCode =
                    workTask.Project.ProjectCode,

                ProjectName =
                    workTask.Project.ProjectName,

                EmployeeCode =
                    workTask.AssignedEmployee == null
                        ? null
                        : workTask.AssignedEmployee
                            .EmployeeCode,

                EmployeeName =
                    workTask.AssignedEmployee == null
                        ? null
                        : workTask.AssignedEmployee
                            .FullName
            })
            .ToListAsync();

        return Ok(workTasks);
    }

    [HttpGet("{id:int}")]
    [Authorize(
        Roles = "Administrator,Manager,Employee"
    )]
    public async Task<ActionResult> GetWorkTask(
        int id
    )
    {
        IQueryable<WorkTask> query =
            _database.WorkTasks
                .AsNoTracking()
                .Where(workTask =>
                    workTask.Id == id
                );

        if (User.IsInRole("Employee"))
        {
            var employeeId = GetEmployeeId();

            if (!employeeId.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "Your login is not linked to an employee."
                });
            }

            query = query.Where(workTask =>
                workTask.AssignedEmployeeId ==
                employeeId.Value
            );
        }

        var workTask = await query
            .Select(workTask => new
            {
                workTask.Id,
                workTask.TaskCode,
                workTask.TaskTitle,
                workTask.Description,
                workTask.ProjectId,
                workTask.AssignedEmployeeId,
                workTask.StartDate,
                workTask.DueDate,
                workTask.Priority,
                workTask.Status,
                workTask.EstimatedHours,
                workTask.IsActive,
                workTask.CreatedAt,

                ProjectCode =
                    workTask.Project.ProjectCode,

                ProjectName =
                    workTask.Project.ProjectName,

                EmployeeCode =
                    workTask.AssignedEmployee == null
                        ? null
                        : workTask.AssignedEmployee
                            .EmployeeCode,

                EmployeeName =
                    workTask.AssignedEmployee == null
                        ? null
                        : workTask.AssignedEmployee
                            .FullName
            })
            .FirstOrDefaultAsync();

        if (workTask is null)
        {
            return NotFound(new
            {
                message =
                    "Task was not found or you do not have access to it."
            });
        }

        return Ok(workTask);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<ActionResult> CreateWorkTask(
        WorkTask request
    )
    {
        if (string.IsNullOrWhiteSpace(
                request.TaskCode
            ))
        {
            return BadRequest(new
            {
                message = "Task code is required."
            });
        }

        if (string.IsNullOrWhiteSpace(
                request.TaskTitle
            ))
        {
            return BadRequest(new
            {
                message = "Task title is required."
            });
        }

        var taskCode =
            request.TaskCode.Trim();

        var duplicateExists =
            await _database.WorkTasks
                .AnyAsync(workTask =>
                    workTask.TaskCode.ToLower() ==
                    taskCode.ToLower()
                );

        if (duplicateExists)
        {
            return Conflict(new
            {
                message =
                    "Task code already exists."
            });
        }

        var projectExists =
            await _database.Projects
                .AnyAsync(project =>
                    project.Id == request.ProjectId &&
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

        var employeeValidation =
            await ValidateAssignedEmployee(
                request.ProjectId,
                request.AssignedEmployeeId
            );

        if (employeeValidation is not null)
        {
            return employeeValidation;
        }

        if (request.DueDate.HasValue &&
            request.DueDate.Value.Date <
            request.StartDate.Date)
        {
            return BadRequest(new
            {
                message =
                    "Due date cannot be earlier than the start date."
            });
        }

        if (request.EstimatedHours < 0)
        {
            return BadRequest(new
            {
                message =
                    "Estimated hours cannot be negative."
            });
        }

        var workTask = new WorkTask
        {
            TaskCode = taskCode,
            TaskTitle = request.TaskTitle.Trim(),

            Description =
                string.IsNullOrWhiteSpace(
                    request.Description
                )
                    ? null
                    : request.Description.Trim(),

            ProjectId = request.ProjectId,

            AssignedEmployeeId =
                request.AssignedEmployeeId,

            StartDate = request.StartDate.Date,
            DueDate = request.DueDate?.Date,

            Priority =
                request.Priority.Trim(),

            Status =
                request.Status.Trim(),

            EstimatedHours =
                request.EstimatedHours,

            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _database.WorkTasks.Add(workTask);

        await _database.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetWorkTask),
            new { id = workTask.Id },
            workTask
        );
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<ActionResult> UpdateWorkTask(
        int id,
        WorkTask request
    )
    {
        var workTask =
            await _database.WorkTasks
                .FindAsync(id);

        if (workTask is null)
        {
            return NotFound(new
            {
                message =
                    "Task was not found."
            });
        }

        if (string.IsNullOrWhiteSpace(
                request.TaskCode
            ))
        {
            return BadRequest(new
            {
                message = "Task code is required."
            });
        }

        if (string.IsNullOrWhiteSpace(
                request.TaskTitle
            ))
        {
            return BadRequest(new
            {
                message = "Task title is required."
            });
        }

        var taskCode =
            request.TaskCode.Trim();

        var duplicateExists =
            await _database.WorkTasks
                .AnyAsync(existingTask =>
                    existingTask.Id != id &&
                    existingTask.TaskCode.ToLower() ==
                    taskCode.ToLower()
                );

        if (duplicateExists)
        {
            return Conflict(new
            {
                message =
                    "Task code already exists."
            });
        }

        var projectExists =
            await _database.Projects
                .AnyAsync(project =>
                    project.Id == request.ProjectId &&
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

        var employeeValidation =
            await ValidateAssignedEmployee(
                request.ProjectId,
                request.AssignedEmployeeId
            );

        if (employeeValidation is not null)
        {
            return employeeValidation;
        }

        if (request.DueDate.HasValue &&
            request.DueDate.Value.Date <
            request.StartDate.Date)
        {
            return BadRequest(new
            {
                message =
                    "Due date cannot be earlier than the start date."
            });
        }

        if (request.EstimatedHours < 0)
        {
            return BadRequest(new
            {
                message =
                    "Estimated hours cannot be negative."
            });
        }

        workTask.TaskCode = taskCode;

        workTask.TaskTitle =
            request.TaskTitle.Trim();

        workTask.Description =
            string.IsNullOrWhiteSpace(
                request.Description
            )
                ? null
                : request.Description.Trim();

        workTask.ProjectId =
            request.ProjectId;

        workTask.AssignedEmployeeId =
            request.AssignedEmployeeId;

        workTask.StartDate =
            request.StartDate.Date;

        workTask.DueDate =
            request.DueDate?.Date;

        workTask.Priority =
            request.Priority.Trim();

        workTask.Status =
            request.Status.Trim();

        workTask.EstimatedHours =
            request.EstimatedHours;

        workTask.IsActive =
            request.IsActive;

        await _database.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult> DeleteWorkTask(
        int id
    )
    {
        var workTask =
            await _database.WorkTasks
                .FindAsync(id);

        if (workTask is null)
        {
            return NotFound(new
            {
                message =
                    "Task was not found."
            });
        }

        var hasWorkLogs =
            await _database.DailyWorkLogs
                .AnyAsync(workLog =>
                    workLog.WorkTaskId == id
                );

        if (hasWorkLogs)
        {
            return BadRequest(new
            {
                message =
                    "This task has work-log records. Set it as inactive instead of deleting it."
            });
        }

        _database.WorkTasks.Remove(workTask);

        await _database.SaveChangesAsync();

        return NoContent();
    }

    private int? GetEmployeeId()
    {
        var employeeIdValue =
            User.FindFirstValue("employeeId");

        if (int.TryParse(
                employeeIdValue,
                out var employeeId
            ))
        {
            return employeeId;
        }

        return null;
    }

    private async Task<ActionResult?>
        ValidateAssignedEmployee(
            int projectId,
            int? employeeId
        )
    {
        if (!employeeId.HasValue)
        {
            return null;
        }

        var employeeExists =
            await _database.Employees
                .AnyAsync(employee =>
                    employee.Id == employeeId.Value &&
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

        var employeeIsProjectMember =
            await _database.ProjectMembers
                .AnyAsync(member =>
                    member.ProjectId == projectId &&
                    member.EmployeeId ==
                        employeeId.Value &&
                    member.IsActive
                );

        if (!employeeIsProjectMember)
        {
            return BadRequest(new
            {
                message =
                    "The selected employee must be an active member of this project."
            });
        }

        return null;
    }
}