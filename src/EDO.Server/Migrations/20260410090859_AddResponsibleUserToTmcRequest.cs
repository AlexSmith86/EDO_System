using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDO.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddResponsibleUserToTmcRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ResponsibleUserId",
                table: "TmcRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TmcRequests_ResponsibleUserId",
                table: "TmcRequests",
                column: "ResponsibleUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TmcRequests_Users_ResponsibleUserId",
                table: "TmcRequests",
                column: "ResponsibleUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TmcRequests_Users_ResponsibleUserId",
                table: "TmcRequests");

            migrationBuilder.DropIndex(
                name: "IX_TmcRequests_ResponsibleUserId",
                table: "TmcRequests");

            migrationBuilder.DropColumn(
                name: "ResponsibleUserId",
                table: "TmcRequests");
        }
    }
}
