using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDesignationMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Designations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignationCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DesignationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Designations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Designations_DesignationCode",
                table: "Designations",
                column: "DesignationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Designations_DesignationName",
                table: "Designations",
                column: "DesignationName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Designations");
        }
    }
}
