using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webgiaohang.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryLat",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryLng",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DistanceKm",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PickupLat",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PickupLng",
                table: "Orders",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryLat",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryLng",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DistanceKm",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PickupLat",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PickupLng",
                table: "Orders");
        }
    }
}
