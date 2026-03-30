using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionLeakDetector.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DetectionPipelineV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ClassificationScore",
                table: "subscriptions",
                newName: "SubscriptionConfidenceScore");

            migrationBuilder.AddColumn<string>(
                name: "MatchedNormalizationRule",
                table: "transactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NormalizationConfidence",
                table: "transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NormalizationReason",
                table: "transactions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedMerchant",
                table: "transactions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawDescription",
                table: "transactions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE transactions SET "RawDescription" = "VendorName" WHERE "RawDescription" IS NULL;
                UPDATE transactions SET "Description" = "VendorName" WHERE "Description" IS NULL;
                """);

            migrationBuilder.AddColumn<bool>(
                name: "IsSubscriptionCandidate",
                table: "subscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "recurring_candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupKey = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    VendorName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NormalizedMerchant = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RecurringType = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionConfidenceScore = table.Column<int>(type: "integer", nullable: false),
                    ClassificationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PatternConfidenceScore = table.Column<int>(type: "integer", nullable: false),
                    Cadence = table.Column<int>(type: "integer", nullable: false),
                    AverageAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    LastChargeDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NextExpectedChargeDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_candidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recurring_candidates_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recurring_candidates_AccountId_GroupKey",
                table: "recurring_candidates",
                columns: new[] { "AccountId", "GroupKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recurring_candidates");

            migrationBuilder.DropColumn(
                name: "MatchedNormalizationRule",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "NormalizationConfidence",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "NormalizationReason",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "NormalizedMerchant",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "RawDescription",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "IsSubscriptionCandidate",
                table: "subscriptions");

            migrationBuilder.RenameColumn(
                name: "SubscriptionConfidenceScore",
                table: "subscriptions",
                newName: "ClassificationScore");
        }
    }
}
