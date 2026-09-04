using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpSync.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Anomalies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PurchaseOrder = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PurchaseOrderItemNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RuleType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anomalies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    PurchaseOrderNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PurchaseOrderType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PurchasingOrganization = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PurchasingGroup = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Supplier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PurchaseOrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DocumentCurrency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PurchasingCompletenessStatus = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.PurchaseOrderNumber);
                });

            migrationBuilder.CreateTable(
                name: "SyncLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordsFetched = table.Column<int>(type: "int", nullable: false),
                    RecordsNew = table.Column<int>(type: "int", nullable: false),
                    AnomaliesFound = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItems",
                columns: table => new
                {
                    PurchaseOrder = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PurchaseOrderItemNumber = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Material = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Plant = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    PurchaseOrderQuantityUnit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NetPriceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItems", x => new { x.PurchaseOrder, x.PurchaseOrderItemNumber });
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrder",
                        column: x => x.PurchaseOrder,
                        principalTable: "PurchaseOrders",
                        principalColumn: "PurchaseOrderNumber",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Anomalies");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItems");

            migrationBuilder.DropTable(
                name: "SyncLogs");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");
        }
    }
}
