using System.ComponentModel.DataAnnotations;

namespace EmployeeTracker.Api.Models;

public class TaskStatusRequest
{
    [Required]
    [MaxLength(30)]
    public string Status { get; set; } =
        string.Empty;
}