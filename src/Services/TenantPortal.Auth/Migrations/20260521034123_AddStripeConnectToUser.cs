using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantPortal.Auth.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeConnectToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StripeConnectChargesEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StripeConnectedAccountId",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeConnectChargesEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StripeConnectedAccountId",
                table: "Users");
        }
    }
}
