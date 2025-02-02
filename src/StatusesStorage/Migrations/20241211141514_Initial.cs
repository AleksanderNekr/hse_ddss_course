using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StatusesStorage.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VacationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacationRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerApprovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Mail = table.Column<string>(type: "TEXT", nullable: false),
                    VacationRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerApprovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerApprovers_VacationRequests_VacationRequestId",
                        column: x => x.VacationRequestId,
                        principalTable: "VacationRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectApprovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VacationRequestId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectApprovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectApprovers_VacationRequests_VacationRequestId",
                        column: x => x.VacationRequestId,
                        principalTable: "VacationRequests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VacationStatusLogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VacationRequestId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ApprovalDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    RejectionDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    RejectionReason = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacationStatusLogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacationStatusLogEntries_VacationRequests_VacationRequestId",
                        column: x => x.VacationRequestId,
                        principalTable: "VacationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerApprovers_VacationRequestId",
                table: "CustomerApprovers",
                column: "VacationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectApprovers_VacationRequestId",
                table: "ProjectApprovers",
                column: "VacationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_VacationStatusLogEntries_VacationRequestId",
                table: "VacationStatusLogEntries",
                column: "VacationRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerApprovers");

            migrationBuilder.DropTable(
                name: "ProjectApprovers");

            migrationBuilder.DropTable(
                name: "VacationStatusLogEntries");

            migrationBuilder.DropTable(
                name: "VacationRequests");
        }
    }
}
