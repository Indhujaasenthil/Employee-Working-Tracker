using System.ComponentModel.DataAnnotations;

namespace EmployeeTracker.Api.Models;

public class Project
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string ProjectCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string ProjectName { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string ClientName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime? Deadline { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Not Started";

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}