using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialInventoryCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QrCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_products_inventory_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "inventory_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "stock_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_items_inventory_products_InventoryProductId",
                        column: x => x.InventoryProductId,
                        principalTable: "inventory_products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_transactions_inventory_products_InventoryProductId",
                        column: x => x.InventoryProductId,
                        principalTable: "inventory_products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_categories_Name",
                table: "inventory_categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_products_Barcode",
                table: "inventory_products",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_products_CategoryId",
                table: "inventory_products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_products_QrCode",
                table: "inventory_products",
                column: "QrCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_products_Sku",
                table: "inventory_products",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_items_InventoryProductId",
                table: "stock_items",
                column: "InventoryProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_InventoryProductId",
                table: "stock_transactions",
                column: "InventoryProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_items");

            migrationBuilder.DropTable(
                name: "stock_transactions");

            migrationBuilder.DropTable(
                name: "inventory_products");

            migrationBuilder.DropTable(
                name: "inventory_categories");
        }
    }
}
