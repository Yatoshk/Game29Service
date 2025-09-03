using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Game29Prices.Migrations
{
    /// <inheritdoc />
    public partial class CreateProductPricesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductPrices",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    subcategory = table.Column<string>(type: "text", nullable: false),
                    product = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    price_data = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPrices", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductPrices_category",
                table: "ProductPrices",
                column: "category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductPrices");
        }
    }
}
