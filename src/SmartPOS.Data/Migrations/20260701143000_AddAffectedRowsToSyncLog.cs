using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAffectedRowsToSyncLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AffectedRows",
                table: "InventorySyncLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AffectedRows",
                table: "InventorySyncLogs");
        }
    }
}
