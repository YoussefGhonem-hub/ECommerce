using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SocialProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SocialProfiles_FacebookUrl",
                table: "AspNetUsers",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialProfiles_InstagramUrl",
                table: "AspNetUsers",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialProfiles_TelegramUrl",
                table: "AspNetUsers",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialProfiles_TikTokUrl",
                table: "AspNetUsers",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialProfiles_WebsiteUrl",
                table: "AspNetUsers",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialProfiles_WhatsAppUrl",
                table: "AspNetUsers",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialProfiles_YouTubeUrl",
                table: "AspNetUsers",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "AspNetRoles",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SocialProfiles_FacebookUrl",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SocialProfiles_InstagramUrl",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SocialProfiles_TelegramUrl",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SocialProfiles_TikTokUrl",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SocialProfiles_WebsiteUrl",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SocialProfiles_WhatsAppUrl",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SocialProfiles_YouTubeUrl",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "AspNetRoles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);
        }
    }
}
