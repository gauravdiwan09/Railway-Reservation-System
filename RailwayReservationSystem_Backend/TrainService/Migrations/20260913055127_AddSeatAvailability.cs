using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainService.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeatAvailabilities",
                columns: table => new
                {
                    SeatAvailabilityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeatId = table.Column<int>(type: "int", nullable: false),
                    JourneyDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatAvailabilities", x => x.SeatAvailabilityId);
                    table.ForeignKey(
                        name: "FK_SeatAvailabilities_Seats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "Seats",
                        principalColumn: "SeatId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeatAvailabilities_SeatId_JourneyDate",
                table: "SeatAvailabilities",
                columns: new[] { "SeatId", "JourneyDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeatAvailabilities");
        }
    }
}
