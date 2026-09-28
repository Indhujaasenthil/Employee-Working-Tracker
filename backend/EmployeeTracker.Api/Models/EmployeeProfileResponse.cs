namespace EmployeeTracker.Api.Models;

public class EmployeeProfileResponse
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; } =
        string.Empty;

    public string FullName { get; set; } =
        string.Empty;

    public string Email { get; set; } =
        string.Empty;

    public string Department { get; set; } =
        string.Empty;

    public string Designation { get; set; } =
        string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}