using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KashefProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    OwnerHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CheckoutKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    AddressLine1 = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    AddressLine2 = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    PostalCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    SubtotalCents = table.Column<long>(type: "INTEGER", nullable: false),
                    ShippingCents = table.Column<long>(type: "INTEGER", nullable: true),
                    TaxCents = table.Column<long>(type: "INTEGER", nullable: true),
                    TotalCents = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StoreOrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Finish = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Size = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    UnitPriceCents = table.Column<long>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLines", x => x.Id);
                    table.CheckConstraint("CK_OrderLine_Price", "UnitPriceCents > 0");
                    table.CheckConstraint("CK_OrderLine_Quantity", "Quantity BETWEEN 1 AND 20");
                    table.ForeignKey(
                        name: "FK_OrderLines_Orders_StoreOrderId",
                        column: x => x.StoreOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_StoreOrderId",
                table: "OrderLines",
                column: "StoreOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CheckoutKey",
                table: "Orders",
                column: "CheckoutKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Reference",
                table: "Orders",
                column: "Reference",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderLines");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
