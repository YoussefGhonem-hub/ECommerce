using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserAddresses_IsDeleted",
                table: "UserAddresses");

            migrationBuilder.DropIndex(
                name: "IX_ShippingZones_IsDeleted",
                table: "ShippingZones");

            migrationBuilder.DropIndex(
                name: "IX_ShippingMethods_IsDeleted",
                table: "ShippingMethods");

            migrationBuilder.DropIndex(
                name: "IX_ProductSettings_IsDeleted",
                table: "ProductSettings");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsDeleted",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductReviews_IsDeleted",
                table: "ProductReviews");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_IsDeleted",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributeValues_IsDeleted",
                table: "ProductAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_IsDeleted",
                table: "ProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributeMappings_IsDeleted",
                table: "ProductAttributeMappings");

            migrationBuilder.DropIndex(
                name: "IX_Orders_IsDeleted",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_IsDeleted",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItemAttribute_IsDeleted",
                table: "OrderItemAttribute");

            migrationBuilder.DropIndex(
                name: "IX_FeaturedProducts_IsDeleted",
                table: "FeaturedProducts");

            migrationBuilder.DropIndex(
                name: "IX_FavoriteProducts_IsDeleted",
                table: "FavoriteProducts");

            migrationBuilder.DropIndex(
                name: "IX_CouponUsages_IsDeleted",
                table: "CouponUsages");

            migrationBuilder.DropIndex(
                name: "IX_Coupons_IsDeleted",
                table: "Coupons");

            migrationBuilder.DropIndex(
                name: "IX_Countries_IsDeleted",
                table: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_Cities_IsDeleted",
                table: "Cities");

            migrationBuilder.DropIndex(
                name: "IX_Categories_IsDeleted",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Carts_IsDeleted",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_IsDeleted",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItemAttributes_IsDeleted",
                table: "CartItemAttributes");

            migrationBuilder.DropIndex(
                name: "IX_Brands_IsDeleted",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_Banners_IsDeleted",
                table: "Banners");

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReasonRevoked = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.CreateIndex(
                name: "IX_UserAddresses_IsDeleted",
                table: "UserAddresses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingZones_IsDeleted",
                table: "ShippingZones",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingMethods_IsDeleted",
                table: "ShippingMethods",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSettings_IsDeleted",
                table: "ProductSettings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsDeleted",
                table: "Products",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_IsDeleted",
                table: "ProductReviews",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_IsDeleted",
                table: "ProductImages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_IsDeleted",
                table: "ProductAttributeValues",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_IsDeleted",
                table: "ProductAttributes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeMappings_IsDeleted",
                table: "ProductAttributeMappings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_IsDeleted",
                table: "Orders",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_IsDeleted",
                table: "OrderItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemAttribute_IsDeleted",
                table: "OrderItemAttribute",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturedProducts_IsDeleted",
                table: "FeaturedProducts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteProducts_IsDeleted",
                table: "FavoriteProducts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CouponUsages_IsDeleted",
                table: "CouponUsages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_IsDeleted",
                table: "Coupons",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_IsDeleted",
                table: "Countries",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_IsDeleted",
                table: "Cities",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsDeleted",
                table: "Categories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_IsDeleted",
                table: "Carts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_IsDeleted",
                table: "CartItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CartItemAttributes_IsDeleted",
                table: "CartItemAttributes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_IsDeleted",
                table: "Brands",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Banners_IsDeleted",
                table: "Banners",
                column: "IsDeleted");
        }
    }
}
