using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mvp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "health_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    street = table.Column<string>(type: "text", nullable: false),
                    number = table.Column<string>(type: "text", nullable: false),
                    neighborhood = table.Column<string>(type: "text", nullable: false),
                    zip_code = table.Column<string>(type: "text", nullable: false),
                    zone = table.Column<string>(type: "text", nullable: false),
                    cnes = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_health_units", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "medicines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    active_ingredient = table.Column<string>(type: "text", nullable: false),
                    dosage = table.Column<string>(type: "text", nullable: false),
                    unit = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medicines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pharmacies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    street = table.Column<string>(type: "text", nullable: false),
                    number = table.Column<string>(type: "text", nullable: false),
                    neighborhood = table.Column<string>(type: "text", nullable: false),
                    zip_code = table.Column<string>(type: "text", nullable: false),
                    zone = table.Column<string>(type: "text", nullable: false),
                    cnpj = table.Column<string>(type: "text", nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pharmacies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    password = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    medicine_id = table.Column<Guid>(type: "uuid", nullable: true),
                    batch_number = table.Column<string>(type: "text", nullable: false),
                    fabrication_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_batches", x => x.id);
                    table.ForeignKey(
                        name: "FK_batches_medicines_medicine_id",
                        column: x => x.medicine_id,
                        principalTable: "medicines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "expiration_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    alert_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expiration_alerts", x => x.id);
                    table.ForeignKey(
                        name: "FK_expiration_alerts_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "stocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pharmacy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    health_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stocks", x => x.id);
                    table.ForeignKey(
                        name: "FK_stocks_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stocks_health_units_health_unit_id",
                        column: x => x.health_unit_id,
                        principalTable: "health_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stocks_pharmacies_pharmacy_id",
                        column: x => x.pharmacy_id,
                        principalTable: "pharmacies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_movements", x => x.id);
                    table.ForeignKey(
                        name: "FK_stock_movements_stocks_stock_id",
                        column: x => x.stock_id,
                        principalTable: "stocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_batches_medicine_id",
                table: "batches",
                column: "medicine_id");

            migrationBuilder.CreateIndex(
                name: "IX_expiration_alerts_batch_id",
                table: "expiration_alerts",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_stock_id",
                table: "stock_movements",
                column: "stock_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocks_batch_id",
                table: "stocks",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocks_health_unit_id",
                table: "stocks",
                column: "health_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_stocks_pharmacy_id",
                table: "stocks",
                column: "pharmacy_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expiration_alerts");

            migrationBuilder.DropTable(
                name: "stock_movements");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "stocks");

            migrationBuilder.DropTable(
                name: "batches");

            migrationBuilder.DropTable(
                name: "health_units");

            migrationBuilder.DropTable(
                name: "pharmacies");

            migrationBuilder.DropTable(
                name: "medicines");
        }
    }
}
