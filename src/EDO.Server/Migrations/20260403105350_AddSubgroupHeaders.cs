using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDO.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSubgroupHeaders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TmcSubgroups_Code",
                table: "TmcSubgroups");

            migrationBuilder.AddColumn<bool>(
                name: "IsHeader",
                table: "TmcSubgroups",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHeader",
                table: "TmcSubgroups");

            migrationBuilder.CreateIndex(
                name: "IX_TmcSubgroups_Code",
                table: "TmcSubgroups",
                column: "Code",
                unique: true);
        }
    }
}
