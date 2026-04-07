using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDO.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowChains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentWorkflowStepId",
                table: "TmcRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowChainId",
                table: "TmcRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkflowChains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowChains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowChainId = table.Column<int>(type: "int", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TargetPosition = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowSteps_WorkflowChains_WorkflowChainId",
                        column: x => x.WorkflowChainId,
                        principalTable: "WorkflowChains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TmcRequests_CurrentWorkflowStepId",
                table: "TmcRequests",
                column: "CurrentWorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_TmcRequests_WorkflowChainId",
                table: "TmcRequests",
                column: "WorkflowChainId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSteps_WorkflowChainId",
                table: "WorkflowSteps",
                column: "WorkflowChainId");

            migrationBuilder.AddForeignKey(
                name: "FK_TmcRequests_WorkflowChains_WorkflowChainId",
                table: "TmcRequests",
                column: "WorkflowChainId",
                principalTable: "WorkflowChains",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TmcRequests_WorkflowSteps_CurrentWorkflowStepId",
                table: "TmcRequests",
                column: "CurrentWorkflowStepId",
                principalTable: "WorkflowSteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TmcRequests_WorkflowChains_WorkflowChainId",
                table: "TmcRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TmcRequests_WorkflowSteps_CurrentWorkflowStepId",
                table: "TmcRequests");

            migrationBuilder.DropTable(
                name: "WorkflowSteps");

            migrationBuilder.DropTable(
                name: "WorkflowChains");

            migrationBuilder.DropIndex(
                name: "IX_TmcRequests_CurrentWorkflowStepId",
                table: "TmcRequests");

            migrationBuilder.DropIndex(
                name: "IX_TmcRequests_WorkflowChainId",
                table: "TmcRequests");

            migrationBuilder.DropColumn(
                name: "CurrentWorkflowStepId",
                table: "TmcRequests");

            migrationBuilder.DropColumn(
                name: "WorkflowChainId",
                table: "TmcRequests");
        }
    }
}
