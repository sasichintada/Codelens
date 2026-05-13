using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeLens.API.Migrations
{
    /// <inheritdoc />
    public partial class MakeUserIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalysisHistory_AspNetUsers_UserId",
                table: "AnalysisHistory");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AnalysisHistory",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalysisHistory_AspNetUsers_UserId",
                table: "AnalysisHistory",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalysisHistory_AspNetUsers_UserId",
                table: "AnalysisHistory");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AnalysisHistory",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AnalysisHistory_AspNetUsers_UserId",
                table: "AnalysisHistory",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
