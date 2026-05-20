using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TenantPortal.Auth.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotificationEmail",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationEmail",
                table: "Users");
        }
    }
}
