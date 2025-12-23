using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrintMate.Terminal.Migrations
{
    /// <inheritdoc />
    public partial class AddPrintSessionsауау : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "PrintSessions",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "PrintSessions",
                newName: "StartedAt");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "LayersStates",
                newName: "StartedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                table: "PrintSessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastCompletedLayer",
                table: "PrintSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProjectInfoId",
                table: "PrintSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProjectName",
                table: "PrintSessions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TotalLayers",
                table: "PrintSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "PrintSessions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                table: "LayersStates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPlatformDown",
                table: "LayersStates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LayerNumber",
                table: "LayersStates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_LayersStates_SessionId",
                table: "LayersStates",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_LayersStates_PrintSessions_SessionId",
                table: "LayersStates",
                column: "SessionId",
                principalTable: "PrintSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LayersStates_PrintSessions_SessionId",
                table: "LayersStates");

            migrationBuilder.DropIndex(
                name: "IX_LayersStates_SessionId",
                table: "LayersStates");

            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "PrintSessions");

            migrationBuilder.DropColumn(
                name: "LastCompletedLayer",
                table: "PrintSessions");

            migrationBuilder.DropColumn(
                name: "ProjectInfoId",
                table: "PrintSessions");

            migrationBuilder.DropColumn(
                name: "ProjectName",
                table: "PrintSessions");

            migrationBuilder.DropColumn(
                name: "TotalLayers",
                table: "PrintSessions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PrintSessions");

            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "LayersStates");

            migrationBuilder.DropColumn(
                name: "IsPlatformDown",
                table: "LayersStates");

            migrationBuilder.DropColumn(
                name: "LayerNumber",
                table: "LayersStates");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "PrintSessions",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "StartedAt",
                table: "PrintSessions",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "StartedAt",
                table: "LayersStates",
                newName: "Date");
        }
    }
}
