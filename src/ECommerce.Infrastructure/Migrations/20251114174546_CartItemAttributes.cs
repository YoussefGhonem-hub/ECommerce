using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CartItemAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItemAttribute_CartItems_CartItemId",
                table: "CartItemAttribute");

            migrationBuilder.DropForeignKey(
                name: "FK_CartItemAttribute_ProductAttributeValues_ProductAttributeValueId",
                table: "CartItemAttribute");

            migrationBuilder.DropForeignKey(
                name: "FK_CartItemAttribute_ProductAttributes_ProductAttributeId",
                table: "CartItemAttribute");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CartItemAttribute",
                table: "CartItemAttribute");

            migrationBuilder.RenameTable(
                name: "CartItemAttribute",
                newName: "CartItemAttributes");

            migrationBuilder.RenameIndex(
                name: "IX_CartItemAttribute_ProductAttributeValueId",
                table: "CartItemAttributes",
                newName: "IX_CartItemAttributes_ProductAttributeValueId");

            migrationBuilder.RenameIndex(
                name: "IX_CartItemAttribute_ProductAttributeId",
                table: "CartItemAttributes",
                newName: "IX_CartItemAttributes_ProductAttributeId");

            migrationBuilder.RenameIndex(
                name: "IX_CartItemAttribute_IsDeleted",
                table: "CartItemAttributes",
                newName: "IX_CartItemAttributes_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_CartItemAttribute_CartItemId",
                table: "CartItemAttributes",
                newName: "IX_CartItemAttributes_CartItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CartItemAttributes",
                table: "CartItemAttributes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItemAttributes_CartItems_CartItemId",
                table: "CartItemAttributes",
                column: "CartItemId",
                principalTable: "CartItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItemAttributes_ProductAttributeValues_ProductAttributeValueId",
                table: "CartItemAttributes",
                column: "ProductAttributeValueId",
                principalTable: "ProductAttributeValues",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItemAttributes_ProductAttributes_ProductAttributeId",
                table: "CartItemAttributes",
                column: "ProductAttributeId",
                principalTable: "ProductAttributes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItemAttributes_CartItems_CartItemId",
                table: "CartItemAttributes");

            migrationBuilder.DropForeignKey(
                name: "FK_CartItemAttributes_ProductAttributeValues_ProductAttributeValueId",
                table: "CartItemAttributes");

            migrationBuilder.DropForeignKey(
                name: "FK_CartItemAttributes_ProductAttributes_ProductAttributeId",
                table: "CartItemAttributes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CartItemAttributes",
                table: "CartItemAttributes");

            migrationBuilder.RenameTable(
                name: "CartItemAttributes",
                newName: "CartItemAttribute");

            migrationBuilder.RenameIndex(
                name: "IX_CartItemAttributes_ProductAttributeValueId",
                table: "CartItemAttribute",
                newName: "IX_CartItemAttribute_ProductAttributeValueId");

            migrationBuilder.RenameIndex(
                name: "IX_CartItemAttributes_ProductAttributeId",
                table: "CartItemAttribute",
                newName: "IX_CartItemAttribute_ProductAttributeId");

            migrationBuilder.RenameIndex(
                name: "IX_CartItemAttributes_IsDeleted",
                table: "CartItemAttribute",
                newName: "IX_CartItemAttribute_IsDeleted");

            migrationBuilder.RenameIndex(
                name: "IX_CartItemAttributes_CartItemId",
                table: "CartItemAttribute",
                newName: "IX_CartItemAttribute_CartItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CartItemAttribute",
                table: "CartItemAttribute",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItemAttribute_CartItems_CartItemId",
                table: "CartItemAttribute",
                column: "CartItemId",
                principalTable: "CartItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItemAttribute_ProductAttributeValues_ProductAttributeValueId",
                table: "CartItemAttribute",
                column: "ProductAttributeValueId",
                principalTable: "ProductAttributeValues",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItemAttribute_ProductAttributes_ProductAttributeId",
                table: "CartItemAttribute",
                column: "ProductAttributeId",
                principalTable: "ProductAttributes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
