using System.Security.Claims;
using EmployeeTracker.Api.Data;
using EmployeeTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeTracker.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(
    Roles = "Administrator,Manager,Employee"
)]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _database;

    public ReportsController(
        AppDbContext database
    )
    {
        _database = database;
    }

    [HttpGet("work-summary")]
    public async Task<ActionResult> GetWorkSummary(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? employeeId,
        [FromQuery] int? projectId
    )
    {
        var reportFromDate =
            (fromDate ??
             new DateTime(
                 DateTime.Today.Year,
                 DateTime.Today.Month,
                 1
             )).Date;

        var reportToDate =
            (toDate ?? DateTime.Today).Date;

        if (reportToDate < reportFromDate)
        {
            return BadRequest(new
            {
                message =
                    "To date cannot be earlier than from date."
            });
        }

        if ((reportToDate - reportFromDate)
            .TotalDays > 366)
        {
            return BadRequest(new
            {
                message =
                    "The report date range cannot exceed one year."
            });
        }

        if (User.IsInRole("Employee"))
        {
            var linkedEmployeeId =
                GetEmployeeId();

            if (!linkedEmployeeId.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "Your login is not linked to an employee."
                });
            }

            employeeId = linkedEmployeeId.Value;
        }

        var exclusiveToDate =
            reportToDate.AddDays(1);

        IQueryable<DailyWorkLog> query =
            _database.DailyWorkLogs
                .AsNoTracking()
                .Where(workLog =>
                    workLog.WorkDate >=
                        reportFromDate &&
                    workLog.WorkDate <
                        exclusiveToDate
                );

        if (employeeId.HasValue)
        {
            query = query.Where(workLog =>
                workLog.EmployeeId ==
                    employeeId.Value
            );
        }

        if (projectId.HasValue)
        {
            query = query.Where(workLog =>
                workLog.ProjectId ==
                    projectId.Value
            );
        }

        var totalWorkLogs =
            await query.CountAsync();

        var totalHours =
            await query.SumAsync(workLog =>
                (decimal?)workLog.HoursWorked
            ) ?? 0;

        var averageProgress =
            await query.AverageAsync(workLog =>
                (decimal?)
                workLog.ProgressPercentage
            ) ?? 0;

        var draftWorkLogs =
            await query.CountAsync(workLog =>
                workLog.Status == "Draft"
            );

        var submittedWorkLogs =
            await query.CountAsync(workLog =>
                workLog.Status == "Submitted"
            );

        var approvedWorkLogs =
            await query.CountAsync(workLog =>
                workLog.Status == "Approved"
            );

        var rejectedWorkLogs =
            await query.CountAsync(workLog =>
                workLog.Status == "Rejected"
            );

        var employeeSummaries =
            await query
                .GroupBy(workLog => new
                {
                    workLog.EmployeeId,
                    workLog.Employee.EmployeeCode,
                    workLog.Employee.FullName
                })
                .Select(group => new
                {
                    group.Key.EmployeeId,
                    group.Key.EmployeeCode,
                    EmployeeName =
                        group.Key.FullName,

                    TotalWorkLogs =
                        group.Count(),

                    TotalHours =
                        group.Sum(workLog =>
                            workLog.HoursWorked
                        ),

                    AverageProgress =
                        group.Average(workLog =>
                            workLog.ProgressPercentage
                        ),

                    ApprovedWorkLogs =
                        group.Count(workLog =>
                            workLog.Status ==
                            "Approved"
                        )
                })
                .OrderBy(item =>
                    item.EmployeeName
                )
                .ToListAsync();

        var projectSummaries =
            await query
                .GroupBy(workLog => new
                {
                    workLog.ProjectId,
                    workLog.Project.ProjectCode,
                    workLog.Project.ProjectName
                })
                .Select(group => new
                {
                    group.Key.ProjectId,
                    group.Key.ProjectCode,
                    group.Key.ProjectName,

                    TotalWorkLogs =
                        group.Count(),

                    TotalHours =
                        group.Sum(workLog =>
                            workLog.HoursWorked
                        ),

                    AverageProgress =
                        group.Average(workLog =>
                            workLog.ProgressPercentage
                        )
                })
                .OrderBy(item =>
                    item.ProjectName
                )
                .ToListAsync();

        var workLogs =
            await query
                .OrderByDescending(workLog =>
                    workLog.WorkDate
                )
                .ThenBy(workLog =>
                    workLog.Employee.FullName
                )
                .Take(500)
                .Select(workLog => new
                {
                    workLog.Id,
                    workLog.WorkDate,
                    workLog.HoursWorked,
                    workLog.ProgressPercentage,
                    workLog.Status,
                    workLog.WorkDescription,
                    workLog.Blockers,

                    workLog.EmployeeId,

                    EmployeeCode =
                        workLog.Employee.EmployeeCode,

                    EmployeeName =
                        workLog.Employee.FullName,

                    workLog.ProjectId,

                    ProjectCode =
                        workLog.Project.ProjectCode,

                    ProjectName =
                        workLog.Project.ProjectName,

                    workLog.WorkTaskId,

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

        return Ok(new
        {
            fromDate = reportFromDate,
            toDate = reportToDate,
            employeeId,
            projectId,

            totalWorkLogs,
            totalHours,
            averageProgress =
                Math.Round(averageProgress, 2),

            draftWorkLogs,
            submittedWorkLogs,
            approvedWorkLogs,
            rejectedWorkLogs,

            employeeSummaries,
            projectSummaries,
            workLogs
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
}