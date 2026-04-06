using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDO.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddInitiatorFieldsToRequestItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InitiatorName",
                table: "TmcRequestItems",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InitiatorPosition",
                table: "TmcRequestItems",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitiatorName",
                table: "TmcRequestItems");

            migrationBuilder.DropColumn(
                name: "InitiatorPosition",
                table: "TmcRequestItems");
        }
    }
}
