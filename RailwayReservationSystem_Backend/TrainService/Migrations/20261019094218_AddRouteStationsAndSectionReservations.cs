using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainService.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteStationsAndSectionReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalDistanceKm",
                table: "Trains",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EndDistanceKm",
                table: "Seats",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StartDistanceKm",
                table: "Seats",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RouteStations",
                columns: table => new
                {
                    RouteStationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainId = table.Column<int>(type: "int", nullable: false),
                    StationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    DistanceFromOriginKm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteStations", x => x.RouteStationId);
                    table.ForeignKey(
                        name: "FK_RouteStations_Trains_TrainId",
                        column: x => x.TrainId,
                        principalTable: "Trains",
                        principalColumn: "TrainId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeatReservations",
                columns: table => new
                {
                    SeatReservationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeatId = table.Column<int>(type: "int", nullable: false),
                    JourneyDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartDistanceKm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    EndDistanceKm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatReservations", x => x.SeatReservationId);
                    table.ForeignKey(
                        name: "FK_SeatReservations_Seats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "Seats",
                        principalColumn: "SeatId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RouteStations_TrainId_Sequence",
                table: "RouteStations",
                columns: new[] { "TrainId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeatReservations_SeatId_JourneyDate",
                table: "SeatReservations",
                columns: new[] { "SeatId", "JourneyDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteStations");

            migrationBuilder.DropTable(
                name: "SeatReservations");

            migrationBuilder.DropColumn(
                name: "TotalDistanceKm",
                table: "Trains");

            migrationBuilder.DropColumn(
                name: "EndDistanceKm",
                table: "Seats");

            migrationBuilder.DropColumn(
                name: "StartDistanceKm",
                table: "Seats");
        }
    }
}
