using System.Security.Claims;
using EmployeeTracker.Api.Data;
using EmployeeTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeTracker.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(
    Roles = "Administrator,Manager,Employee"
)]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _database;

    public DashboardController(
        AppDbContext database
    )
    {
        _database = database;
    }

    [HttpGet]
    public async Task<ActionResult> GetDashboard()
    {
        IQueryable<Employee> employeeQuery =
            _database.Employees
                .AsNoTracking();

        IQueryable<Project> projectQuery =
            _database.Projects
                .AsNoTracking();

        IQueryable<WorkTask> taskQuery =
            _database.WorkTasks
                .AsNoTracking();

        IQueryable<DailyWorkLog> workLogQuery =
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

            employeeQuery =
                employeeQuery.Where(employee =>
                    employee.Id == employeeId.Value
                );

            projectQuery =
                projectQuery.Where(project =>
                    _database.ProjectMembers.Any(member =>
                        member.ProjectId == project.Id &&
                        member.EmployeeId ==
                            employeeId.Value &&
                        member.IsActive
                    )
                );

            taskQuery =
                taskQuery.Where(workTask =>
                    workTask.AssignedEmployeeId ==
                        employeeId.Value
                );

            workLogQuery =
                workLogQuery.Where(workLog =>
                    workLog.EmployeeId ==
                        employeeId.Value
                );
        }

        var totalEmployees =
            await employeeQuery.CountAsync();

        var activeEmployees =
            await employeeQuery.CountAsync(employee =>
                employee.IsActive
            );

        var totalProjects =
            await projectQuery.CountAsync();

        var activeProjects =
            await projectQuery.CountAsync(project =>
                project.IsActive
            );

        var totalTasks =
            await taskQuery.CountAsync();

        var pendingTasks =
            await taskQuery.CountAsync(workTask =>
                workTask.Status != "Completed" &&
                workTask.IsActive
            );

        var completedTasks =
            await taskQuery.CountAsync(workTask =>
                workTask.Status == "Completed"
            );

        var totalHours =
            await workLogQuery.SumAsync(workLog =>
                (decimal?)workLog.HoursWorked
            ) ?? 0;

        var totalWorkLogs =
            await workLogQuery.CountAsync();

        var draftWorkLogs =
            await workLogQuery.CountAsync(workLog =>
                workLog.Status == "Draft"
            );

        var submittedWorkLogs =
            await workLogQuery.CountAsync(workLog =>
                workLog.Status == "Submitted"
            );

        var approvedWorkLogs =
            await workLogQuery.CountAsync(workLog =>
                workLog.Status == "Approved"
            );

        var rejectedWorkLogs =
            await workLogQuery.CountAsync(workLog =>
                workLog.Status == "Rejected"
            );

        var recentWorkLogs =
            await workLogQuery
                .OrderByDescending(workLog =>
                    workLog.WorkDate
                )
                .ThenByDescending(workLog =>
                    workLog.CreatedAt
                )
                .Take(5)
                .Select(workLog => new
                {
                    workLog.Id,
                    workLog.WorkDate,
                    workLog.HoursWorked,
                    workLog.ProgressPercentage,
                    workLog.Status,
                    workLog.WorkDescription,

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

        return Ok(new
        {
            totalEmployees,
            activeEmployees,

            totalProjects,
            activeProjects,

            totalTasks,
            pendingTasks,
            completedTasks,

            totalHours,
            totalWorkHours = totalHours,
            totalHoursWorked = totalHours,

            totalWorkLogs,
            draftWorkLogs,
            submittedWorkLogs,
            approvedWorkLogs,
            rejectedWorkLogs,

            recentWorkLogs
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