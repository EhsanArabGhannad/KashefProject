using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KashefProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerFulfillmentMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerMessage",
                table: "Orders",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerMessage",
                table: "Orders");
        }
    }
}
