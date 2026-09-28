using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class LinkUsersToEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "AppUsers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "AppUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_EmployeeId",
                table: "AppUsers",
                column: "EmployeeId",
                unique: true,
                filter: "[EmployeeId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AppUsers_Employees_EmployeeId",
                table: "AppUsers",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppUsers_Employees_EmployeeId",
                table: "AppUsers");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_EmployeeId",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "AppUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "AppUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);
        }
    }
}
