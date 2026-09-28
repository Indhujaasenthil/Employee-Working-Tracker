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

    public EmployeesController(
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
        ActionResult<IEnumerable<Employee>>
    > GetEmployees()
    {
        var employees =
            await _database.Employees
                .AsNoTracking()
                .OrderBy(employee =>
                    employee.FullName
                )
                .ToListAsync();

        return Ok(employees);
    }

    [HttpGet("{id:int}")]
    [Authorize(
        Roles =
            "Administrator,Manager,Employee"
    )]
    public async Task<
        ActionResult<Employee>
    > GetEmployee(int id)
    {
        var employee =
            await _database.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    employee =>
                        employee.Id == id
                );

        if (employee is null)
        {
            return NotFound(new
            {
                message =
                    "Employee was not found."
            });
        }

        return Ok(employee);
    }

    [HttpPost]
    [Authorize(
        Roles =
            "Administrator,Manager"
    )]
    public async Task<
        ActionResult<Employee>
    > CreateEmployee(
        Employee request
    )
    {
        var employeeCode =
            request.EmployeeCode.Trim();

        var email =
            request.Email.Trim();

        var duplicateExists =
            await _database.Employees
                .AnyAsync(employee =>
                    employee.EmployeeCode
                        .ToLower() ==
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

        var employee = new Employee
        {
            EmployeeCode = employeeCode,
            FullName =
                request.FullName.Trim(),
            Email = email,
            Department =
                request.Department.Trim(),
            Designation =
                request.Designation.Trim(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _database.Employees.Add(employee);

        await _database.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetEmployee),
            new { id = employee.Id },
            employee
        );
    }

    [HttpPut("{id:int}")]
    [Authorize(
        Roles =
            "Administrator,Manager"
    )]
    public async Task<ActionResult>
        UpdateEmployee(
            int id,
            Employee request
        )
    {
        var employee =
            await _database.Employees
                .FindAsync(id);

        if (employee is null)
        {
            return NotFound(new
            {
                message =
                    "Employee was not found."
            });
        }

        var employeeCode =
            request.EmployeeCode.Trim();

        var email =
            request.Email.Trim();

        var duplicateExists =
            await _database.Employees
                .AnyAsync(existingEmployee =>
                    existingEmployee.Id != id &&
                    (
                        existingEmployee
                            .EmployeeCode
                            .ToLower() ==
                        employeeCode.ToLower() ||

                        existingEmployee
                            .Email
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

        employee.EmployeeCode =
            employeeCode;

        employee.FullName =
            request.FullName.Trim();

        employee.Email = email;

        employee.Department =
            request.Department.Trim();

        employee.Designation =
            request.Designation.Trim();

        employee.IsActive =
            request.IsActive;

        await _database.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult>
        DeleteEmployee(int id)
    {
        var employee =
            await _database.Employees
                .FindAsync(id);

        if (employee is null)
        {
            return NotFound(new
            {
                message =
                    "Employee was not found."
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

        if (
            hasProjectAssignments ||
            hasTasks ||
            hasWorkLogs
        )
        {
            return BadRequest(new
            {
                message =
                    "This employee cannot be deleted because related project, task or work-log records exist. Set the employee as inactive instead."
            });
        }

        _database.Employees.Remove(employee);

        await _database.SaveChangesAsync();

        return NoContent();
    }
}