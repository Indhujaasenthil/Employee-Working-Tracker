using EmployeeTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeTracker.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options
    ) : base(options)
    {
    }

    public DbSet<Employee> Employees =>
        Set<Employee>();

    public DbSet<Department> Departments =>
        Set<Department>();

    public DbSet<Designation> Designations =>
        Set<Designation>();

    public DbSet<Project> Projects =>
        Set<Project>();

    public DbSet<ProjectMember> ProjectMembers =>
        Set<ProjectMember>();

    public DbSet<WorkTask> WorkTasks =>
        Set<WorkTask>();

    public DbSet<DailyWorkLog> DailyWorkLogs =>
        Set<DailyWorkLog>();

    public DbSet<AppUser> AppUsers =>
        Set<AppUser>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder
    )
    {
        base.OnModelCreating(modelBuilder);

        ConfigureEmployee(modelBuilder);
        ConfigureDepartment(modelBuilder);
        ConfigureDesignation(modelBuilder);
        ConfigureProject(modelBuilder);
        ConfigureProjectMember(modelBuilder);
        ConfigureWorkTask(modelBuilder);
        ConfigureDailyWorkLog(modelBuilder);
        ConfigureAppUser(modelBuilder);
    }

    private static void ConfigureEmployee(
        ModelBuilder modelBuilder
    )
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");

            entity.HasKey(employee =>
                employee.Id);

            entity.Property(employee =>
                    employee.EmployeeCode)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(employee =>
                    employee.FullName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(employee =>
                    employee.Email)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(employee =>
                    employee.Department)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(employee =>
                    employee.Designation)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(employee =>
                    employee.IsActive)
                .IsRequired();

            entity.Property(employee =>
                    employee.CreatedAt)
                .IsRequired();

            entity.HasIndex(employee =>
                    employee.EmployeeCode)
                .IsUnique();

            entity.HasIndex(employee =>
                    employee.Email)
                .IsUnique();

            entity.HasIndex(employee =>
                employee.ManagerId);

            /*
             * Self-referencing relationship:
             *
             * One manager can manage many employees.
             * An employee can have one optional manager.
             */
            entity.HasOne(employee =>
                    employee.Manager)
                .WithMany(manager =>
                    manager.TeamMembers)
                .HasForeignKey(employee =>
                    employee.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureDepartment(
        ModelBuilder modelBuilder
    )
    {
        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");

            entity.HasKey(department =>
                department.Id);

            entity.Property(department =>
                    department.DepartmentCode)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(department =>
                    department.DepartmentName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(department =>
                    department.Description)
                .HasMaxLength(250);

            entity.Property(department =>
                    department.IsActive)
                .IsRequired();

            entity.Property(department =>
                    department.CreatedAt)
                .IsRequired();

            entity.HasIndex(department =>
                    department.DepartmentCode)
                .IsUnique();

            entity.HasIndex(department =>
                    department.DepartmentName)
                .IsUnique();
        });
    }

    private static void ConfigureDesignation(
        ModelBuilder modelBuilder
    )
    {
        modelBuilder.Entity<Designation>(entity =>
        {
            entity.ToTable("Designations");

            entity.HasKey(designation =>
                designation.Id);

            entity.Property(designation =>
                    designation.DesignationCode)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(designation =>
                    designation.DesignationName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(designation =>
                    designation.Description)
                .HasMaxLength(250);

            entity.Property(designation =>
                    designation.IsActive)
                .IsRequired();

            entity.Property(designation =>
                    designation.CreatedAt)
                .IsRequired();

            entity.HasIndex(designation =>
                    designation.DesignationCode)
                .IsUnique();

            entity.HasIndex(designation =>
                    designation.DesignationName)
                .IsUnique();
        });
    }

    private static void ConfigureProject(
        ModelBuilder modelBuilder
    )
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects");

            entity.HasKey(project =>
                project.Id);

            entity.Property(project =>
                    project.ProjectCode)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(project =>
                    project.ProjectName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(project =>
                    project.ClientName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(project =>
                    project.StartDate)
                .IsRequired();

            entity.Property(project =>
                project.Deadline);

            entity.Property(project =>
                    project.Status)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(project =>
                    project.Description)
                .HasMaxLength(500);

            entity.Property(project =>
                    project.IsActive)
                .IsRequired();

            entity.Property(project =>
                    project.CreatedAt)
                .IsRequired();

            entity.HasIndex(project =>
                    project.ProjectCode)
                .IsUnique();

            entity.HasIndex(project =>
                project.ProjectName);
        });
    }

    private static void ConfigureProjectMember(
        ModelBuilder modelBuilder
    )
    {
        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.ToTable("ProjectMembers");

            entity.HasKey(member =>
                member.Id);

            entity.Property(member =>
                    member.ProjectRole)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(member =>
                    member.AssignedDate)
                .IsRequired();

            entity.Property(member =>
                    member.IsActive)
                .IsRequired();

            entity.HasOne(member =>
                    member.Project)
                .WithMany()
                .HasForeignKey(member =>
                    member.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(member =>
                    member.Employee)
                .WithMany()
                .HasForeignKey(member =>
                    member.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(member => new
                {
                    member.ProjectId,
                    member.EmployeeId
                })
                .IsUnique();
        });
    }

    private static void ConfigureWorkTask(
        ModelBuilder modelBuilder
    )
    {
        modelBuilder.Entity<WorkTask>(entity =>
        {
            entity.ToTable("WorkTasks");

            entity.HasKey(workTask =>
                workTask.Id);

            entity.Property(workTask =>
                    workTask.TaskCode)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(workTask =>
                    workTask.TaskTitle)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(workTask =>
                    workTask.Description)
                .HasMaxLength(1000);

            entity.Property(workTask =>
                    workTask.StartDate)
                .IsRequired();

            entity.Property(workTask =>
                workTask.DueDate);

            entity.Property(workTask =>
                    workTask.Priority)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(workTask =>
                    workTask.Status)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(workTask =>
                    workTask.EstimatedHours)
                .HasPrecision(10, 2);

            entity.Property(workTask =>
                    workTask.IsActive)
                .IsRequired();

            entity.Property(workTask =>
                    workTask.CreatedAt)
                .IsRequired();

            entity.HasIndex(workTask =>
                    workTask.TaskCode)
                .IsUnique();

            entity.HasOne(workTask =>
                    workTask.Project)
                .WithMany()
                .HasForeignKey(workTask =>
                    workTask.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(workTask =>
                    workTask.AssignedEmployee)
                .WithMany()
                .HasForeignKey(workTask =>
                    workTask.AssignedEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureDailyWorkLog(
        ModelBuilder modelBuilder
    )
    {
        modelBuilder.Entity<DailyWorkLog>(entity =>
        {
            entity.ToTable("DailyWorkLogs");

            entity.HasKey(log =>
                log.Id);

            entity.Property(log =>
                    log.WorkDate)
                .IsRequired();

            entity.Property(log =>
                    log.WorkDescription)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(log =>
                    log.HoursWorked)
                .HasPrecision(5, 2);

            entity.Property(log =>
                    log.ProgressPercentage)
                .IsRequired();

            entity.Property(log =>
                    log.Blockers)
                .HasMaxLength(500);

            entity.Property(log =>
                    log.Status)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(log =>
                    log.CreatedAt)
                .IsRequired();

            entity.HasOne(log =>
                    log.Employee)
                .WithMany()
                .HasForeignKey(log =>
                    log.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(log =>
                    log.Project)
                .WithMany()
                .HasForeignKey(log =>
                    log.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(log =>
                    log.WorkTask)
                .WithMany()
                .HasForeignKey(log =>
                    log.WorkTaskId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(log =>
                log.WorkDate);

            entity.HasIndex(log => new
            {
                log.EmployeeId,
                log.WorkDate
            });
        });
    }

    private static void ConfigureAppUser(
        ModelBuilder modelBuilder
    )
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AppUsers");

            entity.HasKey(user =>
                user.Id);

            entity.Property(user =>
                    user.UserName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(user =>
                    user.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(user =>
                    user.Role)
                .IsRequired()
                .HasMaxLength(30);

            entity.HasIndex(user =>
                    user.UserName)
                .IsUnique();

            entity.HasOne(user =>
                    user.Employee)
                .WithOne()
                .HasForeignKey<AppUser>(user =>
                    user.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(user =>
                    user.EmployeeId)
                .IsUnique()
                .HasFilter(
                    "[EmployeeId] IS NOT NULL"
                );
        });
    }
}