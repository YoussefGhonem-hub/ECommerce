using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UserAddres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PostalCode",
                table: "UserAddresses",
                newName: "MobileNumber");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "UserAddresses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HouseNo",
                table: "UserAddresses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullName",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "HouseNo",
                table: "UserAddresses");

            migrationBuilder.RenameColumn(
                name: "MobileNumber",
                table: "UserAddresses",
                newName: "PostalCode");
        }
    }
}
