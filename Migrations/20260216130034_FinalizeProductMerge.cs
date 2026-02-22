using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grevity.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeProductMerge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductSubProductMappings_MainProducts_MainProductId",
                table: "ProductSubProductMappings");

            migrationBuilder.DropTable(
                name: "MainProducts");

            migrationBuilder.RenameColumn(
                name: "MainProductId",
                table: "ProductSubProductMappings",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductSubProductMappings_MainProductId",
                table: "ProductSubProductMappings",
                newName: "IX_ProductSubProductMappings_ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSubProductMappings_Products_ProductId",
                table: "ProductSubProductMappings",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductSubProductMappings_Products_ProductId",
                table: "ProductSubProductMappings");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "ProductSubProductMappings",
                newName: "MainProductId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductSubProductMappings_ProductId",
                table: "ProductSubProductMappings",
                newName: "IX_ProductSubProductMappings_MainProductId");

            migrationBuilder.CreateTable(
                name: "MainProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    GSTPercentage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SellingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MainProducts", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSubProductMappings_MainProducts_MainProductId",
                table: "ProductSubProductMappings",
                column: "MainProductId",
                principalTable: "MainProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
