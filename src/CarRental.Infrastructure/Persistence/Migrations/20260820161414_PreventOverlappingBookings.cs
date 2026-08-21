using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRental.Infrastructure.Persistence.Migrations
{
    public partial class PreventOverlappingBookings : Migration
    {
        private const string ConstraintName = "ck_reservations_no_overlapping_confirmed_bookings";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
            
            migrationBuilder.Sql($"""
                ALTER TABLE "Reservations"
                ADD CONSTRAINT "{ConstraintName}"
                EXCLUDE USING gist (
                    "CarId" WITH =,
                    daterange("StartDate", "EndDate", '[]') WITH &&
                ) WHERE ("Status" = 'Confirmed');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""ALTER TABLE "Reservations" DROP CONSTRAINT "{ConstraintName}";""");
        }
    }
}
