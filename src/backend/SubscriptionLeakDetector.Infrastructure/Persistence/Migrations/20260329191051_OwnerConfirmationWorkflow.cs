using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionLeakDetector.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OwnerConfirmationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastConfirmedInUseAt",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReviewRequestedAt",
                table: "subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NextReviewDate",
                table: "subscriptions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerEmail",
                table: "subscriptions",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerName",
                table: "subscriptions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "subscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewStatus",
                table: "subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsageConfidenceScore",
                table: "subscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AlertStatus",
                table: "renewal_alerts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "renewal_alerts",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RespondedAt",
                table: "renewal_alerts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RespondedByUserId",
                table: "renewal_alerts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponseType",
                table: "renewal_alerts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_OwnerUserId",
                table: "subscriptions",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_subscriptions_users_OwnerUserId",
                table: "subscriptions",
                column: "OwnerUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_subscriptions_users_OwnerUserId",
                table: "subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_subscriptions_OwnerUserId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "LastConfirmedInUseAt",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "LastReviewRequestedAt",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "NextReviewDate",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "OwnerEmail",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "OwnerName",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "UsageConfidenceScore",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "AlertStatus",
                table: "renewal_alerts");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "renewal_alerts");

            migrationBuilder.DropColumn(
                name: "RespondedAt",
                table: "renewal_alerts");

            migrationBuilder.DropColumn(
                name: "RespondedByUserId",
                table: "renewal_alerts");

            migrationBuilder.DropColumn(
                name: "ResponseType",
                table: "renewal_alerts");
        }
    }
}
