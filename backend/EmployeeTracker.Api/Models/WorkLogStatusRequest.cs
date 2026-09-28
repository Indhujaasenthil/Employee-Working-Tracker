using System.ComponentModel.DataAnnotations;

namespace EmployeeTracker.Api.Models;

public class WorkLogStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}