using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mvp.Migrations
{
    /// <inheritdoc />
    public partial class AddOccurredAtAndTransferIdToStockMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "occurred_at",
                table: "stock_movements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "transfer_id",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE stock_movements SET occurred_at = created_at;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "occurred_at",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "transfer_id",
                table: "stock_movements");
        }
    }
}
