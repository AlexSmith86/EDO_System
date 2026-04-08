using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDO.Server.Migrations
{
    /// <inheritdoc />
    public partial class FixActionHistoryForCustomChains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "StageId",
                table: "ActionHistories",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStepId",
                table: "ActionHistories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActionHistories_WorkflowStepId",
                table: "ActionHistories",
                column: "WorkflowStepId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActionHistories_WorkflowSteps_WorkflowStepId",
                table: "ActionHistories",
                column: "WorkflowStepId",
                principalTable: "WorkflowSteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActionHistories_WorkflowSteps_WorkflowStepId",
                table: "ActionHistories");

            migrationBuilder.DropIndex(
                name: "IX_ActionHistories_WorkflowStepId",
                table: "ActionHistories");

            migrationBuilder.DropColumn(
                name: "WorkflowStepId",
                table: "ActionHistories");

            migrationBuilder.AlterColumn<int>(
                name: "StageId",
                table: "ActionHistories",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
