using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KashefProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RefundReason",
                table: "Orders",
                type: "TEXT",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundRequestedUtc",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RefundedCents",
                table: "Orders",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedUtc",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeRefundId",
                table: "Orders",
                type: "TEXT",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeRefundStatus",
                table: "Orders",
                type: "TEXT",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundRequestedUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundedCents",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundedUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StripeRefundId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StripeRefundStatus",
                table: "Orders");
        }
    }
}
