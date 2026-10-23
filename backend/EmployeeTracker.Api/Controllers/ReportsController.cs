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

    /*
     * Returns employees that the logged-in user
     * is allowed to use in the Reports filter.
     *
     * Administrator:
     *     All active employees.
     *
     * Manager:
     *     Only active direct-report employees.
     *
     * Employee:
     *     Only their own employee record.
     */
    [HttpGet("employee-options")]
    public async Task<ActionResult>
        GetEmployeeOptions()
    {
        var loggedInEmployeeId =
            GetEmployeeId();

        IQueryable<Employee> query =
            _database.Employees
                .AsNoTracking()
                .Where(employee =>
                    employee.IsActive
                );

        /*
         * Administrator
         */
        if (User.IsInRole("Administrator"))
        {
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
                        employee.IsActive
                    })
                    .ToListAsync();

            return Ok(employees);
        }

        /*
         * Manager
         */
        if (User.IsInRole("Manager"))
        {
            if (!loggedInEmployeeId.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "Your manager login is not linked to an employee."
                });
            }

            var employees =
                await query
                    .Where(employee =>
                        employee.ManagerId ==
                            loggedInEmployeeId.Value
                    )
                    .OrderBy(employee =>
                        employee.FullName
                    )
                    .Select(employee => new
                    {
                        employee.Id,
                        employee.EmployeeCode,
                        employee.FullName,
                        employee.IsActive
                    })
                    .ToListAsync();

            return Ok(employees);
        }

        /*
         * Employee
         */
        if (User.IsInRole("Employee"))
        {
            if (!loggedInEmployeeId.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "Your login is not linked to an employee."
                });
            }

            var employees =
                await query
                    .Where(employee =>
                        employee.Id ==
                            loggedInEmployeeId.Value
                    )
                    .OrderBy(employee =>
                        employee.FullName
                    )
                    .Select(employee => new
                    {
                        employee.Id,
                        employee.EmployeeCode,
                        employee.FullName,
                        employee.IsActive
                    })
                    .ToListAsync();

            return Ok(employees);
        }

        return Forbid();
    }

    /*
     * Work report
     */
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

        /*
         * Validate dates.
         */
        if (reportToDate < reportFromDate)
        {
            return BadRequest(new
            {
                message =
                    "To date cannot be earlier than from date."
            });
        }

        if (
            (reportToDate - reportFromDate)
                .TotalDays > 366
        )
        {
            return BadRequest(new
            {
                message =
                    "The report date range cannot exceed one year."
            });
        }

        var loggedInEmployeeId =
            GetEmployeeId();

        List<int>? allowedEmployeeIds = null;

        /*
         * -------------------------------------------------
         * EMPLOYEE SECURITY
         * -------------------------------------------------
         *
         * Employee users are always forced to their
         * own employee record.
         *
         * Even if employeeId is manually supplied in
         * the URL, it will be replaced here.
         */
        if (User.IsInRole("Employee"))
        {
            if (!loggedInEmployeeId.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "Your login is not linked to an employee."
                });
            }

            employeeId =
                loggedInEmployeeId.Value;
        }

        /*
         * -------------------------------------------------
         * MANAGER SECURITY
         * -------------------------------------------------
         *
         * Manager users can access only employees
         * whose ManagerId equals the manager's
         * EmployeeId.
         */
        if (User.IsInRole("Manager"))
        {
            if (!loggedInEmployeeId.HasValue)
            {
                return Unauthorized(new
                {
                    message =
                        "Your manager login is not linked to an employee."
                });
            }

            allowedEmployeeIds =
                await _database.Employees
                    .AsNoTracking()
                    .Where(employee =>
                        employee.ManagerId ==
                            loggedInEmployeeId.Value &&
                        employee.IsActive
                    )
                    .Select(employee =>
                        employee.Id
                    )
                    .ToListAsync();

            /*
             * If the Manager requested one specific
             * employee, verify that employee belongs
             * to the Manager.
             */
            if (employeeId.HasValue)
            {
                var employeeIsAllowed =
                    allowedEmployeeIds.Contains(
                        employeeId.Value
                    );

                if (!employeeIsAllowed)
                {
                    return Forbid();
                }
            }
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

        /*
         * Manager without a specific employee filter
         * gets reports for ALL of their direct reports.
         */
        if (User.IsInRole("Manager"))
        {
            query =
                query.Where(workLog =>
                    allowedEmployeeIds!.Contains(
                        workLog.EmployeeId
                    )
                );
        }

        /*
         * Specific employee filter.
         *
         * Administrator:
         *     Can select any employee.
         *
         * Manager:
         *     employeeId has already been checked.
         *
         * Employee:
         *     employeeId was forced to their own ID.
         */
        if (employeeId.HasValue)
        {
            query =
                query.Where(workLog =>
                    workLog.EmployeeId ==
                        employeeId.Value
                );
        }

        /*
         * Optional project filter.
         */
        if (projectId.HasValue)
        {
            query =
                query.Where(workLog =>
                    workLog.ProjectId ==
                        projectId.Value
                );
        }

        /*
         * Main totals.
         */
        var totalWorkLogs =
            await query.CountAsync();

        var totalHours =
            await query.SumAsync(workLog =>
                (decimal?)
                    workLog.HoursWorked
            ) ?? 0;

        var averageProgress =
            await query.AverageAsync(workLog =>
                (decimal?)
                    workLog.ProgressPercentage
            ) ?? 0;

        /*
         * Work-log status totals.
         */
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

        /*
         * Employee summary.
         */
        var employeeSummaries =
            await query
                .GroupBy(workLog => new
                {
                    workLog.EmployeeId,

                    workLog.Employee
                        .EmployeeCode,

                    workLog.Employee
                        .FullName
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

        /*
         * Project summary.
         */
        var projectSummaries =
            await query
                .GroupBy(workLog => new
                {
                    workLog.ProjectId,

                    workLog.Project
                        .ProjectCode,

                    workLog.Project
                        .ProjectName
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

        /*
         * Work-log details.
         *
         * Maximum 500 records are returned.
         */
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
                        workLog.Employee
                            .EmployeeCode,

                    EmployeeName =
                        workLog.Employee
                            .FullName,

                    workLog.ProjectId,

                    ProjectCode =
                        workLog.Project
                            .ProjectCode,

                    ProjectName =
                        workLog.Project
                            .ProjectName,

                    workLog.WorkTaskId,

                    TaskCode =
                        workLog.WorkTask == null
                            ? null
                            : workLog.WorkTask
                                .TaskCode,

                    TaskTitle =
                        workLog.WorkTask == null
                            ? null
                            : workLog.WorkTask
                                .TaskTitle
                })
                .ToListAsync();

        return Ok(new
        {
            fromDate =
                reportFromDate,

            toDate =
                reportToDate,

            employeeId,
            projectId,

            totalWorkLogs,
            totalHours,

            averageProgress =
                Math.Round(
                    averageProgress,
                    2
                ),

            draftWorkLogs,
            submittedWorkLogs,
            approvedWorkLogs,
            rejectedWorkLogs,

            employeeSummaries,
            projectSummaries,
            workLogs
        });
    }

    /*
     * Reads employeeId from the JWT token.
     */
    private int? GetEmployeeId()
    {
        var employeeIdValue =
            User.FindFirstValue(
                "employeeId"
            );

        if (
            int.TryParse(
                employeeIdValue,
                out var employeeId
            )
        )
        {
            return employeeId;
        }

        return null;
    }
}