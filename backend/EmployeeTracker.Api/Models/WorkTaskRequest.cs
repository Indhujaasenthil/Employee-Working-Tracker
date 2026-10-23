using System.ComponentModel.DataAnnotations;

namespace EmployeeTracker.Api.Models;

public class WorkTaskRequest
{
    [Required]
    [MaxLength(20)]
    public string TaskCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string TaskTitle { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int ProjectId { get; set; }

    public int? AssignedEmployeeId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? DueDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "Medium";

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Not Started";

    public decimal EstimatedHours { get; set; }

    public bool IsActive { get; set; } = true;
}