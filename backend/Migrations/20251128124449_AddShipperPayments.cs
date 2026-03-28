using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webgiaohang.Migrations
{
    /// <inheritdoc />
    public partial class AddShipperPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShipperPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderId = table.Column<int>(type: "INTEGER", nullable: false),
                    ShipperName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    CommissionPercent = table.Column<decimal>(type: "TEXT", nullable: false),
                    OrderTotalAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PaymentMethod = table.Column<string>(type: "TEXT", nullable: true),
                    TransactionId = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipperPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipperPayments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShipperPayments_OrderId",
                table: "ShipperPayments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipperPayments_ShipperName",
                table: "ShipperPayments",
                column: "ShipperName");

            migrationBuilder.CreateIndex(
                name: "IX_ShipperPayments_Status",
                table: "ShipperPayments",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShipperPayments");
        }
    }
}
