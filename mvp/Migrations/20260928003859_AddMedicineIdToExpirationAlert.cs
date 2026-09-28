using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mvp.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicineIdToExpirationAlert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_expiration_alerts_batches_batch_id",
                table: "expiration_alerts");

            migrationBuilder.AlterColumn<Guid>(
                name: "batch_id",
                table: "expiration_alerts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "medicine_id",
                table: "expiration_alerts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_expiration_alerts_medicine_id",
                table: "expiration_alerts",
                column: "medicine_id");

            migrationBuilder.AddForeignKey(
                name: "FK_expiration_alerts_batches_batch_id",
                table: "expiration_alerts",
                column: "batch_id",
                principalTable: "batches",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_expiration_alerts_medicines_medicine_id",
                table: "expiration_alerts",
                column: "medicine_id",
                principalTable: "medicines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_expiration_alerts_batches_batch_id",
                table: "expiration_alerts");

            migrationBuilder.DropForeignKey(
                name: "FK_expiration_alerts_medicines_medicine_id",
                table: "expiration_alerts");

            migrationBuilder.DropIndex(
                name: "IX_expiration_alerts_medicine_id",
                table: "expiration_alerts");

            migrationBuilder.DropColumn(
                name: "medicine_id",
                table: "expiration_alerts");

            migrationBuilder.AlterColumn<Guid>(
                name: "batch_id",
                table: "expiration_alerts",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_expiration_alerts_batches_batch_id",
                table: "expiration_alerts",
                column: "batch_id",
                principalTable: "batches",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
