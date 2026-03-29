using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionLeakDetector.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountGlobalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultCurrency",
                table: "accounts",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<string>(
                name: "UiCulture",
                table: "accounts",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "en-US");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultCurrency",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "UiCulture",
                table: "accounts");
        }
    }
}
