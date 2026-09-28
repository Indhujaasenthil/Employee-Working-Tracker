using System.ComponentModel.DataAnnotations;

namespace EmployeeTracker.Api.Models;

public class LoginRequest
{
    [Required(
        ErrorMessage = "Username is required."
    )]
    [MaxLength(100)]
    public string UserName { get; set; } =
        string.Empty;

    [Required(
        ErrorMessage = "Password is required."
    )]
    public string Password { get; set; } =
        string.Empty;
}