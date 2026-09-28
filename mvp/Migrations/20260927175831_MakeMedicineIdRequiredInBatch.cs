using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mvp.Migrations
{
    /// <inheritdoc />
    public partial class MakeMedicineIdRequiredInBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_batches_medicines_medicine_id",
                table: "batches");

            migrationBuilder.AlterColumn<Guid>(
                name: "medicine_id",
                table: "batches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_batches_medicines_medicine_id",
                table: "batches",
                column: "medicine_id",
                principalTable: "medicines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_batches_medicines_medicine_id",
                table: "batches");

            migrationBuilder.AlterColumn<Guid>(
                name: "medicine_id",
                table: "batches",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_batches_medicines_medicine_id",
                table: "batches",
                column: "medicine_id",
                principalTable: "medicines",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
