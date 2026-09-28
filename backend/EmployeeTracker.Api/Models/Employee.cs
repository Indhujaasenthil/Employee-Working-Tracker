using System.ComponentModel.DataAnnotations;

namespace EmployeeTracker.Api.Models;

public class Employee
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string EmployeeCode { get; set; } =
        string.Empty;

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } =
        string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } =
        string.Empty;

    [Required]
    [MaxLength(100)]
    public string Department { get; set; } =
        string.Empty;

    [Required]
    [MaxLength(100)]
    public string Designation { get; set; } =
        string.Empty;

    /*
     * ManagerId is optional because managers and
     * administrators may not report to another employee.
     */
    public int? ManagerId { get; set; }

    /*
     * Employee who manages this employee.
     */
    public Employee? Manager { get; set; }

    /*
     * Employees who report to this employee.
     */
    public ICollection<Employee> TeamMembers { get; set; } =
        new List<Employee>();

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } =
        DateTime.UtcNow;
}