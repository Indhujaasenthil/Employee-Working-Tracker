using EmployeeTracker.Api.Data;
using EmployeeTracker.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeTracker.Api.Controllers;

[ApiController]
[Route("api/designations")]
[Authorize]
public class DesignationsController : ControllerBase
{
    private readonly AppDbContext _database;

    public DesignationsController(
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
        ActionResult<IEnumerable<Designation>>
    > GetDesignations()
    {
        var designations =
            await _database.Designations
                .AsNoTracking()
                .OrderBy(designation =>
                    designation.DesignationName
                )
                .ToListAsync();

        return Ok(designations);
    }

    [HttpGet("{id:int}")]
    [Authorize(
        Roles =
            "Administrator,Manager,Employee"
    )]
    public async Task<
        ActionResult<Designation>
    > GetDesignation(int id)
    {
        var designation =
            await _database.Designations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    designation =>
                        designation.Id == id
                );

        if (designation is null)
        {
            return NotFound(new
            {
                message =
                    "Designation was not found."
            });
        }

        return Ok(designation);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<
        ActionResult<Designation>
    > CreateDesignation(
        Designation request
    )
    {
        var designationCode =
            request.DesignationCode.Trim();

        var designationName =
            request.DesignationName.Trim();

        var duplicateExists =
            await _database.Designations
                .AnyAsync(designation =>
                    designation.DesignationCode
                        .ToLower() ==
                    designationCode.ToLower() ||

                    designation.DesignationName
                        .ToLower() ==
                    designationName.ToLower()
                );

        if (duplicateExists)
        {
            return Conflict(new
            {
                message =
                    "Designation code or name already exists."
            });
        }

        var designation = new Designation
        {
            DesignationCode =
                designationCode,

            DesignationName =
                designationName,

            Description =
                string.IsNullOrWhiteSpace(
                    request.Description
                )
                    ? null
                    : request.Description.Trim(),

            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _database.Designations.Add(
            designation
        );

        await _database.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetDesignation),
            new { id = designation.Id },
            designation
        );
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult>
        UpdateDesignation(
            int id,
            Designation request
        )
    {
        var designation =
            await _database.Designations
                .FindAsync(id);

        if (designation is null)
        {
            return NotFound(new
            {
                message =
                    "Designation was not found."
            });
        }

        var designationCode =
            request.DesignationCode.Trim();

        var designationName =
            request.DesignationName.Trim();

        var duplicateExists =
            await _database.Designations
                .AnyAsync(existingDesignation =>
                    existingDesignation.Id != id &&
                    (
                        existingDesignation
                            .DesignationCode
                            .ToLower() ==
                        designationCode.ToLower() ||

                        existingDesignation
                            .DesignationName
                            .ToLower() ==
                        designationName.ToLower()
                    )
                );

        if (duplicateExists)
        {
            return Conflict(new
            {
                message =
                    "Designation code or name already exists."
            });
        }

        var previousDesignationName =
            designation.DesignationName;

        designation.DesignationCode =
            designationCode;

        designation.DesignationName =
            designationName;

        designation.Description =
            string.IsNullOrWhiteSpace(
                request.Description
            )
                ? null
                : request.Description.Trim();

        designation.IsActive =
            request.IsActive;

        if (
            !string.Equals(
                previousDesignationName,
                designationName,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            var employees =
                await _database.Employees
                    .Where(employee =>
                        employee.Designation ==
                        previousDesignationName
                    )
                    .ToListAsync();

            foreach (var employee in employees)
            {
                employee.Designation =
                    designationName;
            }
        }

        await _database.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult>
        DeleteDesignation(int id)
    {
        var designation =
            await _database.Designations
                .FindAsync(id);

        if (designation is null)
        {
            return NotFound(new
            {
                message =
                    "Designation was not found."
            });
        }

        var isUsedByEmployees =
            await _database.Employees
                .AnyAsync(employee =>
                    employee.Designation ==
                    designation.DesignationName
                );

        if (isUsedByEmployees)
        {
            return BadRequest(new
            {
                message =
                    "This designation is assigned to employees. Set it as inactive instead of deleting it."
            });
        }

        _database.Designations.Remove(
            designation
        );

        await _database.SaveChangesAsync();

        return NoContent();
    }
}