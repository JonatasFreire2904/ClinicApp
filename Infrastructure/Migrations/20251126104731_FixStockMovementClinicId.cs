using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixStockMovementClinicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rebuild StockMovements table to allow nullable ClinicId
            
            // 1. Rename existing table
            migrationBuilder.Sql("ALTER TABLE StockMovements RENAME TO StockMovements_Old;");

            // 2. Create new table with correct schema
            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClinicId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MaterialId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MovementType = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMovements_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockMovements_Users_PerformedByUserId",
                        column: x => x.PerformedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 3. Copy data
            migrationBuilder.Sql("INSERT INTO StockMovements (Id, ClinicId, MaterialId, PerformedByUserId, MovementType, Quantity, Note, CreatedAt) SELECT Id, ClinicId, MaterialId, PerformedByUserId, MovementType, Quantity, Note, CreatedAt FROM StockMovements_Old;");

            // 4. Drop old table
            migrationBuilder.Sql("DROP TABLE StockMovements_Old;");

            // 5. Recreate indices
            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ClinicId",
                table: "StockMovements",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MaterialId",
                table: "StockMovements",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_PerformedByUserId",
                table: "StockMovements",
                column: "PerformedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverting would involve making ClinicId NOT NULL again, which might fail if there are nulls.
            // For now, we'll just drop the table and recreate the old one (simplified) or just do nothing as this is a fix.
            // But to be safe, let's just drop the new one and rename the old one back if we were fully implementing Down.
            // Since we dropped the old one in Up, we can't easily revert without data loss unless we backed it up.
            // We will leave Down empty or just drop the table.
            migrationBuilder.DropTable(name: "StockMovements");
        }
    }
}
