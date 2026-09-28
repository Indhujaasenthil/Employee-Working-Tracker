using System.ComponentModel.DataAnnotations;

namespace EmployeeTracker.Api.Models;

public class Designation
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string DesignationCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DesignationName { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}