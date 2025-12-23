using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintMate.Terminal.Migrations
{
    /// <inheritdoc />
    public partial class fjenwfnekwjfew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowOn",
                table: "PrintSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowOn",
                table: "PrintSessions");
        }
    }
}
