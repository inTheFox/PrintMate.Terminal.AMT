using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintMate.Terminal.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PermissionAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleName = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId1 = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionAccesses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PermissionAccesses_Users_UserId1",
                        column: x => x.UserId1,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Login",
                table: "Users",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAccesses_UserId_RoleName",
                table: "PermissionAccesses",
                columns: new[] { "UserId", "RoleName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionAccesses_UserId1",
                table: "PermissionAccesses",
                column: "UserId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PermissionAccesses");

            migrationBuilder.DropIndex(
                name: "IX_Users_Login",
                table: "Users");
        }
    }
}
