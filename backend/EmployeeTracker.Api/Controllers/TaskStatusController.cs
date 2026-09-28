using System.Security.Claims;
using EmployeeTracker.Api.Data;
using EmployeeTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeTracker.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Route("api/worktasks")]
[Route("api/work-tasks")]
[Authorize(
    Roles = "Administrator,Manager,Employee"
)]
public class TaskStatusController : ControllerBase
{
    private static readonly string[] AllowedStatuses =
    {
        "Not Started",
        "In Progress",
        "On Hold",
        "Completed"
    };

    private readonly AppDbContext _database;

    public TaskStatusController(
        AppDbContext database
    )
    {
        _database = database;
    }

    [HttpPut("{id:int}/status")]
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult> UpdateStatus(
        int id,
        TaskStatusRequest request
    )
    {
        if (string.IsNullOrWhiteSpace(
                request.Status
            ))
        {
            return BadRequest(new
            {
                message =
                    "Task status is required."
            });
        }

        var normalizedStatus =
            NormalizeStatus(request.Status);

        if (normalizedStatus is null)
        {
            return BadRequest(new
            {
                message =
                    "Status must be Not Started, In Progress, On Hold or Completed."
            });
        }

        var workTask =
            await _database.WorkTasks
                .FirstOrDefaultAsync(task =>
                    task.Id == id
                );

        if (workTask is null)
        {
            return NotFound(new
            {
                message =
                    "Task was not found."
            });
        }

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

            if (workTask.AssignedEmployeeId !=
                employeeId.Value)
            {
                return Forbid();
            }

            if (!workTask.IsActive)
            {
                return BadRequest(new
                {
                    message =
                        "An inactive task cannot be updated."
                });
            }
        }

        workTask.Status = normalizedStatus;

        await _database.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Task status updated successfully.",

            workTask.Id,
            workTask.Status
        });
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

    private static string? NormalizeStatus(
        string status
    )
    {
        var matchingStatus =
            AllowedStatuses.FirstOrDefault(
                allowedStatus =>
                    allowedStatus.Equals(
                        status.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    )
            );

        return matchingStatus;
    }
}