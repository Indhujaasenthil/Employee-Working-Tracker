using System.ComponentModel.DataAnnotations;

namespace EmployeeTracker.Api.Models;

public class ProjectMemberRequest
{
    [Range(1, int.MaxValue)]
    public int ProjectId { get; set; }

    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ProjectRole { get; set; } = "Team Member";

    [Required]
    public DateTime AssignedDate { get; set; }

    public bool IsActive { get; set; } = true;
}