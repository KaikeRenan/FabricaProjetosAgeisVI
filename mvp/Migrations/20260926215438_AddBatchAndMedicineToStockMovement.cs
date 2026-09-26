using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mvp.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchAndMedicineToStockMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_stocks_stock_id",
                table: "stock_movements");

            migrationBuilder.AlterColumn<Guid>(
                name: "stock_id",
                table: "stock_movements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "batch_id",
                table: "stock_movements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "medicine_id",
                table: "stock_movements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_batch_id",
                table: "stock_movements",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_medicine_id",
                table: "stock_movements",
                column: "medicine_id");

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_batches_batch_id",
                table: "stock_movements",
                column: "batch_id",
                principalTable: "batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_medicines_medicine_id",
                table: "stock_movements",
                column: "medicine_id",
                principalTable: "medicines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_stocks_stock_id",
                table: "stock_movements",
                column: "stock_id",
                principalTable: "stocks",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_batches_batch_id",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_medicines_medicine_id",
                table: "stock_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_stocks_stock_id",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_batch_id",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_medicine_id",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "batch_id",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "medicine_id",
                table: "stock_movements");

            migrationBuilder.AlterColumn<Guid>(
                name: "stock_id",
                table: "stock_movements",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_stocks_stock_id",
                table: "stock_movements",
                column: "stock_id",
                principalTable: "stocks",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
