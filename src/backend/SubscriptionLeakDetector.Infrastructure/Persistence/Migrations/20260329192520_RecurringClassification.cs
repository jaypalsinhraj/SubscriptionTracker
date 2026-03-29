using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionLeakDetector.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecurringClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCredit",
                table: "transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ClassificationReason",
                table: "subscriptions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ClassificationScore",
                table: "subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedMerchant",
                table: "subscriptions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RecurringType",
                table: "subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE subscriptions
                SET "ClassificationScore" = 72,
                    "RecurringType" = 1,
                    "ClassificationReason" = 'Legacy row — re-run import/detection to reclassify',
                    "NormalizedMerchant" = trim(lower("VendorName"))
                WHERE "ClassificationScore" = 0 AND trim(coalesce("NormalizedMerchant", '')) = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCredit",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "ClassificationReason",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "ClassificationScore",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "NormalizedMerchant",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "RecurringType",
                table: "subscriptions");
        }
    }
}
