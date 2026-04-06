using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDO.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriesAndExtendRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TmcRequestItems_Tmcs_TmcId",
                table: "TmcRequestItems");

            migrationBuilder.DropIndex(
                name: "IX_TmcRequestItems_TmcId",
                table: "TmcRequestItems");

            migrationBuilder.DropColumn(
                name: "TmcId",
                table: "TmcRequestItems");

            migrationBuilder.AddColumn<string>(
                name: "ProjectName",
                table: "TmcRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "TmcRequestItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "TmcRequestItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceLink",
                table: "TmcRequestItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "TmcRequestItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedDeliveryDate",
                table: "TmcRequestItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubgroupId",
                table: "TmcRequestItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "TmcRequestItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TmcGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TmcGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TmcSubgroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TmcSubgroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TmcSubgroups_TmcGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "TmcGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TmcRequestItems_GroupId",
                table: "TmcRequestItems",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TmcRequestItems_SubgroupId",
                table: "TmcRequestItems",
                column: "SubgroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TmcGroups_Code",
                table: "TmcGroups",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TmcSubgroups_Code",
                table: "TmcSubgroups",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TmcSubgroups_GroupId",
                table: "TmcSubgroups",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_TmcRequestItems_TmcGroups_GroupId",
                table: "TmcRequestItems",
                column: "GroupId",
                principalTable: "TmcGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TmcRequestItems_TmcSubgroups_SubgroupId",
                table: "TmcRequestItems",
                column: "SubgroupId",
                principalTable: "TmcSubgroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TmcRequestItems_TmcGroups_GroupId",
                table: "TmcRequestItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TmcRequestItems_TmcSubgroups_SubgroupId",
                table: "TmcRequestItems");

            migrationBuilder.DropTable(
                name: "TmcSubgroups");

            migrationBuilder.DropTable(
                name: "TmcGroups");

            migrationBuilder.DropIndex(
                name: "IX_TmcRequestItems_GroupId",
                table: "TmcRequestItems");

            migrationBuilder.DropIndex(
                name: "IX_TmcRequestItems_SubgroupId",
                table: "TmcRequestItems");

            migrationBuilder.DropColumn(
                name: "ProjectName",
                table: "TmcRequests");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "TmcRequestItems");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "TmcRequestItems");

            migrationBuilder.DropColumn(
                name: "InvoiceLink",
                table: "TmcRequestItems");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "TmcRequestItems");

            migrationBuilder.DropColumn(
                name: "PlannedDeliveryDate",
                table: "TmcRequestItems");

            migrationBuilder.DropColumn(
                name: "SubgroupId",
                table: "TmcRequestItems");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "TmcRequestItems");

            migrationBuilder.AddColumn<int>(
                name: "TmcId",
                table: "TmcRequestItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TmcRequestItems_TmcId",
                table: "TmcRequestItems",
                column: "TmcId");

            migrationBuilder.AddForeignKey(
                name: "FK_TmcRequestItems_Tmcs_TmcId",
                table: "TmcRequestItems",
                column: "TmcId",
                principalTable: "Tmcs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
