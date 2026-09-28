using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class SyncDailyWorkLogModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyWorkLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    WorkTaskId = table.Column<int>(type: "int", nullable: true),
                    WorkDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WorkDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    HoursWorked = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    Blockers = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyWorkLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyWorkLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyWorkLogs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyWorkLogs_WorkTasks_WorkTaskId",
                        column: x => x.WorkTaskId,
                        principalTable: "WorkTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkLogs_EmployeeId_WorkDate",
                table: "DailyWorkLogs",
                columns: new[] { "EmployeeId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkLogs_ProjectId",
                table: "DailyWorkLogs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkLogs_WorkDate",
                table: "DailyWorkLogs",
                column: "WorkDate");

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkLogs_WorkTaskId",
                table: "DailyWorkLogs",
                column: "WorkTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyWorkLogs");
        }
    }
}
