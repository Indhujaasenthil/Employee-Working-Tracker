using System.ComponentModel.DataAnnotations;

namespace EmployeeTracker.Api.Models;

public class DailyWorkLog
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public int? WorkTaskId { get; set; }

    public WorkTask? WorkTask { get; set; }

    public DateTime WorkDate { get; set; }

    [Required]
    [MaxLength(1000)]
    public string WorkDescription { get; set; } =
        string.Empty;

    public decimal HoursWorked { get; set; }

    public int ProgressPercentage { get; set; }

    [MaxLength(500)]
    public string? Blockers { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Draft";

    public DateTime CreatedAt { get; set; } =
        DateTime.UtcNow;
}