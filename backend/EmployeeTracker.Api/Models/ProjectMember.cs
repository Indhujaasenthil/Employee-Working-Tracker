using System.ComponentModel.DataAnnotations;

namespace EmployeeTracker.Api.Models;

public class ProjectMember
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string ProjectRole { get; set; } =
        "Team Member";

    public DateTime AssignedDate { get; set; } =
        DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}