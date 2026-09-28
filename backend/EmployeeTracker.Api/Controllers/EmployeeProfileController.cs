using System.Security.Claims;
using EmployeeTracker.Api.Data;
using EmployeeTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeTracker.Api.Controllers;

[ApiController]
[Route("api/employee-profile")]
[Authorize(
    Roles = "Administrator,Manager,Employee"
)]
public class EmployeeProfileController :
    ControllerBase
{
    private readonly AppDbContext _database;

    public EmployeeProfileController(
        AppDbContext database
    )
    {
        _database = database;
    }

    [HttpGet]
    public async Task<
        ActionResult<EmployeeProfileResponse>
    > GetProfile()
    {
        var employeeIdValue =
            User.FindFirstValue("employeeId");

        if (!int.TryParse(
                employeeIdValue,
                out var employeeId
            ))
        {
            return BadRequest(new
            {
                message =
                    "Your login account is not linked " +
                    "to an employee record."
            });
        }

        var employee =
            await _database.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id == employeeId
                );

        if (employee is null)
        {
            return NotFound(new
            {
                message =
                    "The linked employee record " +
                    "was not found."
            });
        }

        var response =
            new EmployeeProfileResponse
            {
                Id = employee.Id,

                EmployeeCode =
                    employee.EmployeeCode,

                FullName =
                    employee.FullName,

                Email =
                    employee.Email,

                Department =
                    employee.Department,

                Designation =
                    employee.Designation,

                IsActive =
                    employee.IsActive,

                CreatedAt =
                    employee.CreatedAt
            };

        return Ok(response);
    }
}