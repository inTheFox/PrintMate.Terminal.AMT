using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintMate.Terminal.Migrations
{
    /// <inheritdoc />
    public partial class sessionsServiceMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LayersStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsPowderApplied = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMarkingStarted = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMarkingFinished = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LayersStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PrintSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintSessions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LayersStates");

            migrationBuilder.DropTable(
                name: "PrintSessions");
        }
    }
}
