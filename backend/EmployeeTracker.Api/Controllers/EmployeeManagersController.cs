using EmployeeTracker.Api.Data;
using EmployeeTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeTracker.Api.Controllers;

[ApiController]
[Route("api/employee-managers")]
[Authorize(Roles = "Administrator,Manager")]
public class EmployeeManagersController : ControllerBase
{
    private readonly AppDbContext _database;

    public EmployeeManagersController(
        AppDbContext database
    )
    {
        _database = database;
    }

    /*
     * Administrator and Manager can view
     * the employee reporting structure.
     *
     * Administrator:
     * - Can view all employees.
     * - Can assign/remove managers.
     *
     * Manager:
     * - Can view reporting structure only.
     */
    [HttpGet]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<ActionResult> GetEmployees()
    {
        var employees =
            await _database.Employees
                .AsNoTracking()
                .OrderBy(employee =>
                    employee.FullName)
                .Select(employee => new
                {
                    employee.Id,
                    employee.EmployeeCode,
                    employee.FullName,
                    employee.Email,
                    employee.Department,
                    employee.Designation,
                    employee.IsActive,

                    employee.ManagerId,

                    ManagerName =
                        employee.Manager != null
                            ? employee.Manager.FullName
                            : null
                })
                .ToListAsync();

        return Ok(employees);
    }

    /*
     * Only Administrator needs the list
     * of employees that can be selected
     * as managers.
     *
     * Manager UI is read-only, so Managers
     * should not access this endpoint.
     */
    [HttpGet("available-managers")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult>
        GetAvailableManagers()
    {
        var managers =
            await _database.Employees
                .AsNoTracking()
                .Where(employee =>
                    employee.IsActive)
                .OrderBy(employee =>
                    employee.FullName)
                .Select(employee => new
                {
                    employee.Id,
                    employee.EmployeeCode,
                    employee.FullName,
                    employee.Designation
                })
                .ToListAsync();

        return Ok(managers);
    }

    /*
     * Only Administrator can assign,
     * change, or remove an employee's
     * reporting manager.
     *
     * Sending ManagerId as null removes
     * the current manager assignment.
     */
    [HttpPut("{employeeId:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult> AssignManager(
        int employeeId,
        EmployeeManagerRequest request
    )
    {
        var employee =
            await _database.Employees
                .FirstOrDefaultAsync(item =>
                    item.Id == employeeId);

        if (employee is null)
        {
            return NotFound(new
            {
                message =
                    "Employee was not found."
            });
        }

        /*
         * A null value removes the
         * current manager.
         */
        if (!request.ManagerId.HasValue)
        {
            employee.ManagerId = null;

            await _database.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Manager assignment removed successfully."
            });
        }

        var managerId =
            request.ManagerId.Value;

        /*
         * An employee cannot report
         * to themselves.
         */
        if (managerId == employeeId)
        {
            return BadRequest(new
            {
                message =
                    "An employee cannot be their own manager."
            });
        }

        /*
         * Selected manager must exist.
         */
        var manager =
            await _database.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id == managerId);

        if (manager is null)
        {
            return BadRequest(new
            {
                message =
                    "The selected manager was not found."
            });
        }

        /*
         * Inactive employees cannot
         * become reporting managers.
         */
        if (!manager.IsActive)
        {
            return BadRequest(new
            {
                message =
                    "An inactive employee cannot be assigned as a manager."
            });
        }

        /*
         * Prevent circular reporting
         * relationships.
         *
         * Example:
         *
         * EMP001 -> EMP002
         * EMP002 -> EMP001
         *
         * This must not be allowed.
         */
        var createsCircularRelationship =
            await CreatesCircularRelationship(
                employeeId,
                managerId
            );

        if (createsCircularRelationship)
        {
            return BadRequest(new
            {
                message =
                    "This assignment would create a circular manager relationship."
            });
        }

        employee.ManagerId = managerId;

        await _database.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Manager assigned successfully.",

            employee.Id,

            employee.ManagerId,

            ManagerName =
                manager.FullName
        });
    }

    /*
     * Checks the manager hierarchy before
     * saving an assignment.
     */
    private async Task<bool>
        CreatesCircularRelationship(
            int employeeId,
            int selectedManagerId
        )
    {
        int? currentEmployeeId =
            selectedManagerId;

        var visitedEmployeeIds =
            new HashSet<int>();

        while (currentEmployeeId.HasValue)
        {
            /*
             * We reached the employee being
             * modified, which means this new
             * assignment creates a cycle.
             */
            if (
                currentEmployeeId.Value ==
                employeeId
            )
            {
                return true;
            }

            /*
             * Protect against an existing
             * invalid/circular hierarchy.
             */
            if (
                !visitedEmployeeIds.Add(
                    currentEmployeeId.Value
                )
            )
            {
                return true;
            }

            currentEmployeeId =
                await _database.Employees
                    .AsNoTracking()
                    .Where(employee =>
                        employee.Id ==
                        currentEmployeeId.Value)
                    .Select(employee =>
                        employee.ManagerId)
                    .FirstOrDefaultAsync();
        }

        return false;
    }
}