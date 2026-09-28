using EmployeeTracker.Api.Data;
using EmployeeTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeTracker.Api.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _database;

    public DepartmentsController(
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
        ActionResult<IEnumerable<Department>>
    > GetDepartments()
    {
        var departments =
            await _database.Departments
                .AsNoTracking()
                .OrderBy(department =>
                    department.DepartmentName
                )
                .ToListAsync();

        return Ok(departments);
    }

    [HttpGet("{id:int}")]
    [Authorize(
        Roles =
            "Administrator,Manager,Employee"
    )]
    public async Task<
        ActionResult<Department>
    > GetDepartment(int id)
    {
        var department =
            await _database.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    department =>
                        department.Id == id
                );

        if (department is null)
        {
            return NotFound(new
            {
                message =
                    "Department was not found."
            });
        }

        return Ok(department);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<
        ActionResult<Department>
    > CreateDepartment(
        Department request
    )
    {
        var departmentCode =
            request.DepartmentCode.Trim();

        var departmentName =
            request.DepartmentName.Trim();

        var duplicateExists =
            await _database.Departments
                .AnyAsync(department =>
                    department.DepartmentCode
                        .ToLower() ==
                    departmentCode.ToLower() ||

                    department.DepartmentName
                        .ToLower() ==
                    departmentName.ToLower()
                );

        if (duplicateExists)
        {
            return Conflict(new
            {
                message =
                    "Department code or name already exists."
            });
        }

        var department = new Department
        {
            DepartmentCode =
                departmentCode,

            DepartmentName =
                departmentName,

            Description =
                string.IsNullOrWhiteSpace(
                    request.Description
                )
                    ? null
                    : request.Description.Trim(),

            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _database.Departments.Add(
            department
        );

        await _database.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetDepartment),
            new { id = department.Id },
            department
        );
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult>
        UpdateDepartment(
            int id,
            Department request
        )
    {
        var department =
            await _database.Departments
                .FindAsync(id);

        if (department is null)
        {
            return NotFound(new
            {
                message =
                    "Department was not found."
            });
        }

        var departmentCode =
            request.DepartmentCode.Trim();

        var departmentName =
            request.DepartmentName.Trim();

        var duplicateExists =
            await _database.Departments
                .AnyAsync(existingDepartment =>
                    existingDepartment.Id != id &&
                    (
                        existingDepartment
                            .DepartmentCode
                            .ToLower() ==
                        departmentCode.ToLower() ||

                        existingDepartment
                            .DepartmentName
                            .ToLower() ==
                        departmentName.ToLower()
                    )
                );

        if (duplicateExists)
        {
            return Conflict(new
            {
                message =
                    "Department code or name already exists."
            });
        }

        var previousDepartmentName =
            department.DepartmentName;

        department.DepartmentCode =
            departmentCode;

        department.DepartmentName =
            departmentName;

        department.Description =
            string.IsNullOrWhiteSpace(
                request.Description
            )
                ? null
                : request.Description.Trim();

        department.IsActive =
            request.IsActive;

        if (
            !string.Equals(
                previousDepartmentName,
                departmentName,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            var employees =
                await _database.Employees
                    .Where(employee =>
                        employee.Department ==
                        previousDepartmentName
                    )
                    .ToListAsync();

            foreach (var employee in employees)
            {
                employee.Department =
                    departmentName;
            }
        }

        await _database.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult>
        DeleteDepartment(int id)
    {
        var department =
            await _database.Departments
                .FindAsync(id);

        if (department is null)
        {
            return NotFound(new
            {
                message =
                    "Department was not found."
            });
        }

        var isUsedByEmployees =
            await _database.Employees
                .AnyAsync(employee =>
                    employee.Department ==
                    department.DepartmentName
                );

        if (isUsedByEmployees)
        {
            return BadRequest(new
            {
                message =
                    "This department is assigned to employees. Set it as inactive instead of deleting it."
            });
        }

        _database.Departments.Remove(
            department
        );

        await _database.SaveChangesAsync();

        return NoContent();
    }
}