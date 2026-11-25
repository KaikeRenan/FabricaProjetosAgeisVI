using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mvp.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovementOccurredAtAndTransferId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "occurred_at",
                table: "stock_movements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("UPDATE stock_movements SET occurred_at = created_at WHERE occurred_at IS NULL;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "occurred_at",
                table: "stock_movements",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "transfer_id",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_occurred_at_id",
                table: "stock_movements",
                columns: new[] { "occurred_at", "id" },
                descending: new bool[0],
                filter: "removed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_transfer_id",
                table: "stock_movements",
                column: "transfer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
               name: "IX_stock_movements_occurred_at_id",
               table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_transfer_id",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "occurred_at",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "transfer_id",
                table: "stock_movements");
        }
    }
}
