namespace EmployeeTracker.Api.Models;

public class EmployeeManagerRequest
{
    /*
     * Use null to remove the employee's
     * current manager assignment.
     */
    public int? ManagerId { get; set; }
}