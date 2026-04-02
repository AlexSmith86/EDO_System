using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDO.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddTmcRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TmcRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InitiatorUserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CurrentStageId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TmcRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TmcRequests_ApprovalStages_CurrentStageId",
                        column: x => x.CurrentStageId,
                        principalTable: "ApprovalStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TmcRequests_Users_InitiatorUserId",
                        column: x => x.InitiatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TmcRequestItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TmcRequestId = table.Column<int>(type: "int", nullable: false),
                    TmcId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TmcRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TmcRequestItems_TmcRequests_TmcRequestId",
                        column: x => x.TmcRequestId,
                        principalTable: "TmcRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TmcRequestItems_Tmcs_TmcId",
                        column: x => x.TmcId,
                        principalTable: "Tmcs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TmcRequestItems_TmcId",
                table: "TmcRequestItems",
                column: "TmcId");

            migrationBuilder.CreateIndex(
                name: "IX_TmcRequestItems_TmcRequestId",
                table: "TmcRequestItems",
                column: "TmcRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TmcRequests_CurrentStageId",
                table: "TmcRequests",
                column: "CurrentStageId");

            migrationBuilder.CreateIndex(
                name: "IX_TmcRequests_InitiatorUserId",
                table: "TmcRequests",
                column: "InitiatorUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TmcRequestItems");

            migrationBuilder.DropTable(
                name: "TmcRequests");
        }
    }
}
