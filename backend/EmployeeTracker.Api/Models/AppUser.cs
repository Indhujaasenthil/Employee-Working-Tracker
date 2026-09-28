using System.ComponentModel.DataAnnotations;

namespace EmployeeTracker.Api.Models;

public class AppUser
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Role { get; set; } = "Employee";

    // Links an Employee login account to an employee record.
    // Administrator and Manager accounts can leave this null.
    public int? EmployeeId { get; set; }

    public Employee? Employee { get; set; }
}