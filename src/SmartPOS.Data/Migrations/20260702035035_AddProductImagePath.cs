using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPOS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImagePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode",
                unique: true,
                filter: "\"Barcode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ExternalInventoryId",
                table: "Products",
                column: "ExternalInventoryId",
                unique: true,
                filter: "\"ExternalInventoryId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_QrCode",
                table: "Products",
                column: "QrCode",
                unique: true,
                filter: "\"QrCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                table: "Products",
                column: "Sku",
                unique: true,
                filter: "\"Sku\" IS NOT NULL AND \"Sku\" != ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Barcode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ExternalInventoryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_QrCode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Sku",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Products");
        }
    }
}
