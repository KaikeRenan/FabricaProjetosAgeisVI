using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mvp.Migrations
{
    /// <inheritdoc />
    public partial class AddRedistribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "redistribution_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    already_expired_batches = table.Column<int>(type: "integer", nullable: false),
                    as_of = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_fingerprint = table.Column<string>(type: "text", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    horizon_weeks = table.Column<int>(type: "integer", nullable: false),
                    model_version = table.Column<string>(type: "text", nullable: false),
                    no_forecast_pairs = table.Column<int>(type: "integer", nullable: false),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    residual_waste_units = table.Column<double>(type: "double precision", nullable: false),
                    serving_tier = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    triggered_by = table.Column<string>(type: "text", nullable: true),
                    units_at_risk = table.Column<double>(type: "double precision", nullable: false),
                    units_rescued = table.Column<double>(type: "double precision", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    warnings = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_redistribution_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "redistribution_suggestions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    days_to_expiry = table.Column<int>(type: "integer", nullable: false),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decided_by = table.Column<string>(type: "text", nullable: true),
                    distance_km = table.Column<double>(type: "double precision", nullable: true),
                    expiration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    from_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_location_kind = table.Column<string>(type: "text", nullable: false),
                    medicine_id = table.Column<Guid>(type: "uuid", nullable: false),
                    projected_waste_at_source = table.Column<double>(type: "double precision", nullable: false),
                    proximity_cost = table.Column<double>(type: "double precision", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    to_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_location_kind = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_redistribution_suggestions", x => x.id);
                    table.ForeignKey(
                        name: "FK_redistribution_suggestions_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_redistribution_suggestions_medicines_medicine_id",
                        column: x => x.medicine_id,
                        principalTable: "medicines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_redistribution_suggestions_redistribution_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "redistribution_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_redistribution_suggestions_batch_id",
                table: "redistribution_suggestions",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_redistribution_suggestions_medicine_id",
                table: "redistribution_suggestions",
                column: "medicine_id");

            migrationBuilder.CreateIndex(
                name: "IX_redistribution_suggestions_run_id",
                table: "redistribution_suggestions",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "IX_redistribution_suggestions_status",
                table: "redistribution_suggestions",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "redistribution_suggestions");

            migrationBuilder.DropTable(
                name: "redistribution_runs");
        }
    }
}
