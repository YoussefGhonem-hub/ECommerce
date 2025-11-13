using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SelectedAttributeDto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CartItemAttribute",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CartItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductAttributeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductAttributeValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttributeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItemAttribute", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItemAttribute_CartItems_CartItemId",
                        column: x => x.CartItemId,
                        principalTable: "CartItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItemAttribute_ProductAttributeValues_ProductAttributeValueId",
                        column: x => x.ProductAttributeValueId,
                        principalTable: "ProductAttributeValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CartItemAttribute_ProductAttributes_ProductAttributeId",
                        column: x => x.ProductAttributeId,
                        principalTable: "ProductAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItemAttribute",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductAttributeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductAttributeValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttributeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemAttribute", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemAttribute_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItemAttribute_ProductAttributeValues_ProductAttributeValueId",
                        column: x => x.ProductAttributeValueId,
                        principalTable: "ProductAttributeValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderItemAttribute_ProductAttributes_ProductAttributeId",
                        column: x => x.ProductAttributeId,
                        principalTable: "ProductAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CartItemAttribute_CartItemId",
                table: "CartItemAttribute",
                column: "CartItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItemAttribute_IsDeleted",
                table: "CartItemAttribute",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CartItemAttribute_ProductAttributeId",
                table: "CartItemAttribute",
                column: "ProductAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItemAttribute_ProductAttributeValueId",
                table: "CartItemAttribute",
                column: "ProductAttributeValueId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemAttribute_IsDeleted",
                table: "OrderItemAttribute",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemAttribute_OrderItemId",
                table: "OrderItemAttribute",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemAttribute_ProductAttributeId",
                table: "OrderItemAttribute",
                column: "ProductAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemAttribute_ProductAttributeValueId",
                table: "OrderItemAttribute",
                column: "ProductAttributeValueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartItemAttribute");

            migrationBuilder.DropTable(
                name: "OrderItemAttribute");
        }
    }
}
