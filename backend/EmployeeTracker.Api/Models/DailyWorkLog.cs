using System.ComponentModel.DataAnnotations;

namespace EmployeeTracker.Api.Models;

public class DailyWorkLog
{
    public int Id { get; set; }


    // =========================
    // Employee
    // =========================

    public int EmployeeId { get; set; }

    public Employee? Employee { get; set; }


    // =========================
    // Project
    // =========================

    public int ProjectId { get; set; }

    public Project? Project { get; set; }


    // =========================
    // Task
    // =========================

    public int? WorkTaskId { get; set; }

    public WorkTask? WorkTask { get; set; }


    // =========================
    // Work Date
    // =========================

    public DateTime WorkDate { get; set; }


    // =========================
    // Work Description
    // =========================

    [Required]
    [MaxLength(1000)]
    public string WorkDescription { get; set; } =
        string.Empty;


    // =========================
    // Hours Worked
    // =========================

    public decimal HoursWorked { get; set; }


    // =========================
    // Progress Percentage
    // =========================

    public int ProgressPercentage { get; set; }


    // =========================
    // Blockers
    // =========================

    [MaxLength(500)]
    public string? Blockers { get; set; }


    // =========================
    // Status
    // =========================

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } =
        "Draft";


    // =========================
    // Created At
    // =========================

    public DateTime CreatedAt { get; set; } =
        DateTime.UtcNow;
}