using System.Security.Claims;
using EmployeeTracker.Api.Data;
using EmployeeTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeTracker.Api.Controllers;

[ApiController]
[Route("api/dailyworklogs")]
[Route("api/daily-work-logs")]
[Route("api/worklogs")]
[Authorize]
public class DailyWorkLogsController :
    ControllerBase
{
    private readonly AppDbContext _database;

    public DailyWorkLogsController(
        AppDbContext database
    )
    {
        _database = database;
    }

    [HttpGet]
    [Authorize(
        Roles = "Administrator,Manager,Employee"
    )]
    public async Task<ActionResult>
        GetDailyWorkLogs()
    {
        IQueryable<DailyWorkLog> query =
            _database.DailyWorkLogs
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

            query = query.Where(workLog =>
                workLog.EmployeeId ==
                employeeId.Value
            );
        }
        else if (User.IsInRole("Manager"))
        {
            var managerId = GetEmployeeId();

            if (!managerId.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "Your manager login is not linked to an employee."
                });
            }

            query = query.Where(workLog =>
                workLog.Employee.ManagerId ==
                managerId.Value
            );
        }

        var workLogs = await query
            .OrderByDescending(workLog =>
                workLog.WorkDate
            )
            .ThenBy(workLog =>
                workLog.Employee.FullName
            )
            .Select(workLog => new
            {
                workLog.Id,
                workLog.EmployeeId,
                workLog.ProjectId,
                workLog.WorkTaskId,
                workLog.WorkDate,
                workLog.HoursWorked,
                workLog.ProgressPercentage,
                workLog.Status,
                workLog.WorkDescription,
                workLog.Blockers,
                workLog.CreatedAt,

                EmployeeCode =
                    workLog.Employee.EmployeeCode,

                EmployeeName =
                    workLog.Employee.FullName,

                ProjectCode =
                    workLog.Project.ProjectCode,

                ProjectName =
                    workLog.Project.ProjectName,

                TaskCode =
                    workLog.WorkTask == null
                        ? null
                        : workLog.WorkTask.TaskCode,

                TaskTitle =
                    workLog.WorkTask == null
                        ? null
                        : workLog.WorkTask.TaskTitle
            })
            .ToListAsync();

        return Ok(workLogs);
    }

    [HttpGet("{id:int}")]
    [Authorize(
        Roles = "Administrator,Manager,Employee"
    )]
    public async Task<ActionResult>
        GetDailyWorkLog(
            int id
        )
    {
        IQueryable<DailyWorkLog> query =
            _database.DailyWorkLogs
                .AsNoTracking()
                .Where(workLog =>
                    workLog.Id == id
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

            query = query.Where(workLog =>
                workLog.EmployeeId ==
                employeeId.Value
            );
        }
        else if (User.IsInRole("Manager"))
        {
            var managerId = GetEmployeeId();

            if (!managerId.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "Your manager login is not linked to an employee."
                });
            }

            query = query.Where(workLog =>
                workLog.Employee.ManagerId ==
                managerId.Value
            );
        }

        var workLog = await query
            .Select(workLog => new
            {
                workLog.Id,
                workLog.EmployeeId,
                workLog.ProjectId,
                workLog.WorkTaskId,
                workLog.WorkDate,
                workLog.HoursWorked,
                workLog.ProgressPercentage,
                workLog.Status,
                workLog.WorkDescription,
                workLog.Blockers,
                workLog.CreatedAt,

                EmployeeCode =
                    workLog.Employee.EmployeeCode,

                EmployeeName =
                    workLog.Employee.FullName,

                ProjectCode =
                    workLog.Project.ProjectCode,

                ProjectName =
                    workLog.Project.ProjectName,

                TaskCode =
                    workLog.WorkTask == null
                        ? null
                        : workLog.WorkTask.TaskCode,

                TaskTitle =
                    workLog.WorkTask == null
                        ? null
                        : workLog.WorkTask.TaskTitle
            })
            .FirstOrDefaultAsync();

        if (workLog is null)
        {
            return NotFound(new
            {
                message =
                    "Daily work log was not found or you do not have access to it."
            });
        }

        return Ok(workLog);
    }

    [HttpPost]
    [Authorize(
        Roles = "Administrator,Manager,Employee"
    )]
    public async Task<ActionResult>
        CreateDailyWorkLog(
            DailyWorkLog request
        )
    {
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

            request.EmployeeId =
                employeeId.Value;
        }
        else if (User.IsInRole("Manager"))
        {
            var canAccessEmployee =
                await CanManagerAccessEmployee(
                    request.EmployeeId
                );

            if (!canAccessEmployee)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        message =
                            "You can create work logs only for employees who report to you."
                    }
                );
            }
        }

        var validationResult =
            await ValidateWorkLog(request);

        if (validationResult is not null)
        {
            return validationResult;
        }

        var duplicateExists =
            await _database.DailyWorkLogs
                .AnyAsync(workLog =>
                    workLog.EmployeeId ==
                        request.EmployeeId &&
                    workLog.WorkTaskId ==
                        request.WorkTaskId &&
                    workLog.WorkDate.Date ==
                        request.WorkDate.Date
                );

        if (duplicateExists)
        {
            return Conflict(new
            {
                message =
                    "A work log already exists for this employee, task and date."
            });
        }

        var workLog = new DailyWorkLog
        {
            EmployeeId =
                request.EmployeeId,

            ProjectId =
                request.ProjectId,

            WorkTaskId =
                request.WorkTaskId,

            WorkDate =
                request.WorkDate.Date,

            HoursWorked =
                request.HoursWorked,

            ProgressPercentage =
                request.ProgressPercentage,

            Status =
                NormalizeEditableStatus(
                    request.Status
                ),

            WorkDescription =
                request.WorkDescription.Trim(),

            Blockers =
                string.IsNullOrWhiteSpace(
                    request.Blockers
                )
                    ? null
                    : request.Blockers.Trim(),

            CreatedAt =
                DateTime.UtcNow
        };

        _database.DailyWorkLogs.Add(workLog);

        await _database.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetDailyWorkLog),
            new { id = workLog.Id },
            workLog
        );
    }

    [HttpPut("{id:int}")]
    [Authorize(
        Roles = "Administrator,Manager,Employee"
    )]
    public async Task<ActionResult>
        UpdateDailyWorkLog(
            int id,
            DailyWorkLog request
        )
    {
        var workLog =
            await _database.DailyWorkLogs
                .FindAsync(id);

        if (workLog is null)
        {
            return NotFound(new
            {
                message =
                    "Daily work log was not found."
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

            if (workLog.EmployeeId !=
                employeeId.Value)
            {
                return Forbid();
            }

            if (!CanEditOrDelete(workLog.Status))
            {
                return BadRequest(new
                {
                    message =
                        "Only Draft or Rejected work logs can be edited."
                });
            }

            request.EmployeeId =
                employeeId.Value;
        }
        else if (User.IsInRole("Manager"))
        {
            var canAccessExistingEmployee =
                await CanManagerAccessEmployee(
                    workLog.EmployeeId
                );

            var canAccessRequestedEmployee =
                await CanManagerAccessEmployee(
                    request.EmployeeId
                );

            if (!canAccessExistingEmployee ||
                !canAccessRequestedEmployee)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        message =
                            "You can update work logs only for employees who report to you."
                    }
                );
            }
        }

        var validationResult =
            await ValidateWorkLog(request);

        if (validationResult is not null)
        {
            return validationResult;
        }

        var duplicateExists =
            await _database.DailyWorkLogs
                .AnyAsync(existingWorkLog =>
                    existingWorkLog.Id != id &&
                    existingWorkLog.EmployeeId ==
                        request.EmployeeId &&
                    existingWorkLog.WorkTaskId ==
                        request.WorkTaskId &&
                    existingWorkLog.WorkDate.Date ==
                        request.WorkDate.Date
                );

        if (duplicateExists)
        {
            return Conflict(new
            {
                message =
                    "A work log already exists for this employee, task and date."
            });
        }

        workLog.EmployeeId =
            request.EmployeeId;

        workLog.ProjectId =
            request.ProjectId;

        workLog.WorkTaskId =
            request.WorkTaskId;

        workLog.WorkDate =
            request.WorkDate.Date;

        workLog.HoursWorked =
            request.HoursWorked;

        workLog.ProgressPercentage =
            request.ProgressPercentage;

        workLog.Status =
            NormalizeEditableStatus(
                request.Status
            );

        workLog.WorkDescription =
            request.WorkDescription.Trim();

        workLog.Blockers =
            string.IsNullOrWhiteSpace(
                request.Blockers
            )
                ? null
                : request.Blockers.Trim();

        await _database.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id:int}/status")]
    [HttpPatch("{id:int}/status")]
    [Authorize(
        Roles = "Administrator,Manager"
    )]
    public async Task<ActionResult> UpdateStatus(
        int id,
        Dictionary<string, string> request
    )
    {
        var workLog =
            await _database.DailyWorkLogs
                .FirstOrDefaultAsync(item =>
                    item.Id == id
                );

        if (workLog is null)
        {
            return NotFound(new
            {
                message =
                    "Daily work log was not found."
            });
        }

        if (User.IsInRole("Manager"))
        {
            var canAccessEmployee =
                await CanManagerAccessEmployee(
                    workLog.EmployeeId
                );

            if (!canAccessEmployee)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        message =
                            "You can approve or reject work logs only for employees who report to you."
                    }
                );
            }
        }

        if (!request.TryGetValue(
                "status",
                out var requestedStatus
            ))
        {
            return BadRequest(new
            {
                message =
                    "Status is required."
            });
        }

        var status =
            NormalizeStatus(requestedStatus);

        if (status != "Approved" &&
            status != "Rejected")
        {
            return BadRequest(new
            {
                message =
                    "Approval status must be Approved or Rejected."
            });
        }

        if (!string.Equals(
                workLog.Status,
                "Submitted",
                StringComparison.OrdinalIgnoreCase
            ))
        {
            return BadRequest(new
            {
                message =
                    "Only submitted work logs can be approved or rejected."
            });
        }

        workLog.Status = status;

        await _database.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(
        Roles = "Administrator,Manager,Employee"
    )]
    public async Task<ActionResult>
        DeleteDailyWorkLog(
            int id
        )
    {
        var workLog =
            await _database.DailyWorkLogs
                .FindAsync(id);

        if (workLog is null)
        {
            return NotFound(new
            {
                message =
                    "Daily work log was not found."
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

            if (workLog.EmployeeId !=
                employeeId.Value)
            {
                return Forbid();
            }

            if (!CanEditOrDelete(workLog.Status))
            {
                return BadRequest(new
                {
                    message =
                        "Only Draft or Rejected work logs can be deleted."
                });
            }
        }
        else if (User.IsInRole("Manager"))
        {
            var canAccessEmployee =
                await CanManagerAccessEmployee(
                    workLog.EmployeeId
                );

            if (!canAccessEmployee)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new
                    {
                        message =
                            "You can delete work logs only for employees who report to you."
                    }
                );
            }
        }

        _database.DailyWorkLogs.Remove(workLog);

        await _database.SaveChangesAsync();

        return NoContent();
    }

    private async Task<ActionResult?>
        ValidateWorkLog(
            DailyWorkLog request
        )
    {
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

        var task =
            await _database.WorkTasks
                .AsNoTracking()
                .FirstOrDefaultAsync(workTask =>
                    workTask.Id ==
                        request.WorkTaskId &&
                    workTask.IsActive
                );

        if (task is null)
        {
            return BadRequest(new
            {
                message =
                    "Select a valid active task."
            });
        }

        if (task.ProjectId !=
            request.ProjectId)
        {
            return BadRequest(new
            {
                message =
                    "The selected task does not belong to the selected project."
            });
        }

        if (User.IsInRole("Employee"))
        {
            if (!task.AssignedEmployeeId.HasValue ||
                task.AssignedEmployeeId.Value !=
                    request.EmployeeId)
            {
                return BadRequest(new
                {
                    message =
                        "You can create a work log only for a task assigned to you."
                });
            }
        }
        else if (
            task.AssignedEmployeeId.HasValue &&
            task.AssignedEmployeeId.Value !=
                request.EmployeeId
        )
        {
            return BadRequest(new
            {
                message =
                    "The selected task is assigned to a different employee."
            });
        }

        if (request.HoursWorked <= 0 ||
            request.HoursWorked > 24)
        {
            return BadRequest(new
            {
                message =
                    "Hours worked must be greater than 0 and cannot exceed 24."
            });
        }

        if (request.ProgressPercentage < 0 ||
            request.ProgressPercentage > 100)
        {
            return BadRequest(new
            {
                message =
                    "Progress percentage must be between 0 and 100."
            });
        }

        if (string.IsNullOrWhiteSpace(
                request.WorkDescription
            ))
        {
            return BadRequest(new
            {
                message =
                    "Work description is required."
            });
        }

        return null;
    }

    private async Task<bool>
        CanManagerAccessEmployee(
            int employeeId
        )
    {
        var managerId = GetEmployeeId();

        if (!managerId.HasValue)
        {
            return false;
        }

        return await _database.Employees
            .AsNoTracking()
            .AnyAsync(employee =>
                employee.Id == employeeId &&
                employee.ManagerId ==
                    managerId.Value
            );
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

    private static bool CanEditOrDelete(
        string status
    )
    {
        return
            string.Equals(
                status,
                "Draft",
                StringComparison.OrdinalIgnoreCase
            ) ||
            string.Equals(
                status,
                "Rejected",
                StringComparison.OrdinalIgnoreCase
            );
    }

    private static string
        NormalizeEditableStatus(
            string? status
        )
    {
        return string.Equals(
            status,
            "Submitted",
            StringComparison.OrdinalIgnoreCase
        )
            ? "Submitted"
            : "Draft";
    }

    private static string NormalizeStatus(
        string? status
    )
    {
        return status?
            .Trim()
            .ToLowerInvariant()
            switch
            {
                "submitted" => "Submitted",
                "approved" => "Approved",
                "rejected" => "Rejected",
                _ => "Draft"
            };
    }
}