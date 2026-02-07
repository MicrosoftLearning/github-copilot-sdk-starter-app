using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContosoShop.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnedQuantityToOrderItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReturnedQuantity",
                table: "OrderItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnedQuantity",
                table: "OrderItems");
        }
    }
}
