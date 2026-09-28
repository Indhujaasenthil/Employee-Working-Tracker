using System.ComponentModel.DataAnnotations;

namespace EmployeeTracker.Api.Models;

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } =
        string.Empty;

    [Required]
    [StringLength(
        100,
        MinimumLength = 6,
        ErrorMessage =
            "New password must contain at least 6 characters."
    )]
    public string NewPassword { get; set; } =
        string.Empty;

    [Required]
    [Compare(
        nameof(NewPassword),
        ErrorMessage =
            "New password and confirmation password do not match."
    )]
    public string ConfirmPassword { get; set; } =
        string.Empty;
}