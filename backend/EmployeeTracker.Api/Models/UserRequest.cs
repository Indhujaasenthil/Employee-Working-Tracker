using System.ComponentModel.DataAnnotations;

namespace EmployeeTracker.Api.Models;

public class UserRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    [MinLength(6)]
    [MaxLength(100)]
    public string? Password { get; set; }

    [Required]
    [MaxLength(30)]
    public string Role { get; set; } = "Employee";

    // Required when Role is Employee.
    // It can be null for Administrator and Manager.
    public int? EmployeeId { get; set; }
}